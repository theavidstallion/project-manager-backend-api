using ProjectManager.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

// ITokenService defines the contract (interface) for generating the token.
public interface ITokenService
{
    Task<string> CreateToken(ApplicationUser user);
}

// TokenService is the concrete class that implements the token creation logic.
public class TokenService : ITokenService
{
    // Origin: Injected via DI (reads configuration settings like the secret key)
    private readonly IConfiguration _configuration;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public TokenService(IConfiguration configuration, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _configuration = configuration;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<string> CreateToken(ApplicationUser user)
    {
        // 1. Define the user's claims (Identity/Data used in the token)
        var claims = new List<Claim>
        {
            // Standard claim to identify the user uniquely (essential)
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email),
        };

        // 2. Add roles to claims (Crucial for Authorization checks via [Authorize(Roles="Admin")])
        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        // 3. Add user-specific claims
        var userClaims = await _userManager.GetClaimsAsync(user);
        claims.AddRange(userClaims);

        // 4. NEW: Add claims from each role the user belongs to
        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                var roleClaims = await _roleManager.GetClaimsAsync(role);
                claims.AddRange(roleClaims);
            }
        }

        // 5. Get the symmetric key from Configuration (CRITICAL: Must be secure and long)
        // Note: You must add a "Jwt:Key" setting to your appsettings.json/secrets.json
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]
                                         ?? throw new InvalidOperationException("JWT Key not configured.")));

        // 6. Create signing credentials (The digital signature for verification)
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        // 7. Define the token content
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.Now.AddDays(7), // Token expiration date
            SigningCredentials = creds
        };

        // 8. Generate the token string
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}