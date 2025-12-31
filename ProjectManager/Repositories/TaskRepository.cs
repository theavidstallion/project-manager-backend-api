using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Data;
using ProjectManager.DTOs;
using ProjectManager.Interfaces;
using ProjectManager.Models;
using System.Data;

namespace ProjectManager.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private readonly ApplicationDbContext _context;
        public TaskRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // -------------------------------------------------
        // Helper Methods
        private async Task<IEnumerable<TaskResponseDto>> FetchTasksWithDapper(string spName, object parameters)
        {
            using var connection = new SqlConnection(_context.Database.GetConnectionString());

            // 1. Dapper Fetch: Get raw flat data
            var flatTasks = await connection.QueryAsync<FlatTaskResult>(
                spName,
                parameters,
                commandType: CommandType.StoredProcedure
            );

            // 2. Map: Convert Flat Result -> Final DTO
            return flatTasks.Select(t => new TaskResponseDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status,
                Priority = t.Priority,
                DueDate = t.DueDate,
                CreatorId = t.CreatorId,
                ProjectId = t.ProjectId,
                AssignedUserId = t.AssignedUserId,

                ProjectName = t.ProjectName,
                AssignedUserName = t.AssignedUserFirstName != null
                    ? $"{t.AssignedUserFirstName} {t.AssignedUserLastName}"
                    : "Unassigned",

                // 3. Logic: Convert Comma-Separated Strings to Lists
                TagIds = !string.IsNullOrEmpty(t.TagIds)
                    ? t.TagIds.Split(',').Select(int.Parse).ToList()
                    : new List<int>(),

                Tags = !string.IsNullOrEmpty(t.TagNames)
                    ? t.TagNames.Split(',').ToList()
                    : new List<string>()
            });
        }

        // -------------------------------------------------


        public async Task<IEnumerable<TaskResponseDto>> GetDashboardTasksAsync(string? userId)
        {
            return await FetchTasksWithDapper("spGetDashboardTasks", new { UserId = userId });
        }

        public async Task<IEnumerable<TaskResponseDto>> GetProjectTasksAsync(int projectId, string? userId)
        {
            return await FetchTasksWithDapper("spGetProjectTasks", new { ProjectId = projectId, UserId = userId });
        }


        //-------------------------------------------------


        public async Task<ProjectTask> CreateTaskAsync(ProjectTask task, List<int> tagIds)
        {
            
            
            // Need to understand relationships and working with them more clearly
            if (tagIds != null && tagIds.Count > 0)
            {
                var tags = await _context.Tags
                    .Where(t => tagIds.Contains(t.Id))
                    .ToListAsync();
                foreach (var tag in tags)
                {
                    task.TaskTags.Add(new TaskTag
                    {
                        TagId = tag.Id
                    });
                }
            }

            var createTask = _context.Tasks.Add(task);
            var saveTask = await _context.SaveChangesAsync();

            return task;
        }

        //-------------------------------------------------
        


        // Get task by ID
        public async Task<ProjectTask?> GetTaskByIdAsync(int id)
        {
            return await _context.Tasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == id);
        }


        // Assign task to user
        public async Task<bool> AssignTaskToUserAsync (ProjectTask task, string newUserId)
        {
            task.AssignedUserId = newUserId;
            await _context.SaveChangesAsync();
            return true;
        }


        // Delete task
        public async Task<bool> DeleteTaskAsync(int taskId)
        {
            var task = await _context.Tasks.FindAsync(taskId);
            if (task == null)
            {
                return false;
            }
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            return true;
        }


        // Update task status
        public async Task<bool> UpdateTaskStatusAsync(ProjectTask task, string newStatus)
        {
            if (task == null)
            {
                return false;
            }
            task.Status = newStatus;
            await _context.SaveChangesAsync();
            return true;
        }


        // Update task
        public async Task<bool> UpdateTaskAsync(ProjectTask task)
        {
            _context.Tasks.Update(task);
            return await _context.SaveChangesAsync() > 0;
        }

        // Update Task Tags
        public async Task<bool> UpdateTaskTagsAsync(int taskId, List<int> tagIds)
        {
            // Get current links from DB
            var existingTags = await _context.TaskTags
                .Where(tt => tt.TaskId == taskId)
                .ToListAsync();

            // Remove tags that aren't in the new list
            var toRemove = existingTags.Where(et => !tagIds.Contains(et.TagId)).ToList();
            if (toRemove.Any()) _context.TaskTags.RemoveRange(toRemove);

            // Add tags that aren't already in the DB
            var existingIds = existingTags.Select(et => et.TagId).ToList();
            var toAdd = tagIds
                .Where(id => !existingIds.Contains(id))
                .Select(id => new TaskTag { TaskId = taskId, TagId = id })
                .ToList();

            if (toAdd.Any()) await _context.TaskTags.AddRangeAsync(toAdd);

            return await _context.SaveChangesAsync() > 0;
        }


    }
}
