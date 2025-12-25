using ProjectManager.Models;

namespace ProjectManager.Interfaces
{
    public interface IProjectRepository
    {
        Task<Project?> GetProjectByIdAsync(int id);

        Task<IEnumerable<Project>> GetProjectsAsync(string? userId);


    }
}
