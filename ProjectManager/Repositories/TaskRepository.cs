using ProjectManager.Models;
using ProjectManager.Data;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Interfaces;

namespace ProjectManager.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private readonly ApplicationDbContext _context;
        public TaskRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // "Relationship FIX-UP" HELPER 
        private async Task PopulateTaskDataAsync(List<ProjectTask> tasks)
        {
            if (!tasks.Any()) return;

            var taskIds = tasks.Select(t => t.Id).ToList();

            // Load Projects (to know which project the task belongs to)
            var projectIds = tasks.Select(t => t.ProjectId).Distinct().ToList();
            await _context.Projects.Where(p => projectIds.Contains(p.Id)).LoadAsync();

            // Load Assigned Users
            var userIds = tasks.Select(t => t.AssignedUserId).Distinct().ToList();
            await _context.Users.Where(u => userIds.Contains(u.Id)).LoadAsync();

            // Load Tags (Many-to-Many)
            await _context.TaskTags
                .Include(tt => tt.Tag)
                .Where(tt => taskIds.Contains(tt.TaskId))
                .LoadAsync();
        }

        public async Task<IEnumerable<ProjectTask>> GetDashboardTasksAsync(string? userId)
        {
            var tasks = await _context.Tasks
                .FromSqlInterpolated($"EXEC dbo.spGetDashboardTasks @UserId = {userId}")
                .ToListAsync();

            await PopulateTaskDataAsync(tasks);
            return tasks;
        }

        public async Task<IEnumerable<ProjectTask>> GetProjectTasksAsync(int? projectId, string? userId)
        {
            var tasks = await _context.Tasks
                .FromSqlInterpolated($"EXEC dbo.spGetProjectTasks @ProjectId = {projectId}, @UserId = {userId}")
                .ToListAsync();

            await PopulateTaskDataAsync(tasks);
            return tasks;
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
