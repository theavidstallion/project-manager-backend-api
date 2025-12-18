using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Data;
using ProjectManager.DTOs;
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
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        public TaskController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
            var project = await _context.Projects
                .Include(p => p.ProjectUsers)
                .FirstOrDefaultAsync(p => p.Id == projectId);

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

            // Need to understand relationships and working with them more clearly
            if (taskModel.TagIds != null && taskModel.TagIds.Count > 0)
            {
                var tags = await _context.Tags
                    .Where(t => taskModel.TagIds.Contains(t.Id))
                    .ToListAsync();
                foreach (var tag in tags)
                {
                    newTask.TaskTags.Add(new TaskTag
                    {
                        TagId = tag.Id
                    });
                }
            }

            _context.Tasks.Add(newTask);
            await _context.SaveChangesAsync();

            return NoContent();

        }


        // Get Tasks
        [HttpGet] // Route: api/Tasks
        public async Task<IActionResult> GetTasksAsync()
        {
            // Fetch user ID once from the token (Corrected method)
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Start building the query
            IQueryable<ProjectTask> query = _context.Tasks;

            
            if (User.IsInRole("Admin") || User.IsInRole("Manager"))
            {
                // No filter applied to the query.
            }
            else if (User.IsInRole("Member"))
            {
                query = query.Where(t => t.Project.ProjectUsers.Any(pu => pu.UserId == currentUserId));
            }
            else
            {
                return Forbid("Access denied: Insufficient role permissions.");
            }

            var taskDtos = await query
                .Select(t => new TaskResponseDto // Use a projection to load only necessary fields
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    Status = t.Status,
                    Priority = t.Priority,
                    DueDate = t.DueDate,
                    CreatorId = t.CreatorId,
                    ProjectId = t.ProjectId,
                    // Map related data using navigation properties (EF performs the joins)
                    ProjectName = t.Project.Name,
                    AssignedUserName = t.AssignedUser.FirstName + " " + t.AssignedUser.LastName,
                    AssignedUserId = t.AssignedUserId,

                    // Example of mapping Tags
                    Tags = t.TaskTags.Select(tt => tt.Tag.Name).ToList()
                })
                .ToListAsync();

            return Ok(taskDtos);
        }


        // Get Task by ID
        [HttpGet("{id}")] // Route: GET api/Task/{id}
        public async Task<IActionResult> GetTaskById(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            IQueryable<ProjectTask> query = _context.Tasks;

            if (User.IsInRole("Admin") || User.IsInRole("Manager"))
            {

            }
            if (User.IsInRole("Member"))
            {
                query = query.Where(t => t.AssignedUserId == userId);
            }

            var taskDto = await query
                .Where(t => t.Id == id)
                .Select(t => new TaskResponseDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    DueDate = t.DueDate,
                    Priority = t.Priority,
                    Status = t.Status,
                    CreatorId = t.CreatorId,
                    ProjectId = t.ProjectId,
                    ProjectName = t.Project.Name,
                    AssignedUserId = t.AssignedUserId,
                    AssignedUserName = t.AssignedUser.FirstName + " " + t.AssignedUser.LastName,
                    Tags = t.TaskTags.Select(tt => tt.Tag.Name).ToList()
                }).FirstOrDefaultAsync();

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
            var task = await _context.Tasks
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == id);

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
            task.AssignedUserId = model.NewAssignedUserId;
            await _context.SaveChangesAsync();
            return NoContent();

        }


        // Delete Task by ID
        [Authorize(Roles = "Admin, Manager")]
        [HttpDelete("{id}")] // Route: DELETE /api/Task/{id}
        public async Task<IActionResult> DeleteTask(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
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
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            // NOTE: Returning NoContent() (204) is the standard for successful DELETE.
            return NoContent();
        }


        // Update Task by ID, including Tags
        // Admins, Managers of the Project, and Assigned Members can update tasks.
        [HttpPut("{id}")] // Route: PUT /api/Task/{id}
        public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdateTaskDto taskModel)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. Fetch Task and Project for Authorization Check
            var task = await _context.Tasks
                .Include(t => t.Project)  // Need Project to check Manager ownership
                .Include(t => t.TaskTags) // Include existing tags
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound(new { Message = "Task not found." });
            }

            if (User.IsInRole("Member"))
            {
                if (currentUserId != task.AssignedUserId)
                {
                    return StatusCode(403, new { message = "Members who are not assigned users are not authorized to update tasks." });
                } 
            }
            // 2. AUTHORIZATION CHECK (Admin/Manager)

            // Only check ownership if the user is a Manager AND NOT an Admin.
            if (User.IsInRole("Manager") && !User.IsInRole("Admin"))
            {
                // Check if the Manager is the Creator/Manager of the PROJECT this task belongs to.
                if (task.Project.CreatorId != currentUserId)
                {
                    return StatusCode(403, new { message = "Managers can only update tasks within projects they manage." });
                }
            }

            // 3. Update Core Task Properties

            // We explicitly exclude AssignedUserId and Status from this action.
            task.Title = taskModel.Title;
            task.Description = taskModel.Description;
            task.Priority = taskModel.Priority;
            task.DueDate = taskModel.DueDate;
            task.Status = taskModel.Status;       

            // 4. Update Tags (FIXED ARRAY MANIPULATION)
            if (taskModel.TagIds != null)
            {
                // REMOVE EXISTING TAGS: Find all link entities to remove
                var tagsToRemove = task.TaskTags
                    .Where(tt => !taskModel.TagIds.Contains(tt.TagId))
                    .ToList(); // Materialize the list for removal

                foreach (var tagLink in tagsToRemove)
                {
                    task.TaskTags.Remove(tagLink);
                }

                // ADD NEW TAGS
                var existingTagIds = task.TaskTags.Select(tt => tt.TagId).ToHashSet();
                var newTagIds = taskModel.TagIds.Where(tagId => !existingTagIds.Contains(tagId)).ToList();

                foreach (var tagId in newTagIds)
                {
                    task.TaskTags.Add(new TaskTag { TagId = tagId });
                }
            }

            // 5. Save Changes
            await _context.SaveChangesAsync();
            return NoContent();
        }


        // Change Task Status for Member role
        [HttpPut("{id}/status")] // Route: PUT /api/Task/{id}/status
        public async Task<IActionResult> ChangeTaskStatus(int id, [FromBody] ChangeTaskStatusDto statusDto)
        {
            var task = await _context.Tasks.FindAsync(id);
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
            else if (User.IsInRole("Admin") || User.IsInRole("Manager"))
            {
                // Admins and Managers can change status of any task
            }
            else
            {
                return StatusCode(403, new { message = "Access denied: Insufficient role permissions." } );
            }
            task.Status = statusDto.NewStatus;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Add Tags to Task for Member role
        [HttpPost("{id}/tags")] // Route: POST /api/Task/{id}/tags
        public async Task<IActionResult> AddTagsToTask(int id, [FromBody] AddTagsDto tagsDto)
        {
            var task = await _context.Tasks
                .Include(t => t.TaskTags)
                .FirstOrDefaultAsync(t => t.Id == id);
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (task == null)
            {
                return NotFound(new { Message = "Task not found." });
            }
            // Authorization: Members can only add tags to tasks assigned to them
            if (User.IsInRole("Member"))
            {
                if (task.AssignedUserId != currentUserId)
                {
                    return StatusCode(403, new { message = "Members can only add tags to tasks assigned to them." } );
                }
            }
            else if (User.IsInRole("Admin"))
            {
                // Admins and Managers can add tags to any task
            }
            else if (User.IsInRole("Manager"))
            {
                // Managers can add tags only if they manage the project
                var project = await _context.Projects.FindAsync(task.ProjectId);
                if (project.CreatorId != currentUserId)
                {
                    return StatusCode(403, new { message = "Managers can only add tags to tasks within projects they manage." } );
                }
            }
            else
            {
                return StatusCode(403, new { message = "Access denied: Insufficient role permissions." });
            }
            // Add new tags, avoiding duplicates
            var existingTagIds = task.TaskTags.Select(tt => tt.TagId).ToHashSet();
            foreach (var tagId in tagsDto.TagIds)
            {
                if (!existingTagIds.Contains(tagId))
                {
                    task.TaskTags.Add(new TaskTag { TagId = tagId });
                }
            }
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Update Task Status for member role only
        [Authorize]
        [HttpPut("{id}/member/status")] // Route: PUT /api/Task/{id}/member/status
        public async Task<IActionResult> UpdateTaskStatusByMember(int id, [FromBody] ChangeTaskStatusDto statusDto)
        {
            var task = await _context.Tasks.FindAsync(id);
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
            task.Status = statusDto.NewStatus;
            await _context.SaveChangesAsync();
            return NoContent();
        }




    }
}
