namespace ProjectManager.DTOs
{
    public class UpdateTaskDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; } // Low / Medium / High
        public DateTime DueDate { get; set; }
        public string Status { get; set; } // To Do / In Progress / Done

        public List<int>? TagIds { get; set; }

    }
}
