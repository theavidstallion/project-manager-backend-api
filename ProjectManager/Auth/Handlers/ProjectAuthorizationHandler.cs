using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using ProjectManager.Models;
using System.Security.Claims;

namespace ProjectManager.Auth.Handlers
{
    public class ProjectAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, Project>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OperationAuthorizationRequirement requirement,
            Project project)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Task.CompletedTask;

            // Admin Override - Admins can do everything
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Switch based on operation
            switch (requirement.Name)
            {
                case "View":
                    if (CanView(context.User, project, userId))
                        context.Succeed(requirement);
                    break;

                case "Update":
                    if (CanUpdate(context.User, project, userId))
                        context.Succeed(requirement);
                    break;

                case "Delete":
                    if (CanDelete(context.User, project, userId))
                        context.Succeed(requirement);
                    break;

                case "ManageMembers":
                    if (CanManageMembers(context.User, project, userId))
                        context.Succeed(requirement);
                    break;
            }

            return Task.CompletedTask;
        }

        // Logic for Viewing
        private bool CanView(ClaimsPrincipal user, Project project, string userId)
        {
            // Managers can view
            if (user.IsInRole("Manager"))
                return true;

            // Members can view projects they're part of
            if (user.IsInRole("Member"))
            {
                var isMember = project.ProjectUsers?.Any(pu => pu.UserId == userId) ?? false;
                if (isMember) return true;
            }

            return false;
        }

        // Logic for Updating
        private bool CanUpdate(ClaimsPrincipal user, Project project, string userId)
        {
            // Only managers who own the project can update it
            if (user.IsInRole("Manager") && project.CreatorId == userId)
                return true;

            return false;
        }

        // Logic for Deleting
        private bool CanDelete(ClaimsPrincipal user, Project project, string userId)
        {
            // Only managers who own the project can delete it
            if (user.IsInRole("Manager") && project.CreatorId == userId)
                return true;

            return false;
        }

        // Logic for Managing Members (Add/Remove)
        private bool CanManageMembers(ClaimsPrincipal user, Project project, string userId)
        {
            // Only managers who own the project can manage members
            if (user.IsInRole("Manager") && project.CreatorId == userId)
                return true;

            return false;
        }
    }
}