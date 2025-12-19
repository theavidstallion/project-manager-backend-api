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

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            const string MemberRole = "Member";
            var user = await _userManager.FindByEmailAsync(registerDto.Email);

            // SCENARIO 1: User exists but is NOT confirmed -> Resend Email
            if (user != null && !user.EmailConfirmed)
            {
                // Optional: Ensure they have the role (Safe check)
                if (!await _userManager.IsInRoleAsync(user, MemberRole))
                {
                    await _userManager.AddToRoleAsync(user, MemberRole);
                }

                await SendConfirmationEmailAsync(user);
                return Ok(new { message = "Your email is already registered. Please check your email to confirm your account." });
            }

            // SCENARIO 2: User exists AND is confirmed -> Error
            if (user != null)
            {
                return BadRequest("Email address is already in use.");
            }

            // SCENARIO 3: New User -> Create
            user = new ApplicationUser
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (result.Succeeded)
            {
                if (await _roleManager.RoleExistsAsync(MemberRole))
                {
                    await _userManager.AddToRoleAsync(user, MemberRole);
                }

                await SendConfirmationEmailAsync(user);
                return Ok(new { message = "Registration successful. Please check your email to confirm your account." });
            }

            return BadRequest(result.Errors);
        }

        // --- HELPER METHOD TO SEND CONFIRMATION EMAIL (Keeps controller clean) ---
        private async Task SendConfirmationEmailAsync(ApplicationUser user)
        {
            var clientSettings = _configuration.GetSection("Client");
            var clientUrl = clientSettings["Url"] ?? "http://localhost:4200";

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            // Important: Use WebEncoders for URL safety, though Base64 is okay if careful
            var encodedToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(token));

            var callbackUrl = $"{clientUrl}/confirm-email?userId={user.Id}&token={encodedToken}";

            await _emailSender.SendEmailAsync(user.Email, "Confirm your email",
                $"Please confirm your account by clicking this link: <a href='{callbackUrl}'>link</a>");
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
                await SendConfirmationEmailAsync(user);
                return StatusCode(403, new { message = "Email not confirmed. A new confirmation link has been sent to your email." });
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


        // Forget Password Endpoint
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                // Don't reveal that the user does not exist or is not confirmed
                return Ok(new { message = "If an account with that email exists, a password reset link has been sent." });
            }
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(token));
            var clientSettings = _configuration.GetSection("Client");
            var clientUrl = clientSettings["Url"] ?? "https://humble-project-manager.netlify.app";
            var callbackUrl = $"{clientUrl}/reset-password?userId={user.Id}&token={encodedToken}";
            await _emailSender.SendEmailAsync(user.Email, "Reset Password",
                $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");
            return Ok(new { message = "If an account with that email exists, a password reset link has been sent." });
        }

        // Reset Password Endpoint
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return BadRequest("Invalid request.");
            }
            // Decode the Base64 token
            string decodedToken;
            try
            {
                decodedToken = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(model.Token));
            }
            catch
            {
                return BadRequest("Invalid token format.");
            }
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword);
            if (result.Succeeded)
            {
                return Ok(new { message = "Password has been reset successfully." });
            }
            return BadRequest(new { message = "Password reset failed.", errors = result.Errors });
        }


    }

}