using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Data;
using ProjectManager.Models;
using ProjectManager.Services;
using System.Security.Claims;
using System.Text.Json;

namespace ProjectManager.Filters
{
    public class AuditLogActionFilter : IAsyncActionFilter
    {
        private readonly IAuditService _auditService;
        private readonly ApplicationDbContext _context;

        public AuditLogActionFilter(IAuditService auditService, ApplicationDbContext context)
        {
            _auditService = auditService;
            _context = context;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            object? oldEntitySnapshot = null;
            string? entityName = null;
            int entityId = 0;

            // 1. BEFORE ACTION: Capture old state for UPDATE/DELETE
            var httpMethod = context.HttpContext.Request.Method;
            var controllerName = context.ActionDescriptor.RouteValues["controller"];

            if (httpMethod == "PUT" || httpMethod == "PATCH" || httpMethod == "DELETE")
            {
                entityId = ExtractEntityId(context);
                if (entityId > 0)
                {
                    entityName = controllerName;
                    oldEntitySnapshot = await CaptureOldState(entityName, entityId);
                }
            }

            // 2. EXECUTE THE ACTION
            var executedContext = await next();

            // 3. AFTER ACTION: Log if successful
            if (executedContext.Exception == null && IsSuccessStatusCode(executedContext))
            {
                var userId = context.HttpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier);

                if (ShouldLog(httpMethod))
                {
                    entityName ??= controllerName;
                    string action = DetermineAction(httpMethod);

                    // For CREATE, extract ID from response
                    if (action == "Created")
                    {
                        entityId = ExtractCreatedEntityId(executedContext, context);
                    }

                    if (entityId > 0)
                    {
                        object? newEntitySnapshot = null;

                        // For CREATE/UPDATE, capture new state
                        if (action == "Created" || action == "Updated")
                        {
                            newEntitySnapshot = await CaptureNewState(entityName, entityId);
                        }

                        await _auditService.LogAsync(
                            entityName: entityName,
                            entityId: entityId,
                            action: action,
                            oldVal: oldEntitySnapshot,
                            newVal: newEntitySnapshot,
                            userId: userId ?? "System"
                        );
                    }
                }
            }
        }

        private async Task<object?> CaptureOldState(string entityName, int entityId)
        {
            return entityName switch
            {
                "Project" => await _context.Projects
                    .Where(p => p.Id == entityId)
                    .Select(p => new { p.Name, p.Description, p.Status, p.StartDate, p.EndDate })
                    .FirstOrDefaultAsync(),

                "Task" => await _context.Tasks
                    .Where(t => t.Id == entityId)
                    .Select(t => new { t.Title, t.Description, t.Status, t.Priority, t.DueDate })
                    .FirstOrDefaultAsync(),

                "Comment" => await _context.Comments
                    .Where(c => c.Id == entityId)
                    .Select(c => new { c.Content })
                    .FirstOrDefaultAsync(),

                "ProjectUser" => await _context.ProjectUsers
                    .Where(pu => pu.ProjectId == entityId)
                    .Select(pu => new { pu.UserId, pu.ProjectId })
                    .FirstOrDefaultAsync(),

                _ => null
            };
        }

        private async Task<object?> CaptureNewState(string entityName, int entityId)
        {
            // Re-query the entity after the update to get the new state
            return await CaptureOldState(entityName, entityId);
        }

        private int ExtractEntityId(ActionExecutingContext context)
        {
            // Try common parameter names
            if (context.ActionArguments.TryGetValue("id", out var id) && id is int intId)
                return intId;

            if (context.ActionArguments.TryGetValue("projectId", out var projectId) && projectId is int pId)
                return pId;

            if (context.ActionArguments.TryGetValue("taskId", out var taskId) && taskId is int tId)
                return tId;

            return 0;
        }

        private int ExtractCreatedEntityId(ActionExecutedContext executedContext, ActionExecutingContext context)
        {
            // Try to extract from response body
            if (executedContext.Result is ObjectResult objResult && objResult.Value != null)
            {
                var resultType = objResult.Value.GetType();
                var idProp = resultType.GetProperty("Id") ?? resultType.GetProperty("id");

                if (idProp != null && idProp.GetValue(objResult.Value) is int id)
                    return id;
            }

            // Fallback: Try action arguments (some endpoints return the created ID)
            return ExtractEntityId(context);
        }

        private bool IsSuccessStatusCode(ActionExecutedContext context)
        {
            if (context.Result is StatusCodeResult statusCodeResult)
                return statusCodeResult.StatusCode >= 200 && statusCodeResult.StatusCode < 300;

            if (context.Result is ObjectResult objectResult)
                return objectResult.StatusCode == null || (objectResult.StatusCode >= 200 && objectResult.StatusCode < 300);

            return context.Result is NoContentResult or OkResult or OkObjectResult or CreatedResult or CreatedAtActionResult;
        }

        private bool ShouldLog(string httpMethod)
        {
            return httpMethod is "POST" or "PUT" or "PATCH" or "DELETE";
        }

        private string DetermineAction(string httpMethod)
        {
            return httpMethod switch
            {
                "POST" => "Created",
                "PUT" or "PATCH" => "Updated",
                "DELETE" => "Deleted",
                _ => "Unknown"
            };
        }
    }
}