namespace ProjectManager.DTOs
{
    public class TaskResponseDto
    {
        // Core Task Details
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }

        public string CreatorId { get; set; }

        // Foreign Keys (Kept for clarity/editing, but names are provided too)
        public int ProjectId { get; set; }
        public string? AssignedUserId { get; set; } // Can be null if not yet assigned

        // User-Friendly Data (New fields)
        public string ProjectName { get; set; }
        public string AssignedUserName { get; set; }

        // Tags as Names (Clean list of tag strings)
        public List<string> Tags { get; set; }
    }
}