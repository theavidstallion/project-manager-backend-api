using ProjectManager.Data;

namespace ProjectManager.Services
{
    public class ProjectAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public ProjectAuditService(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task UpdateTaskStatus(int taskId, string newStatus, string userId)
        {
            var task = await _context.Tasks.FindAsync(taskId);
            if (task == null) return;

            // 1. Capture Old Value (Snapshot)
            var oldSnapshot = new { Status = task.Status };

            // 2. Make Changes
            task.Status = newStatus;
            await _context.SaveChangesAsync();

            // 3. Log Activity Manually
            // Since we explicitly call this, it will NEVER run during Login/Auth
            await _audit.LogAsync(
                entityName: "ProjectTask",
                entityId: task.Id,
                action: "Updated",
                oldVal: oldSnapshot,
                newVal: new { Status = newStatus },
                userId: userId
            );
        }
    }
}
