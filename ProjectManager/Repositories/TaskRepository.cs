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

        // Helper method to avoid repeating Include logic for every method
        private IQueryable<ProjectTask> GetBaseTaskQuery()
        {
            return _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedUser)
                .Include(t => t.TaskTags)
                    .ThenInclude(tt => tt.Tag);
        }

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
        // TASKS ON DASHBOARD
        // For admins on dashboard
        public async Task<IEnumerable<ProjectTask>> GetAllTasksAsync()
        {
            return await GetBaseTaskQuery()
                .ToListAsync();
        }
        // For project managers on dashboard
        public async Task<IEnumerable<ProjectTask>> GetTasksByProjectManagerIdAsync(string creatorId)
        {
            return await GetBaseTaskQuery()
                .Where(t => t.Project.CreatorId == creatorId)
                .ToListAsync();
        }
        // Get all tasks for a specific user on dashboard
        public async Task<IEnumerable<ProjectTask>> GetAssignedTasksAsync (string userId)
        {
            return await GetBaseTaskQuery()
                .Where(t => t.AssignedUserId == userId)
                .ToListAsync();
        }
        //-------------------------------------------------


        //-------------------------------------------------
        // TASKS INSIDE A PROJECT PAGE
        // For admins and project managers inside a project page
        public async Task<IEnumerable<ProjectTask>> GetTasksByProjectId(int projectId)
        {
            return await GetBaseTaskQuery()
                .Where(t => t.ProjectId == projectId)
                .ToListAsync();
        }
        // For members inside a project page
        public async Task<IEnumerable<ProjectTask>> GetUserTasksByProjectId(string userId, int projectId)
        {
            return await GetBaseTaskQuery()
                .Where(t => t.AssignedUserId == userId && t.ProjectId == projectId)
                .ToListAsync();
        }
        //-------------------------------------------------


        // Get task by ID
        public async Task<ProjectTask?> GetTaskByIdAsync(int id)
        {
            return await GetBaseTaskQuery().FirstOrDefaultAsync(t => t.Id == id);
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
