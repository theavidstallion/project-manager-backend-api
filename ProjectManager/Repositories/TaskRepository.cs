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

            // 1. Load Projects (So we know which project the task belongs to)
            var projectIds = tasks.Select(t => t.ProjectId).Distinct().ToList();
            await _context.Projects.Where(p => projectIds.Contains(p.Id)).LoadAsync();

            // 2. Load Assigned Users (So we see names/emails)
            var userIds = tasks.Select(t => t.AssignedUserId).Distinct().ToList();
            await _context.Users.Where(u => userIds.Contains(u.Id)).LoadAsync();

            // 3. Load Tags (Many-to-Many)
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
            return await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id);
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
        public async Task<bool> UpdateTaskAsync(ProjectTask task, List<int> newTagIds)
        {
            // Handle Tag Synchronization (Stupid complex many-to-many relationships in EF Core - need practice)
            if (newTagIds != null)
            {
                // Find links that exist in DB but aren't in the new list (Remove these)
                var tagsToRemove = task.TaskTags
                    .Where(tt => !newTagIds.Contains(tt.TagId))
                    .ToList();

                if (tagsToRemove.Any())
                {
                    _context.TaskTags.RemoveRange(tagsToRemove);
                }

                // Find IDs in the new list that don't have links yet (Add these)
                var existingTagIds = task.TaskTags.Select(tt => tt.TagId).ToHashSet();
                var tagsToAdd = newTagIds
                    .Where(id => !existingTagIds.Contains(id))
                    .Select(id => new TaskTag { TaskId = task.Id, TagId = id })
                    .ToList();

                if (tagsToAdd.Any())
                {
                    await _context.TaskTags.AddRangeAsync(tagsToAdd);
                }
            }

            // 2. Save the whole transaction (automatically handles both scalar updates and many-to-many changes due to EF Core tracking)
            return await _context.SaveChangesAsync() >= 0;
        }



    }
}
