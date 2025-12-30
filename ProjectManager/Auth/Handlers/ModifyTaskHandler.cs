using ProjectManager.Models;
using Microsoft.AspNetCore.Authorization;
using ProjectManager.Auth.Requirements;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProjectManager.Auth.Handlers
{
    public class ModifyTaskHandler : AuthorizationHandler<ModifyTaskRequirement, ProjectTask>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ModifyTaskRequirement requirement,
            ProjectTask task)
        {
            // Get User ID
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            // If no ID (not logged in), fail immediately
            if (string.IsNullOrEmpty(userId)) return Task.CompletedTask;

            // Admin: KING
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Manager: Must be Project Creator
            if (context.User.IsInRole("Manager"))
            {
                if (task.Project?.CreatorId == userId)
                {
                    context.Succeed(requirement);
                }
            }

            // Member: Must be Assigned User
            else if (context.User.IsInRole("Member"))
            {
                if (task.AssignedUserId == userId)
                {
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}
