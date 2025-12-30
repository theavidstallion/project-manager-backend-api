using Microsoft.AspNetCore.Identity;
using ProjectManager.Models;

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

        public static async Task InitializeAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // --- 1. ENSURE ROLES EXIST ---
            await CreateRoleIfNotExists(roleManager, AdminRole);
            await CreateRoleIfNotExists(roleManager, ManagerRole);
            await CreateRoleIfNotExists(roleManager, MemberRole);

            // --- 2. CREATE ADMIN USER IF NOT EXISTS ---
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
    }
}