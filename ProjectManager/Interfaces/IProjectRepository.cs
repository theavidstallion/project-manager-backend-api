using ProjectManager.Models;

namespace ProjectManager.Interfaces
{
    public interface IProjectRepository
    {
        Task<IEnumerable<ProjectResponseDto>> GetProjectsAsync(string? userId);
        Task<Project?> GetProjectByIdAsync(int id);

    }
}
