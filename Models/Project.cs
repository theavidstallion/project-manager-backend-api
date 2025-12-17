namespace ProjectManager.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }
        public string CreatorId { get; set; }
        public string? CreatorName { get; set; } = "N/A";


        // Navigation property for members (Many-to-Many setup)
        public ICollection<ProjectUser> ProjectUsers { get; set; } = new List<ProjectUser>();

        public ApplicationUser Creator { get; set; } = null!;

        // Navigation property for tasks (One-to-Many setup)
        public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
    }
}
