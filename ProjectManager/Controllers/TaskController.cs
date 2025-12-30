using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Data;
using ProjectManager.DTOs;
using ProjectManager.Interfaces;
using ProjectManager.Models;
using System.Data.Common;
using System.Security.Claims;

namespace ProjectManager.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class TaskController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IProjectRepository _projectRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly IAuthorizationService _authorizationService;
        public TaskController(UserManager<ApplicationUser> userManager, ITaskRepository taskRepository, IProjectRepository projectRepository, IAuthorizationService authorizationService)
        {
            _userManager = userManager;
            _taskRepository = taskRepository;
            _projectRepository = projectRepository;
            _authorizationService = authorizationService;
        }

        // ---------------------------------------------------


        // Actions
        // Create Task inside a Project
        [Authorize(Roles = "Admin, Manager")]
        [HttpPost] // Route: api/Task
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto taskModel)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized("User not found.");
            }
            var projectId = taskModel.ProjectId;
            var project = await _projectRepository.GetProjectByIdAsync(projectId);

            if (project == null)
            {
                return NotFound($"Project with ID {projectId} not found.");
            }

            if (User.IsInRole("User"))
            {
                return Forbid("You do not have permission to create tasks.");
            }
            if (User.IsInRole("Manager"))
            {
                var isManagerOfProject = project.CreatorId == user.Id;
                if (!isManagerOfProject)
                {
                    return Forbid("Managers can only create tasks in projects they manage.");
                }
            }

            // Mapping DTO to Model
            var newTask = new ProjectTask
            {
                Title = taskModel.Title,
                Description = taskModel.Description,
                Priority = taskModel.Priority,
                DueDate = taskModel.DueDate,
                Status = taskModel.Status,
                ProjectId = taskModel.ProjectId,
                CreatorId = user.Id,
                AssignedUserId = taskModel.AssignedUserId
            };

            var result = await _taskRepository.CreateTaskAsync(newTask, taskModel.TagIds);

            if (result == null)
            {
                return StatusCode(500, "An error occurred while creating the task.");
            }
            
            return NoContent();

        }


        // --------------------------------------------------------------




        [HttpGet]
        public async Task<IActionResult> GetTasks([FromQuery] int? projectId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            IEnumerable<TaskResponseDto> tasks; // NOTE: Variable type is now the DTO

            if (!projectId.HasValue)
            {
                // Dashboard
                string? filterId = User.IsInRole("Admin") ? null : currentUserId;
                tasks = await _taskRepository.GetDashboardTasksAsync(filterId);
            }
            else
            {
                // Project Page
                string? filterId = (User.IsInRole("Admin") || User.IsInRole("Manager")) ? null : currentUserId;
                tasks = await _taskRepository.GetProjectTasksAsync(projectId.Value, filterId);
            }

            // Return directly - Mapping happened in the Repo via ADO.NET
            return Ok(tasks);
        }


        // --------------------------------------------------------------

        // Get Task by ID
        [HttpGet("{id}")] // Route: GET api/Task/{id}
        public async Task<IActionResult> GetTaskById(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var task = await _taskRepository.GetTaskByIdAsync(id);


            if (task == null)
            {
                return NotFound("Task not found.");
            }
            if (User.IsInRole("Admin") || User.IsInRole("Manager"))
            {

            }
            if (User.IsInRole("Member"))
            {
                if (task.AssignedUserId != userId)
                {
                    return StatusCode(403, new { message = "Members can only access tasks assigned to them." });
                }
            }

            var taskDto = new TaskResponseDto
                {
                    Id = task.Id,
                    Title = task.Title,
                    Description = task.Description,
                    DueDate = task.DueDate,
                    Priority = task.Priority,
                    Status = task.Status,
                    CreatorId = task.CreatorId,
                    ProjectId = task.ProjectId,
                    ProjectName = task.Project.Name,
                    AssignedUserId = task.AssignedUserId,
                    AssignedUserName = task.AssignedUser.FirstName + " " + task.AssignedUser.LastName,
                    Tags = task.TaskTags.Select(tt => tt.Tag.Name).ToList()
                };

            if (taskDto == null)
            {
                return NotFound("No records for such task are found.");
            }

            return Ok(taskDto);
        }


        // This action will result in replacing the previous assigned user with the new one.
        // Assigned Members, Managers of the Project, and Admins can re-assign tasks.
        [HttpPost("{id}/assign")] // Route: PUT /api/Task/{id}/assign
        public async Task<IActionResult> AssignUserToTask(int id, [FromBody] TaskAssignDto model)
        {
            var task = await _taskRepository.GetTaskByIdAsync(id);

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (task == null)
            {
                return NotFound(new { Message = "Task not found." });
            }
            // Authorization Check
            var authCheck = await _authorizationService.AuthorizeAsync(User, task, "CanModifyTask");
            if (!authCheck.Succeeded)
            {
                return StatusCode(403, "You do not have permission to re-assign this task.");
            }

            // Perform Re-assignment
            var result = await _taskRepository.AssignTaskToUserAsync(task, model.NewAssignedUserId);
            if (result)
            {
                return NoContent();
            }

            return StatusCode(500, "An error occurred while re-assigning the task.");

        }

        // Delete Task by ID
        [Authorize(Roles = "Admin, Manager")]
        [HttpDelete("{id}")] // Route: DELETE /api/Task/{id}
        public async Task<IActionResult> DeleteTask(int id)
        {
            var task = await _taskRepository.GetTaskByIdAsync(id);
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (task == null)
            {
                return NotFound(new { Message = "Task not found." });
            }

            if (task.Status == "In Progress")
            {
                return BadRequest("Cannot delete a task that is In Progress.");
            }

            // Admin Check: Full control
            if (User.IsInRole("Admin"))
            {
                // Admin is authorized, proceed to delete.
            }
            // Manager Check: Must be the creator/owner
            else if (User.IsInRole("Manager"))
            {
                if (task.CreatorId != currentUserId)
                {
                    return StatusCode(403, new { message = "Managers can only delete tasks they manage/created." } );
                }
            }
            else
            {
                // Should be caught by [Authorize], but as a final safeguard:
                return StatusCode(403);
            }

            var result = await _taskRepository.DeleteTaskAsync(id);
            if (!result)
            {
                return StatusCode(500, "An error occurred while deleting the task.");
            }
            
            return NoContent();
        }



        // Update basic Task details
        // Admins, Managers of the Project, and Assigned Members can update tasks.
        [HttpPut("{id}")] // Route: PUT /api/Task/{id}
        public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdateTaskDto taskModel)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Fetch Task
            var task = await _taskRepository.GetTaskByIdAsync(id);
            if (task == null) return NotFound(new { Message = "Task not found." });

            // Authorization (Gatekeeper)
            var authCheck = await _authorizationService.AuthorizeAsync(User, task, "CanModifyTask");
            if (!authCheck.Succeeded) return StatusCode(403, "Unauthorized");

            // Map DTO to Entity
            // We update the properties on the 'task' object we already have in memory
            task.Title = taskModel.Title;
            task.Description = taskModel.Description;
            task.Priority = taskModel.Priority;
            task.DueDate = taskModel.DueDate;
            task.Status = taskModel.Status;

            var success = await _taskRepository.UpdateTaskAsync(task);

            if (!success) return StatusCode(500, "Update failed.");

            return NoContent();
        }


        // Update Task Tags
        // PUT: api/Task/{taskId}/tags (Tags Only)
        [Authorize]
        [HttpPut("{id}/tags")]
        public async Task<IActionResult> UpdateTaskTags(int id, [FromBody] List<int> tagIds)
        {
            var task = await _taskRepository.GetTaskByIdAsync(id);
            if (task == null) return NotFound();

            // AUTH CHECK
            var authCheck = await _authorizationService.AuthorizeAsync(User, task, "CanModifyTask");
            if (!authCheck.Succeeded) return StatusCode(403, "Unauthorized");

            var success = await _taskRepository.UpdateTaskTagsAsync(id, tagIds);
            return success ? NoContent() : StatusCode(500);
        }


        // Update Task Status (for Member's ease - can be removed)
        [Authorize]
        [HttpPut("{id}/status")] // Route: PUT /api/Task/{id}/status
        public async Task<IActionResult> UpdateTaskStatusByMember(int id, [FromBody] ChangeTaskStatusDto statusDto)
        {
            var task = await _taskRepository.GetTaskByIdAsync(id);
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (task == null)
            {
                return NotFound(new { Message = "Task not found." });
            }
            // Authorization: Members can only change status of tasks assigned to them
            var authCheck = await _authorizationService.AuthorizeAsync(User, task, "CanModifyTask");
            if (!authCheck.Succeeded) return StatusCode(403, "Unauthorized");

            // Update status
            var result = await _taskRepository.UpdateTaskStatusAsync(task, statusDto.NewStatus);
            if (!result)
            {
                return StatusCode(500, "An error occurred while updating the task status.");
            }
            return NoContent();
        }

        // ------------------------------------------------------------------




    }
}
