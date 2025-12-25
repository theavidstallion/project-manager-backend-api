using ProjectManager.Models;

namespace ProjectManager.Interfaces
{
    public interface ITaskRepository
    {
        Task<ProjectTask> CreateTaskAsync(ProjectTask task, List<int> tagIds);
        
        Task<ProjectTask?> GetTaskByIdAsync(int id);


        // Tasks on Dashboard page for admins
        Task<IEnumerable<ProjectTask>> GetAllTasksAsync();

        // Tasks on Dashboard page for managers
        Task<IEnumerable<ProjectTask>> GetTasksByProjectManagerIdAsync(string creatorId);

        // Tasks on Dashboard page for members
        Task<IEnumerable<ProjectTask>> GetAssignedTasksAsync(string userId);


        // Tasks inside Project page for admins and managers
        Task<IEnumerable<ProjectTask>> GetTasksByProjectId(int projectId);

        // Tasks inside Project page for members 
        Task<IEnumerable<ProjectTask>> GetUserTasksByProjectId(string userId, int projectId);

        // Assign task to user
        Task<bool> AssignTaskToUserAsync(ProjectTask task, string newUserId);

        //Task<ProjectTask> UpdateTaskAsync(ProjectTask task);
        Task<bool> DeleteTaskAsync(int taskId);

        Task<bool> UpdateTaskStatusAsync(ProjectTask task, string newStatus);

        Task<bool> UpdateTaskAsync(ProjectTask task, List<int> tagIds);


    }
}
