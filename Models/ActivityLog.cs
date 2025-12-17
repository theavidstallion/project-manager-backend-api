using System;

namespace ProjectManager.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }
        public string EntityName { get; set; } // "Task", "Project"
        public int EntityId { get; set; }      // 101
        public string Action { get; set; }     // "Created", "Updated", "Deleted"
        public string? UserId { get; set; }    // Who did it
        public DateTimeOffset Timestamp { get; set; }

        // JSON strings to store flexible data
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
    }
}
