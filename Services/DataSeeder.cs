using Microsoft.AspNetCore.Identity;
using ProjectManager.Data;
using ProjectManager.Models;
using System.Security.Claims;

namespace ProjectManager.Services
{
    public static class DataSeeder
    {
        private const string AdminEmail = "admin@project.com";
        private const string AdminPassword = "Admin@12345";
        private const string AdminPhoneNumber = "+1234567890";

        // Role names
        private const string AdminRole = "Admin";
        private const string ManagerRole = "Manager";
        private const string MemberRole = "Member";

        // Claims for roles
        public const string claimType = "TaskPermission";
        public const string claimValue = "CanCloseTask";


        public static async Task InitializeAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // --- 1. ENSURE ROLES EXIST ---
            // Create roles only if they don't exist. We run this block every time.
            await CreateRoleIfNotExists(roleManager, AdminRole);
            await CreateRoleIfNotExists(roleManager, ManagerRole);
            await CreateRoleIfNotExists(roleManager, MemberRole);

            // --- 2. ASSIGN CLAIMS (Run this every time to ensure claims are present) ---
            var adminRole = await roleManager.FindByNameAsync(AdminRole);
            if (adminRole != null)
            {
                // Admin gets CanManageProjects
                await AddClaimToRoleIfNotExists(roleManager, adminRole, claimType, claimValue);
            }

            var managerRole = await roleManager.FindByNameAsync(ManagerRole);
            if (managerRole != null)
            {
                // Manager also gets CanManageProjects
                await AddClaimToRoleIfNotExists(roleManager, managerRole, claimType, claimValue);
            }

            if (await roleManager.FindByNameAsync(MemberRole) == null)
            {
                await roleManager.CreateAsync(new IdentityRole(MemberRole));
            }
            

            // Creating Admin User if it does not exist
            if (await userManager.FindByEmailAsync(AdminEmail) == null)
            {
                var AdminUser = new ApplicationUser
                {
                    UserName = AdminEmail,
                    Email = AdminEmail,
                    PhoneNumber = AdminPhoneNumber,
                    FirstName = "Administrator",
                    LastName = "N/A",
                    EmailConfirmed = true,
                };

                var result = await userManager.CreateAsync(AdminUser, AdminPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(AdminUser, AdminRole);
                }
            }

            

        }

        // Helper Methods for Seeder
        private static async Task CreateRoleIfNotExists(RoleManager<IdentityRole> roleManager, string roleName)
        {
            if (await roleManager.FindByNameAsync(roleName) == null)
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // Use the existing AddClaimToRole logic, but rename it for clarity.
        private static async Task AddClaimToRoleIfNotExists(
            RoleManager<IdentityRole> roleManager,
            IdentityRole role,
            string claimType,
            string claimValue)
        {
            var claims = await roleManager.GetClaimsAsync(role);
            if (!claims.Any(c => c.Type == claimType && c.Value == claimValue))
            {
                await roleManager.AddClaimAsync(role, new Claim(claimType, claimValue));
            }
        }

    }
}
