namespace ProjectManager.Models
{
    public class ProjectUser
    {
        public int ProjectId { get; set; }
        public string UserId { get; set; }


        // Navigation properties for Relationships
        public Project Project { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
    }
}


// Need this model to establish Many-to-Many relationship between Projects and ApplicationUsers (Members). 
// One to Many or One to One rely on foreign keys in either of the two models, but Many-to-Many needs a junction table/model to hold foreign keys of both models.