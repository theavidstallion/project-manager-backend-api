using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace ProjectManager.Auth.Requirements
{
    public class CommentOperations
    {
        public static OperationAuthorizationRequirement Edit = new() { Name = "Edit" };
        public static OperationAuthorizationRequirement Delete = new() { Name = "Delete" };
    }
}
