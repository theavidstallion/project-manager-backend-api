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

    public class ProjectRepository : IProjectRepository
    {
        private readonly ApplicationDbContext _context;

        public ProjectRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // NOTE: Return type changed to the DTO
        public async Task<IEnumerable<ProjectResponseDto>> GetProjectsAsync(string? userId)
        {
            using var connection = new SqlConnection(_context.Database.GetConnectionString());

            // 1. Dapper Fetch: One line replaces DataTable + Adapter + JSON conversion
            var flatData = await connection.QueryAsync<FlatProjectResult>(
                "spGetProjects",
                new { UserId = userId },
                commandType: CommandType.StoredProcedure
            );

            // 2. Grouping & Mapping: Turn flat rows into nested DTOs
            var result = flatData
                .GroupBy(p => p.Id)
                .Select(g =>
                {
                    var project = g.First();
                    return new ProjectResponseDto
                    {
                        Id = project.Id,
                        Name = project.Name,
                        Description = project.Description,
                        StartDate = project.StartDate,
                        EndDate = project.EndDate,
                        Status = project.Status,
                        CreatorId = project.CreatorId,
                        CreatorName = project.CreatorName,

                        // Handle the list of members
                        Members = g.Where(m => !string.IsNullOrEmpty(m.MemberId))
                                   .Select(m => new ProjectMemberDto
                                   {
                                       UserId = m.MemberId,
                                       FirstName = m.MemberFirstName,
                                       LastName = m.MemberLastName,
                                       Email = m.MemberEmail
                                   }).ToList()
                    };
                });

            return result;
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
