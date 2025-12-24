using ProjectManager.Models;

namespace ProjectManager.Interfaces
{
    public interface IProjectRepository
    {
        Task<Project?> GetProjectById(int id);
    }
}
