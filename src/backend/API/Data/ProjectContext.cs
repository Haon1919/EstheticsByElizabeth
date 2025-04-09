using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data // Adjust namespace
{
    public class ProjectContext : DbContext
    {
        // Constructor used for dependency injection (common in ASP.NET Core)
        public ProjectContext(DbContextOptions<ProjectContext> options)
            : base(options)
        {
        }

        // DbSet properties represent the tables in your database
        public DbSet<Client> Clients { get; set; } = null!; // null! suppresses nullable warnings, EF ensures initialization
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Service> Services { get; set; } = null!;
        public DbSet<Appointment> Appointments { get; set; } = null!;

        // Optional: Configure model details using Fluent API (alternative/complement to Data Annotations)
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Recommended to call base method

            // Example: Configuring UNIQUE constraints using Fluent API (more robust than annotations)
            modelBuilder.Entity<Client>()
                .HasIndex(c => c.Email)
                .IsUnique();

            modelBuilder.Entity<Category>()
                .HasIndex(cat => cat.Name)
                .IsUnique();

            // Example: Defining relationship behavior (e.g., ON DELETE) - Default is usually RESTRICT
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Client)
                .WithMany(c => c.Appointments)
                .HasForeignKey(a => a.ClientId)
                .OnDelete(DeleteBehavior.Restrict); // Explicitly set delete behavior (Restrict is default)

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Service)
                .WithMany(s => s.Appointments)
                .HasForeignKey(a => a.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

             modelBuilder.Entity<Service>()
                .HasOne(s => s.Category)
                .WithMany(c => c.Services)
                .HasForeignKey(s => s.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // Or Cascade, SetNull depending on requirements
        }
    }
}
