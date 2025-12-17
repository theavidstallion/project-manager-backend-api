using ProjectManager.Data;
using ProjectManager.Models;
using System.Text.Json;

namespace ProjectManager.Services
{

    public interface IAuditService
    {
        Task LogAsync(string entityName, int entityId, string action, object? oldVal, object? newVal, string userId);
    }

    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string entityName, int entityId, string action, object? oldVal, object? newVal, string userId)
        {
            var log = new ActivityLog
            {
                EntityName = entityName,
                EntityId = entityId,
                Action = action,
                UserId = userId,
                Timestamp = DateTimeOffset.UtcNow,
                // Serialize the objects to JSON strings
                OldValues = oldVal == null ? null : JsonSerializer.Serialize(oldVal),
                NewValues = newVal == null ? null : JsonSerializer.Serialize(newVal)
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
