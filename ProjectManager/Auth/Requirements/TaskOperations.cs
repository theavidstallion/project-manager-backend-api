using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace ProjectManager.Auth.Requirements
{

    public class TaskOperations 
    {
        public static OperationAuthorizationRequirement Update = new() { Name = "Update" };
        public static OperationAuthorizationRequirement Delete = new() { Name = "Delete" };
        public static OperationAuthorizationRequirement Create = new() { Name = "Create" };
        public static OperationAuthorizationRequirement View = new() { Name = "View" };
    }
}
