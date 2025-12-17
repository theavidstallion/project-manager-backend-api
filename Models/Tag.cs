namespace ProjectManager.Models
{
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; }

        // Navigation property for the join entity (Many-to-Many setup)
        public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
    }
}
