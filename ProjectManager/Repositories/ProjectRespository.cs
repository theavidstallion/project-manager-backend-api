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

        public async Task<IEnumerable<Project>> GetProjectsAsync(string? userId)
        {
            // 1. Execute the Stored Procedure (The "Gatekeeper") - It filters projects
            // Can't use Include() with FromSqlInterpolated() or whatever the heck was the reason, so we do it in two steps.
            var projects = await _context.Projects
                .FromSqlInterpolated($"EXEC dbo.spGetProjects @UserId = {userId}")
                .ToListAsync();

            if (!projects.Any()) return projects;

            // 2. Fetch the Members
            // We only fetch members for the specific projects returned by the SP.
            var projectIds = projects.Select(p => p.Id).ToList();

            await _context.ProjectUsers
                .Include(pu => pu.User)
                .Where(pu => projectIds.Contains(pu.ProjectId))
                .LoadAsync(); // This "snaps" the users into the project objects in RAM

            return projects;
        }


        public async Task<Project?> GetProjectByIdAsync(int id)
        {
            return await _context.Projects
                .Include(p => p.ProjectUsers)
                    .ThenInclude(pu => pu.User)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

    }
}
