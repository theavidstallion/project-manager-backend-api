using ProjectManager.DTOs;
using ProjectManager.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace ProjectManager.Services
{
    public interface IProjectMappingService
    {
        IEnumerable<ProjectResponseDto> TransformFlatRowsToDtos(IEnumerable<FlatProjectResult> flatRows);
    }

    public class ProjectMappingService : IProjectMappingService
    {
        public IEnumerable<ProjectResponseDto> TransformFlatRowsToDtos(IEnumerable<FlatProjectResult> flatRows)
        {
            if (flatRows == null || !flatRows.Any())
                return new List<ProjectResponseDto>();

            return flatRows
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

                        Members = g.Where(row => !string.IsNullOrEmpty(row.MemberId))
                                    .Select(row => new ProjectMemberDto
                                    {
                                        UserId = row.MemberId,
                                        FirstName = row.MemberFirstName,
                                        LastName = row.MemberLastName,
                                        Email = row.MemberEmail
                                    })
                                    .DistinctBy(m => m.UserId) // Safety check for duplicates
                                    .ToList()
                    };
                });
        }
    }
}
