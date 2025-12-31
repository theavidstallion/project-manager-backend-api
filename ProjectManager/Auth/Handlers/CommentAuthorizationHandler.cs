using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using ProjectManager.Auth.Requirements;
using ProjectManager.Models;
using System.Security.Claims;

namespace ProjectManager.Auth.Handlers
{
    public class CommentAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, Comment>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, Comment comment)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Task.CompletedTask;

            // Admins have full access
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // switch-case to jump to the correct permission check
            switch (requirement.Name)
            {
                case "Edit":
                    if (CanEdit(context.User, comment, userId))
                    {
                        context.Succeed(requirement);
                    }
                    break;
                case "Delete":
                    if (CanDelete(context.User, comment, userId))
                    {
                        context.Succeed(requirement);
                    }
                    break;
            }
            return Task.CompletedTask;

        }

        // Helpers to check permissions
        private bool CanEdit(ClaimsPrincipal user, Comment comment, string userId)
        {
            // Authors can edit their own comments
            if (comment.AuthorId == userId)
            {
                return true;
            }
            return false;
        }

        private bool CanDelete(ClaimsPrincipal user, Comment comment, string userId)
        {
            // Authors can delete their own comments
            if (comment.AuthorId == userId)
            {
                return true;
            }
            return false;
        }

    }
}

