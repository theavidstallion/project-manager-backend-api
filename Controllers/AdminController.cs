using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Data;
using ProjectManager.DTOs;
using ProjectManager.Models;
using System;
using System.Security.Claims;

namespace ProjectManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [Authorize(Roles = "Admin")]
        // Create a user with any role
        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserDto userDto)
        {
            var adminUser = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (adminUser == null)
            {
                return Unauthorized(new { Message = "Admin privileges required." });
            }
            var user = new ApplicationUser
            {
                UserName = userDto.Email,
                Email = userDto.Email,
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                EmailConfirmed = true
            };
            
            var addUserResult = await _userManager.CreateAsync(user, userDto.Password);
            if (!addUserResult.Succeeded)
            {
                return BadRequest(new { Message = "User creation failed.", Errors = addUserResult.Errors });
            }

            var addUserRole = await _userManager.AddToRoleAsync(user, userDto.Role);
            if (!addUserRole.Succeeded)
            {
                return BadRequest(new { Message = "Assigning role failed.", Errors = addUserRole.Errors });
            }

            return Ok(new { Message = "User created successfully." });
        }

        // Authorize both Admins and Managers
        [Authorize(Roles = "Admin,Manager")]
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers() // 1. Use async Task
        {
            // 2. Fetch the full User Entities in one go (await)
            // We fetch the actual ApplicationUser objects so we can pass them directly to GetRolesAsync
            var users = await _userManager.Users.ToListAsync();

            var usersWithRoles = new List<object>();

            foreach (var user in users)
            {
                // 3. Get Roles Async (No .Result blocking)
                // Since we already have the 'user' object, we don't need FindByIdAsync again!
                var roles = await _userManager.GetRolesAsync(user);

                // 4. Extract single role
                var primaryRole = roles.FirstOrDefault() ?? "None";

                usersWithRoles.Add(new
                {
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    Role = primaryRole // Matching your Frontend model expectation
                });
            }

            return Ok(usersWithRoles);
        }

        [Authorize(Roles = "Admin")]
        // Delete a user by ID
        [HttpDelete("delete-user/{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var adminUser = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (adminUser == null)
            {
                return Unauthorized(new { Message = "Admin privileges required." });
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }
            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                return BadRequest(new { Message = "User deletion failed.", Errors = deleteResult.Errors });
            }
            return Ok(new { Message = "User deleted successfully." });
        }

        [Authorize(Roles = "Admin")]
        // Change user role
        [HttpPost("change-role/{userId}")]
        public async Task<IActionResult> ChangeUserRole(string userId, [FromBody] ChangeUserRoleDto roleDto)
        {
            var adminUser = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (adminUser == null)
            {
                return Unauthorized(new { Message = "Admin privileges required." });
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }
            var currentRoles = await _userManager.GetRolesAsync(user);
            var removeRolesResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeRolesResult.Succeeded)
            {
                return BadRequest(new { Message = "Removing existing roles failed.", Errors = removeRolesResult.Errors });
            }
            var addRoleResult = await _userManager.AddToRoleAsync(user, roleDto.NewRole);
            if (!addRoleResult.Succeeded)
            {
                return BadRequest(new { Message = "Assigning new role failed.", Errors = addRoleResult.Errors });
            }
            return Ok(new { Message = "User role updated successfully." });
        }


    }
}
