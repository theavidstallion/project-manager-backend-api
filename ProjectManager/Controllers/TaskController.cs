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
        public TaskController(UserManager<ApplicationUser> userManager, ITaskRepository taskRepository, IProjectRepository projectRepository)
        {
            _userManager = userManager;
            _taskRepository = taskRepository;
            _projectRepository = projectRepository;
        }


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
            if (User.IsInRole("User"))
            {
                return Forbid("You do not have permission to create tasks.");
            }

            var projectId = taskModel.ProjectId;
            var project = await _projectRepository.GetProjectByIdAsync(projectId);

            if (project == null)
            {
                return NotFound($"Project with ID {projectId} not found.");
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

        // Get Tasks - with filtering based on Role and optional ProjectId
        [HttpGet] // Route: api/Task?projectId=5 or just api/Task
        public async Task<IActionResult> GetTasksAsync([FromQuery] int? projectId)
        {
            // 1. Get User Identity
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // 2. Decide which Repository method to call
            IEnumerable<ProjectTask> tasks;

            if (projectId.HasValue)
            {
                // --- PROJECT PAGE VIEW ---
                if (User.IsInRole("Admin") || User.IsInRole("Manager"))
                {
                    // Admins and Managers see everything in the project
                    tasks = await _taskRepository.GetTasksByProjectId(projectId.Value);
                }
                else
                {
                    // Members see only what is assigned to them in this project
                    tasks = await _taskRepository.GetUserTasksByProjectId(userId, projectId.Value);
                }
            }
            else
            {
                // --- DASHBOARD VIEW ---
                if (User.IsInRole("Admin"))
                {
                    tasks = await _taskRepository.GetAllTasksAsync();
                }
                else if (User.IsInRole("Manager"))
                {
                    // Managers see tasks from projects they created
                    tasks = await _taskRepository.GetTasksByProjectManagerIdAsync(userId);
                }
                else
                {
                    // Members see all tasks assigned to them across all projects
                    tasks = await _taskRepository.GetAssignedTasksAsync(userId);
                }
            }

            // 3. One single mapping procedure for all results
            var taskDtos = tasks.Select(t => new TaskResponseDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status,
                Priority = t.Priority,
                DueDate = t.DueDate,
                CreatorId = t.CreatorId,
                ProjectId = t.ProjectId,
                ProjectName = t.Project.Name,
                AssignedUserName = t.AssignedUser != null
                    ? $"{t.AssignedUser.FirstName} {t.AssignedUser.LastName}"
                    : "Unassigned",
                AssignedUserId = t.AssignedUserId,
                Tags = t.TaskTags.Select(tt => tt.Tag.Name).ToList()
            }).ToList();

            return Ok(taskDtos);
        }


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
            if (User.IsInRole("Admin"))
            {
                // Admins can re-assign any task
            }
            else if (User.IsInRole("Manager"))
            {
                // Managers can re-assign tasks only within projects they manage
                if (task.Project.CreatorId != currentUserId)
                {
                    return StatusCode(403, new { message = "Managers can only re-assign tasks within projects they manage." });
                }
            }
            else if (User.IsInRole("Member"))
            {
                // Assigned Members can re-assign their own tasks
                if (task.AssignedUserId != currentUserId)
                {
                    return StatusCode(403, new { message = "Members can only re-assign tasks assigned to them." });
                }
            }
            else
            {
                return StatusCode(403, new { message = "Access denied: Insufficient role permissions." });
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



        // Update Task by ID, Tags are also updated here - collectively and independently. (Maybe change later)
        // Admins, Managers of the Project, and Assigned Members can update tasks.
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdateTaskDto taskModel)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. Fetch Task
            var task = await _taskRepository.GetTaskByIdAsync(id);
            if (task == null) return NotFound(new { Message = "Task not found." });

            // 2. Authorization (Gatekeeper)
            if (User.IsInRole("Member") && currentUserId != task.AssignedUserId)
            {
                return StatusCode(403, new { message = "Unauthorized." });
            }
            if (User.IsInRole("Manager") && !User.IsInRole("Admin") && task.Project?.CreatorId != currentUserId)
            {
                return StatusCode(403, new { message = "Unauthorized." });
            }

            // 3. Map DTO to Entity
            // We update the properties on the 'task' object we already have in memory
            task.Title = taskModel.Title;
            task.Description = taskModel.Description;
            task.Priority = taskModel.Priority;
            task.DueDate = taskModel.DueDate;
            task.Status = taskModel.Status;

            // 4. Delegate the "Scary" logic and the Database Save to the Repo
            var success = await _taskRepository.UpdateTaskAsync(task, taskModel.TagIds);

            if (!success) return StatusCode(500, "Update failed.");

            return NoContent();
        }


        // Update Task Status for member role only
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
            if (User.IsInRole("Member"))
            {
                if (task.AssignedUserId != currentUserId)
                {
                    return StatusCode(403, new { message = "Members can only change status of tasks assigned to them." } );
                }
            }
            else
            {
                return StatusCode(403, new { message = "Access denied: Insufficient role permissions." } );
            }

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
