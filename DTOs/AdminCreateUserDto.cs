namespace ProjectManager.DTOs
{
    public class AdminCreateUserDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; } // e.g., "Admin", "Manager", "Member".
    }
}
