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


        public Task<Project?> GetProjectById(int id)
        {
            var project = _context.Projects
                 .Include(p => p.ProjectUsers)
                 .FirstOrDefaultAsync(p => p.Id == id);

            return project;
        }
    }
}
