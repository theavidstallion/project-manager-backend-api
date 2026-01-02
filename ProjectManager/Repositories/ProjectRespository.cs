using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Data;
using ProjectManager.DTOs;
using ProjectManager.Interfaces;
using ProjectManager.Models;
using ProjectManager.Services;
using System.Data;

namespace ProjectManager.Repositories
{

    public class ProjectRepository : IProjectRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IProjectMappingService _mapper;

        public ProjectRepository(ApplicationDbContext context, IProjectMappingService mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // NOTE: Return type changed to the DTO
        public async Task<IEnumerable<ProjectResponseDto>> GetProjectsAsync(string? userId)
        {
            using var connection = new SqlConnection(_context.Database.GetConnectionString());

            // 2. Fetch Data (The "Raw Material")
            var flatData = await connection.QueryAsync<FlatProjectResult>(
                "spGetProjects",
                new { UserId = userId },
                commandType: CommandType.StoredProcedure
            );

            // 3. Transform Data (The "Factory")
            return _mapper.TransformFlatRowsToDtos(flatData);
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
