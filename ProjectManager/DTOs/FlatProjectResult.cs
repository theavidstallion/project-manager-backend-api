namespace ProjectManager.DTOs
{
    public class FlatProjectResult
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }
        public string CreatorId { get; set; }
        public string CreatorName { get; set; }

        // Member Columns
        public string? MemberId { get; set; }
        public string? MemberFirstName { get; set; }
        public string? MemberLastName { get; set; }
        public string? MemberEmail { get; set; }
    }
}
