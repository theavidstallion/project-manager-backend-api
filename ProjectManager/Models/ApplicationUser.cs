using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ProjectManager.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = "N/A";
        public string LastName { get; set; } = "N/A";


        // Navigation property for projects (Many-to-Many setup)
        public ICollection<ProjectUser> ProjectUsers { get; set; } = new List<ProjectUser>();

        // Navigation property for tasks (One-to-Many setup)
        public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();

        // Navigation property for comments (One-to-Many setup)
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
