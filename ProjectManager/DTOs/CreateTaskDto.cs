namespace ProjectManager.DTOs
{
    public class CreateTaskDto
    {
        // Core Task Details
        public string Title { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; } // Low / Medium / High
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = "Open"; // To Do / In Progress / Done

        // Foreign Keys (Required for placing the task)
        public int ProjectId { get; set; }
        public string AssignedUserId { get; set; }

        // Tags to be associated immediately (List of existing Tag IDs)
        public List<int>? TagIds { get; set; }
    }
}