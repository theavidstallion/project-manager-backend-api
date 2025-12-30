using ProjectManager.DTOs;
using ProjectManager.Models;

namespace ProjectManager.Interfaces
{
    public interface ITaskRepository
    {
        Task<ProjectTask> CreateTaskAsync(ProjectTask task, List<int> tagIds);
        
        Task<ProjectTask?> GetTaskByIdAsync(int id);

        // -------------------------------------------
        Task<IEnumerable<TaskResponseDto>> GetProjectTasksAsync(int projectId, string? userId);

        Task<IEnumerable<TaskResponseDto>> GetDashboardTasksAsync(string? userId);

        // -------------------------------------------

        // Assign task to user
        Task<bool> AssignTaskToUserAsync(ProjectTask task, string newUserId);

        //Task<ProjectTask> UpdateTaskAsync(ProjectTask task);
        Task<bool> DeleteTaskAsync(int taskId);

        Task<bool> UpdateTaskStatusAsync(ProjectTask task, string newStatus);

        Task<bool> UpdateTaskAsync(ProjectTask task);

        Task<bool> UpdateTaskTagsAsync(int taskId, List<int> tagIds);


    }
}
