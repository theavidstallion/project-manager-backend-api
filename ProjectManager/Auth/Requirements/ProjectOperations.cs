using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace ProjectManager.Auth.Requirements
{
    public class ProjectOperations
    {
        public static OperationAuthorizationRequirement View = new() { Name = "View" };
        public static OperationAuthorizationRequirement Update = new() { Name = "Update" };
        public static OperationAuthorizationRequirement Delete = new() { Name = "Delete" };
        public static OperationAuthorizationRequirement ManageMembers = new() { Name = "ManageMembers" };
    }
}