using ProjectManager.DTOs;

namespace ProjectManager.Services
{
    public interface ITaskMappingService
    {
        IEnumerable<TaskResponseDto> TransformFlatDataToDto (IEnumerable<FlatTaskResult> flatData);
    }

    public class TaskMappingService : ITaskMappingService
    {
        public IEnumerable<TaskResponseDto> TransformFlatDataToDto (IEnumerable<FlatTaskResult> flatData)
        {
            if (flatData == null) return new List<TaskResponseDto>();

            var tasksDto = flatData.Select(t => new TaskResponseDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status,
                Priority = t.Priority,
                DueDate = t.DueDate,
                CreatorId = t.CreatorId,
                ProjectId = t.ProjectId,
                AssignedUserId = t.AssignedUserId,

                ProjectName = t.ProjectName,
                AssignedUserName = t.AssignedUserFirstName != null
                ? $"{t.AssignedUserFirstName} {t.AssignedUserLastName}"
                : "Unassigned",

                // 3. Logic: Convert Comma-Separated Strings to Lists
                TagIds = !string.IsNullOrEmpty(t.TagIds)
                ? t.TagIds.Split(',').Select(int.Parse).ToList()
                : new List<int>(),

                Tags = !string.IsNullOrEmpty(t.TagNames)
                ? t.TagNames.Split(',').ToList()
                : new List<string>()
            });


            return tasksDto;

        }
    }


}
