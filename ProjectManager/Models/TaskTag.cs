namespace ProjectManager.Models
{
    public class TaskTag
    {
        public int TagId { get; set; }
        public int TaskId { get; set; }

        // Navigation properties for Relationships
        public Tag Tag { get; set; } = null!;
        public ProjectTask Task { get; set; } = null!;
    }
}
