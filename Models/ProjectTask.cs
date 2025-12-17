namespace ProjectManager.Models
{
    public class ProjectTask
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; }


        public string CreatorId { get; set; } = "N/A";

        // Foreign keys
        public string AssignedUserId { get; set; }
        public int ProjectId { get; set; }

        // Navigation properties for Relationships
        public Project Project { get; set; }
        public ApplicationUser AssignedUser { get; set; }

        public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
