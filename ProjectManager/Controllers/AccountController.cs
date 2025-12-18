using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using ProjectManager.DTOs; 
using ProjectManager.Models; 
using ProjectManager.Services;
using System.Security.Claims;

namespace ProjectManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Route: api/Account
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;


        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService,
            RoleManager<IdentityRole> roleManager,
            IEmailSender emailSender,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _configuration = configuration;
        }

        // --- 1. Registration Endpoint (Only for Members) ---

        [HttpPost("register")] // Route: api/Account/register
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            // Requirement: Only used for creating Member users.
            const string MemberRole = "Member";

            // Check if user already exists
            if (await _userManager.FindByEmailAsync(registerDto.Email) != null)
            {
                return BadRequest("Email address is already in use.");
            }

            // Create the new user entity
            var user = new ApplicationUser
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName
            };

            // Create user with password
            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (result.Succeeded)
            {
                // Requirement: Every user who registers automatically becomes a Member.
                if (await _roleManager.RoleExistsAsync(MemberRole))
                {
                    await _userManager.AddToRoleAsync(user, MemberRole);
                }

                var clientSettings = _configuration.GetSection("Client");
                var clientUrl = clientSettings["Url"] ?? "http://localhost:4200";
                // Generate Email Confirmation Token
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(token));
                var callbackUrl = $"{clientUrl}/confirm-email?userId={user.Id}&token={encodedToken}";
                // Send confirmation email
                await _emailSender.SendEmailAsync(user.Email, "Confirm your email", $"Please confirm your account by clicking this link: <a href='{callbackUrl}'>link</a>");

                return Ok(new { message = "Registration successful. Please check your email to confirm your account." });
            }

            // If creation failed (e.g., password complexity)
            return BadRequest(result.Errors);
        }

        // --- 2. Login Endpoint (For Admin, Manager, and Member) ---

        [HttpPost("login")] // Route: api/Account/login
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            // Requirement: Used by Admin, Manager, and Member.

            var user = await _userManager.FindByEmailAsync(loginDto.Email);

            if (user == null)
            {
                return Unauthorized("Invalid email or password.");
            }

            // Check if email is confirmed
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                return Unauthorized("Please confirm your email address before logging in.");
            }

            // Attempt to sign in the user
            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

            if (result.Succeeded)
            {
                // Requirement: After login, the user receives a JWT token.
                var token = await _tokenService.CreateToken(user);

                return Ok(new
                {
                    userId = user.Id,
                    username = user.UserName,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    phoneNumber = user.PhoneNumber,
                    role = (await _userManager.GetRolesAsync(user)).FirstOrDefault(),
                    token = token // The client (Angular) must store this token.
                });
            }

            // Failure case
            return Unauthorized("Invalid email or password.");
        }


        // Confirm Email Endpoint - Changed to POST to handle token in body
        [HttpPost("confirm-email")] // Route: api/Account/confirm-email
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto confirmEmailDto)
        {
            if (string.IsNullOrEmpty(confirmEmailDto.UserId) || string.IsNullOrEmpty(confirmEmailDto.Token))
            {
                return BadRequest("User ID and token are required.");
            }

            var user = await _userManager.FindByIdAsync(confirmEmailDto.UserId);
            if (user == null)
            {
                return BadRequest("Invalid user ID.");
            }

            // Decode the Base64 token
            string decodedToken;
            try
            {
                decodedToken = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(confirmEmailDto.Token));
            }
            catch
            {
                return BadRequest("Invalid token format.");
            }

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            if (result.Succeeded)
            {
                return Ok(new { message = "Email confirmed successfully. You can now log in." });
            }

            return BadRequest(new { message = "Email confirmation failed.", errors = result.Errors });
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null) return NotFound("User not found");

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { message = "Profile updated successfully" });
            }

            return BadRequest(result.Errors);
        }



    }

}