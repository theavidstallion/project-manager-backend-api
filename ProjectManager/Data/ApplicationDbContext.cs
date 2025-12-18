using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Models;
using Microsoft.AspNetCore.Http; // Required for IHttpContextAccessor
using System.Security.Claims;    // Required for Claims
using System.Text.Json;          // Required for serialization

namespace ProjectManager.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) 
        {
        }

        // --- DbSet Additions for New Entities ---
        public DbSet<Project> Projects { get; set; } // Project entity
        public DbSet<ProjectUser> ProjectUsers { get; set; }    // Project-User join entity
        public DbSet<ProjectTask> Tasks { get; set; }         // Task entity
        public DbSet<Tag> Tags { get; set; }           // Tag entity
        public DbSet<TaskTag> TaskTags { get; set; }   // Task-Tag join entity
        public DbSet<Comment> Comments { get; set; } // Comment entity

        public DbSet<ActivityLog> ActivityLogs { get; set; } // ActivityLog entity





        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // IMPORTANT: Configures all Identity tables first.
            base.OnModelCreating(modelBuilder);

            // -----------------------------------------------------------
            // --- 1. PROJECT-USER RELATIONSHIPS (Existing) ---
            // -----------------------------------------------------------

            // Project -> Creator (One-to-One/Many)
            modelBuilder.Entity<Project>()
                .HasOne(p => p.Creator)
                .WithMany() // ApplicationUser does not need a collection for 'Created Projects'
                .HasForeignKey(p => p.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Project <-> ProjectUser <-> ApplicationUser (Many-to-Many)
            modelBuilder.Entity<ProjectUser>()
                .HasKey(pu => new { pu.ProjectId, pu.UserId });

            modelBuilder.Entity<ProjectUser>()
                .HasOne(pu => pu.Project)
                .WithMany(p => p.ProjectUsers)
                .HasForeignKey(pu => pu.ProjectId);

            modelBuilder.Entity<ProjectUser>()
                .HasOne(pu => pu.User)
                .WithMany(u => u.ProjectUsers)
                .HasForeignKey(pu => pu.UserId);


            // -----------------------------------------------------------
            // --- 2. TASK-PROJECT RELATIONSHIPS (NEW: One-to-Many) ---
            // -----------------------------------------------------------

            // Task -> Project (Task belongs to one Project)
            modelBuilder.Entity<ProjectManager.Models.ProjectTask>()
                .HasOne(t => t.Project)          // Task has ONE Project
                .WithMany(p => p.Tasks)          // Project has MANY Tasks
                .HasForeignKey(t => t.ProjectId) // FK is Task.ProjectId
                .OnDelete(DeleteBehavior.Cascade); // If a Project is deleted, its Tasks are deleted.


            // -----------------------------------------------------------
            // --- 3. TASK-USER RELATIONSHIPS (NEW: One-to-One/Many) ---
            // -----------------------------------------------------------

            // Task -> AssignedUser (Task is assigned to one ApplicationUser)
            modelBuilder.Entity<ProjectManager.Models.ProjectTask>()
                .HasOne(t => t.AssignedUser)            // Task has ONE AssignedUser
                .WithMany(u => u.Tasks)         // ApplicationUser has MANY AssignedTasks (renamed for clarity)
                .HasForeignKey(t => t.AssignedUserId)   // FK is Task.AssignedUserId
                .OnDelete(DeleteBehavior.Restrict);     // Prevent deleting a User if they have tasks assigned.


            // -----------------------------------------------------------
            // --- 4. TASK-TAG RELATIONSHIPS (NEW: Many-to-Many) ---
            // -----------------------------------------------------------

            // Define the composite key on the TagTask join entity
            modelBuilder.Entity<TaskTag>()
                .HasKey(tt => new { tt.TagId, tt.TaskId });

            // Link TagTask to Tag
            modelBuilder.Entity<TaskTag>()
                .HasOne(tt => tt.Tag)
                .WithMany(t => t.TaskTags) // Assuming Tag.cs uses 'TagTasks' for the collection
                .HasForeignKey(tt => tt.TagId);

            // Link TagTask to Task
            modelBuilder.Entity<TaskTag>()
                .HasOne(tt => tt.Task)
                .WithMany(t => t.TaskTags) // Task.cs uses 'TagTasks' for the collection
                .HasForeignKey(tt => tt.TaskId);

            // -----------------------------------------------------------
            // --- 5. COMMENT RELATIONSHIPS (NEW: One-to-Many) ---
            // -----------------------------------------------------------

            // Comment -> Task (Comment belongs to one ProjectTask)
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Task)                    // Comment has ONE Task
                .WithMany(t => t.Comments)              // ProjectTask has MANY Comments
                .HasForeignKey(c => c.TaskId)           // FK is Comment.TaskId
                .OnDelete(DeleteBehavior.Cascade);      // If a Task is deleted, its Comments are deleted.

            // Comment -> Author (Comment belongs to one ApplicationUser)
            modelBuilder.Entity<Comment>()
                .HasOne<ApplicationUser>()              // Comment has ONE Author (linking to ApplicationUser)
                .WithMany(u => u.Comments)              // ApplicationUser has MANY Comments (Assuming you added 'ICollection<Comment> Comments' to ApplicationUser)
                .HasForeignKey(c => c.AuthorId)         // FK is Comment.AuthorId
                .OnDelete(DeleteBehavior.Restrict);     // Prevent deleting a User if they have comments.

        }

    }
}