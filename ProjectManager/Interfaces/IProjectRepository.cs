using ProjectManager.Models;

namespace ProjectManager.Interfaces
{
    public interface IProjectRepository
    {
        Task<Project?> GetProjectByIdAsync(int id);

        Task<IEnumerable<Project>> GetProjectsByUserIdAsync(string userId);

        Task<IEnumerable<Project>> GetAllProjectsAsync();

    }
}
