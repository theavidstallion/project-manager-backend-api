namespace ProjectManager.DTOs
{
    public class FlatTaskResult
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public DateTime DueDate { get; set; }
        public string CreatorId { get; set; }
        public int ProjectId { get; set; }
        public string? AssignedUserId { get; set; }

        // Joined Columns
        public string ProjectName { get; set; }
        public string? AssignedUserFirstName { get; set; }
        public string? AssignedUserLastName { get; set; }

        // Tag strings from STRING_AGG (e.g., "1,2,3")
        public string? TagIds { get; set; }
        public string? TagNames { get; set; }
    }
}
