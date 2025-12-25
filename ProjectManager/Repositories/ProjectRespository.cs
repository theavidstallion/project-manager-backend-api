using ProjectManager.Interfaces;
using ProjectManager.Models;
using ProjectManager.Data;
using Microsoft.EntityFrameworkCore;

namespace ProjectManager.Repositories
{
    public class ProjectRespository : IProjectRepository
    {
        private readonly ApplicationDbContext _context;
        public ProjectRespository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Base handler
        private IQueryable<Project> GetBaseProjectQuery()
        {
            return _context.Projects
                .Include(p => p.Creator)    // Can ignore, as we have CreatorId and name as explicit fields
                .Include(p => p.ProjectUsers)
                    .ThenInclude(pu => pu.User);
        }

        public async Task<IEnumerable<Project>> GetAllProjectsAsync()
        {
            return await GetBaseProjectQuery().ToListAsync();
        }

        public async Task<IEnumerable<Project>> GetProjectsByUserIdAsync(string userId)
        {
            return await GetBaseProjectQuery()
                .Where(p => p.ProjectUsers.Any(pu => pu.UserId == userId))
                .ToListAsync();
        }

        public async Task<Project?> GetProjectByIdAsync(int id)
        {
            return await GetBaseProjectQuery()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

    }
}
