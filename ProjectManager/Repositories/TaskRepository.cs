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

        // --- Dashboard Method ---
        public async Task<IEnumerable<TaskResponseDto>> GetDashboardTasksAsync(string? userId)
        {
            var parameters = new[]
            {
            new SqlParameter("@UserId", userId ?? (object)DBNull.Value)
        };

            return await ExecuteStoredProcAsync("spGetDashboardTasks", parameters);
        }

        // --- Project Page Method ---
        public async Task<IEnumerable<TaskResponseDto>> GetProjectTasksAsync(int projectId, string? userId)
        {
            var parameters = new[]
            {
            new SqlParameter("@ProjectId", projectId),
            new SqlParameter("@UserId", userId ?? (object)DBNull.Value)
        };

            return await ExecuteStoredProcAsync("spGetProjectTasks", parameters);
        }

        // --- Helper to DRY up the ADO.NET logic ---
        private async Task<List<TaskResponseDto>> ExecuteStoredProcAsync(string spName, SqlParameter[] parameters)
        {
            var tasks = new List<TaskResponseDto>();
            var dt = new DataTable();

            using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
            using (var command = new SqlCommand(spName, connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddRange(parameters);

                using (var adapter = new SqlDataAdapter(command))
                {
                    connection.Open();
                    adapter.Fill(dt);
                }
            }

            foreach (DataRow row in dt.Rows)
            {
                tasks.Add(MapRowToDto(row));
            }

            return tasks;
        }

        // --- Helper to Map DataRow to DTO ---
        private TaskResponseDto MapRowToDto(DataRow row)
        {
            return new TaskResponseDto
            {
                Id = Convert.ToInt32(row["Id"]),
                Title = row["Title"].ToString(),
                Description = row["Description"] != DBNull.Value ? row["Description"].ToString() : "",
                Status = row["Status"].ToString(),
                Priority = row["Priority"].ToString(),
                DueDate = Convert.ToDateTime(row["DueDate"]),
                CreatorId = row["CreatorId"].ToString(),
                ProjectId = Convert.ToInt32(row["ProjectId"]),
                AssignedUserId = row["AssignedUserId"] != DBNull.Value ? row["AssignedUserId"].ToString() : null,

                // New Mapped Columns
                ProjectName = row["ProjectName"].ToString(),
                AssignedUserName = row["AssignedUserFirstName"] != DBNull.Value
                    ? $"{row["AssignedUserFirstName"]} {row["AssignedUserLastName"]}"
                    : "Unassigned",

                // Tag Parsing (Splitting the comma-separated string)
                TagIds = row["TagIds"] != DBNull.Value
                    ? row["TagIds"].ToString().Split(',').Select(int.Parse).ToList()
                    : new List<int>(),

                Tags = row["TagNames"] != DBNull.Value
                    ? row["TagNames"].ToString().Split(',').ToList()
                    : new List<string>()
            };
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
