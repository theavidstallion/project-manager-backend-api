using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using ProjectManager.Models;
using System.Security.Claims;

namespace ProjectManager.Auth.Handlers
{
    public class TaskAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, ProjectTask>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OperationAuthorizationRequirement requirement,
            ProjectTask task)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Task.CompletedTask;

            // 1. Admin Override (Global)
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // 2. Switch Logic based on Operation Name
            switch (requirement.Name)
            {
                case "Update":
                    if (CanEdit(context.User, task, userId)) context.Succeed(requirement);
                    break;
                case "Delete":
                    if (CanDelete(context.User, task, userId)) context.Succeed(requirement);
                    break;
                case "View":
                    if (CanView(context.User, task, userId)) context.Succeed(requirement);
                    break;
            }

            return Task.CompletedTask;
        }

        


        // Logic for Updating
        private bool CanEdit(ClaimsPrincipal user, ProjectTask task, string userId)
        {
            // Manager: Must own project
            if (user.IsInRole("Manager") && task.Project?.CreatorId == userId) return true;

            // Member: Must be assigned
            if (user.IsInRole("Member") && task.AssignedUserId == userId) return true;

            return false;
        }

        // Logic for Deleting
        private bool CanDelete(ClaimsPrincipal user, ProjectTask task, string userId)
        {
            // Manager: Must own project
            if (user.IsInRole("Manager") && task.Project?.CreatorId == userId) return true;

            // Members cannot delete
            return false;
        }

        // Logic for Viewing
        private bool CanView(ClaimsPrincipal user, ProjectTask task, string userId)
        {
            // Managers can view
            if (user.IsInRole("Manager")) return true;

            // Members can view if they are assigned to the task
            if (user.IsInRole("Member") && task.AssignedUserId == userId) return true;

            return false;
        }

    }
}