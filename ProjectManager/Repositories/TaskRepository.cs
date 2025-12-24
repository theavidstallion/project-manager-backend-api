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

    }
}
