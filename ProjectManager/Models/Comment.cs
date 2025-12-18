namespace ProjectManager.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string AuthorId { get; set; }


        // Foreign key to the associated task
        public int TaskId { get; set; }

        // Navigation property to the associated task
        public ProjectTask Task { get; set; }

    }
}




// Not set in Tasks or DbContext