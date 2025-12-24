using ProjectManager.Models;

namespace ProjectManager.Interfaces
{
    public interface ITaskRepository
    {
        Task<ProjectTask> CreateTaskAsync(ProjectTask task, List<int> tagIds);
        //Task<ProjectTask?> GetTaskByIdAsync(int id);
        //Task<IEnumerable<ProjectTask>> GetAllTasksAsync();
        //Task<ProjectTask> UpdateTaskAsync(ProjectTask task);
        //Task<bool> DeleteTaskAsync(int id);

    }
}
