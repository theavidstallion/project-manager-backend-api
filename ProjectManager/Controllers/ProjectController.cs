using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Data;
using ProjectManager.DTOs;
using ProjectManager.Models;
using System.Security.Claims;
using ProjectManager.Interfaces;

namespace ProjectManager.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")] // Route: api/Project
    public class ProjectController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IProjectRepository _projectRepository;
        private readonly IAuthorizationService _authorizationService;

        public ProjectController(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IProjectRepository projectRepository, IAuthorizationService authorizationService)
        {
            _userManager = userManager;
            _context = context;
            _projectRepository = projectRepository;
            _authorizationService = authorizationService;
        }


        // Route: GET api/Project
        [HttpGet]
        public async Task<IActionResult> GetProjects()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. DECISION LOGIC: Determine what the SP needs
            // If Admin/Manager, we send null (Get All).
            // If Member, we send their ID (Filtered).
            string? filterId = (User.IsInRole("Admin") || User.IsInRole("Manager"))
                ? null
                : currentUserId;

            var projects = await _projectRepository.GetProjectsAsync(filterId);

            // 2. MAPPING: Entity -> DTO
            var projectDtos = projects.Select(project => new ProjectResponseDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                Status = project.Status,
                CreatorId = project.CreatorId,
                CreatorName = project.CreatorName,

                Members = project.ProjectUsers.Select(pu => new ProjectMemberDto
                {
                    UserId = pu.UserId,
                    FirstName = pu.User?.FirstName,
                    LastName = pu.User?.LastName,
                    Email = pu.User?.Email
                }).ToList()
            }).ToList();

            return Ok(projectDtos);
        }






        // Create Project
        // Route: POST /api/Project
        [Authorize(Roles = "Admin, Manager")]
        [HttpPost]  
        public async Task <IActionResult> CreateProject ([FromBody] CreateProjectDto model)
        {
            var creatorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var creator = await _userManager.FindByIdAsync(creatorId);
            if (creator == null)
            {
                return Unauthorized("User not found.");
            }

            var project = new Project
            {
                Name = model.Name,
                Description = model.Description,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Status = model.Status,
                CreatorId = creator.Id,
                CreatorName = creator.FirstName
            };

            // Initiate Project Membership (Many-to-Many relationship)
            // Create the join entity linking the new project to the user who created it.
            var projectUser = new ProjectUser
            {
                UserId = creator.Id
            };

            project.ProjectUsers.Add(projectUser);


            try
            {
                _context.Projects.Add(project);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while creating the project: " + ex.Message);
            }

            // Frontend will handle the redirection and project details display.
            return Ok(new { Message = "Project created successfully."});

        }


        // Get Project by ID with Role-Based Access Control
        // Route: api/Project/{id}
        [HttpGet("{id}")] 
        public async Task<IActionResult> GetProjectById(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. Fetch the project and EAGER LOAD necessary user data:
            var project = await _context.Projects
                // Eager load the Creator details (One-to-One/Many relationship)
                .Include(p => p.Creator)

                // Eager load the members (Many-to-Many join) and THEN the actual User object
                .Include(p => p.ProjectUsers)
                    .ThenInclude(pu => pu.User) // Loads the ApplicationUser object for names

                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return NotFound($"Project with ID {id} not found.");
            }

            // ENFORCE VIEWING PERMISSIONS (Logic remains the same)
            var authCheck = await _authorizationService.AuthorizeAsync(User, project, "CanViewProject");
            if (!authCheck.Succeeded)
            {
                return StatusCode(403, "Unauthorized.");
            }

            // 3. Map Entity to DTO (Fixing the data display)
            var responseDto = new ProjectResponseDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                Status = project.Status,
                CreatorId = project.CreatorId,

                // Populate CreatorName using the loaded Creator entity
                CreatorName = project.Creator.FirstName, // Assuming you only need FirstName for the Creator

                // Map the members to the new MemberDto structure
                Members = project.ProjectUsers.Select(pu => new ProjectMemberDto
                {
                    UserId = pu.UserId,
                    FirstName = pu.User.FirstName,
                    LastName = pu.User.LastName,
                    Email = pu.User.Email
                }).ToList()
            };

            return Ok(responseDto);
        }



        [HttpDelete("{id}")] // Route: api/Project/{id}
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> DeleteProject(int id) 
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var projectToDelete = await _context.Projects
                .Include(p => p.Tasks) 
                .FirstOrDefaultAsync(p => p.Id == id);

            if (projectToDelete == null)
            {
                return NotFound($"Project with ID {id} not found.");
            }

            // Task.Status is "Open" or "InProgress" for unfinished tasks.
            if (projectToDelete.Tasks.Any(t => t.Status != "Done"))
            {
                return BadRequest(new { message = "Cannot delete project: it still contains unfinished tasks." });
            }


            var authCheck = await _authorizationService.AuthorizeAsync(User, projectToDelete, "CanDeleteProject");
            if (!authCheck.Succeeded)
            {
                return StatusCode(403, "Unauthorized to delete this project.");
            }

            _context.Projects.Remove(projectToDelete);
            await _context.SaveChangesAsync();

            // NOTE: Returning NoContent() (204) is the standard for successful DELETE.
            return NoContent();
        }


        // Assign User to Project
        [Authorize(Roles = "Admin, Manager")]
        [HttpPost("{id}/members")] // Route: POST api/Project/{projectId}/members
        public async Task<IActionResult> AssignUserToProject (int id, [FromBody] ProjectMemberDto model)
        {
            var project = await _context.Projects
                .Include(p => p.Creator)
                .Include(p => p.ProjectUsers)
                    .ThenInclude(pu => pu.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return NotFound($"Project with ID {id} not found.");
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            
            if (user == null)
            {
                return NotFound($"User with ID {model.UserId} not found.");
            }

            // Check if the user is already assigned to the project
            var existingAssignment = await _context.ProjectUsers
                .FirstOrDefaultAsync(pu => pu.ProjectId == id && pu.UserId == model.UserId);
            if (existingAssignment != null)
            {
                return BadRequest($"User with ID {model.UserId} is already assigned to project ID {id}.");
            }

            // Role Check
            var authCheck = await _authorizationService.AuthorizeAsync(User, project, "CanManageMembers");
            if (!authCheck.Succeeded) {
                return StatusCode(403, new { message = "Unauthorized to assign members to this project." });
            }

            var projectUser = new ProjectUser
                {
                    UserId = model.UserId,
                    ProjectId = id
            };

            _context.ProjectUsers.Add(projectUser);
            await _context.SaveChangesAsync();
            
            return NoContent();
        }


        // Edit a Project
        [Authorize(Roles = "Admin, Manager")]
        [HttpPut("{id}")] // Route: PUT api/Project/{id}
        public async Task<IActionResult> EditProject(int id, [FromBody] EditProjectDto model)
        {
            var project = await _context.Projects
                .Include(p => p.Creator)
                .Include(p => p.ProjectUsers)
                    .ThenInclude(pu => pu.User)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (project == null)
            {
                return NotFound($"Project with ID {id} not found.");
            }
            // Role Check
            var authCheck = await _authorizationService.AuthorizeAsync(User, project, "CanModifyProject");
            if (!authCheck.Succeeded) {
                return StatusCode(403, new { message = "Unauthorized to edit this project." });
            }

            // Update project properties
            project.Name = model.Name;
            project.Description = model.Description;
            project.StartDate = model.StartDate;
            project.EndDate = model.EndDate;
            project.Status = model.Status;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Add a member to the project
        [Authorize(Roles = "Admin, Manager")]
        [HttpPost("{id}/add-member")] // Route: POST api/Project/{id}/add-member
        public async Task<IActionResult> AddMemberToProject(int id, [FromBody] ProjectMemberDto model)
        {
            var project = await _context.Projects
                .Include(p => p.Creator)
                .Include(p => p.ProjectUsers)
                    .ThenInclude(pu => pu.User)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (project == null)
            {
                return NotFound($"Project with ID {id} not found.");
            }
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound($"User with ID {model.UserId} not found.");
            }
            // Check if the user is already a member of the project
            var existingMember = await _context.ProjectUsers
                .FirstOrDefaultAsync(pu => pu.ProjectId == id && pu.UserId == model.UserId);
            if (existingMember != null)
            {
                return BadRequest($"User with ID {model.UserId} is already a member of project ID {id}.");
            }

            // Role Check
            var authCheck = await _authorizationService.AuthorizeAsync(User, project, "CanManageMembers");
            if (!authCheck.Succeeded) {
                return StatusCode(403, new { message = "Unauthorized to add members to this project." });
            }

            var projectUser = new ProjectUser
            {
                UserId = model.UserId,
                ProjectId = id
            };
            _context.ProjectUsers.Add(projectUser);
            await _context.SaveChangesAsync();
            return NoContent();
        }



        // Remove Member from Project
        [Authorize(Roles = "Admin, Manager")]
        [HttpDelete("{projectId}/members/{userId}")]
        public async Task<IActionResult> RemoveMember(int projectId, string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var project = await _context.Projects
                .Include(p => p.ProjectUsers)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) return NotFound("Project not found.");

            // 1. Authorization: Only Admin or the Project Creator can remove members
            var authCheck = await _authorizationService.AuthorizeAsync(User, project, "CanManageMembers");
            if (!authCheck.Succeeded)
            {
                return StatusCode(403, "Unauthorized to remove members from this project.");
            }

            // 2. Prevent Manager from removing themselves (Optional safety)
            if (userId == project.CreatorId)
            {
                return BadRequest("The Project Manager cannot be removed from the project.");
            }

            // 3. VALIDATION: Check for "Active" Tasks
            // We check if the user is assigned to any task in this project 
            // where the status is NOT "Done" and NOT "Completed".
            var hasActiveTasks = await _context.Tasks.AnyAsync(t =>
                t.ProjectId == projectId &&
                t.AssignedUserId == userId &&
                t.Status != "Done"
            );

            if (hasActiveTasks)
            {
                return BadRequest("Cannot remove member. They are assigned to active tasks. Please reassign or complete those tasks first.");
            }

            // 4. Remove the User
            var projectUser = await _context.ProjectUsers
                .FirstOrDefaultAsync(pu => pu.ProjectId == projectId && pu.UserId == userId);

            if (projectUser == null) return NotFound("Member not found in this project.");

            _context.ProjectUsers.Remove(projectUser);
            await _context.SaveChangesAsync();

            return NoContent();
        }

    }
}