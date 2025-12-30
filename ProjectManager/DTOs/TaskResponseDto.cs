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

        // Foreign Keys
        public int ProjectId { get; set; }
        public string? AssignedUserId { get; set; }

        // User-Friendly Data
        public string ProjectName { get; set; }
        public string AssignedUserName { get; set; }

        // 🟢 ADD THIS for Edit/Update operations
        public List<int> TagIds { get; set; } = new();

        // Tags as Names (For Display)
        public List<string> Tags { get; set; } = new();
    }
}