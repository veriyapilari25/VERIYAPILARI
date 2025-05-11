using Microsoft.EntityFrameworkCore;
using VERIYAPILARI.Models.Entities;
using VERIYAPILARI.Models;

namespace VERIYAPILARI.Data
{
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options)
        {
            
        }
        public DbSet<Employee> Employees { get; set; }

        public DbSet<Department> Departments { get; set; }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)

        {
            modelBuilder.Entity<Department>().HasData(
                new Department
                {
                    Id = 1,
                    Name = "Management"
                },
                new Department
                {
                    Id = 2,
                    Name = "Engineering"
                },
                new Department
                {
                    Id = 3,
                    Name = "HR"
                }
                );
            modelBuilder.Entity<Employee>().HasData(
                new Employee { Id = 1, Name = "Alice", ManagerId = null, DepartmentId = 1, Position = "CEO", StartDate = new DateTime(2023, 4, 1) },
                new Employee { Id = 2, Name = "Bob", ManagerId = 1, DepartmentId = 2, Position = "Engineering Manager", StartDate = new DateTime(2023, 4, 1) },
                new Employee { Id = 3, Name = "Charlie", ManagerId = 2, DepartmentId = 2, Position = "Software Engineer", StartDate = new DateTime(2023, 4, 1) },
                new Employee { Id = 4, Name = "David", ManagerId = 2, DepartmentId = 2, Position = "Software Engineer", StartDate = new DateTime(2023, 4, 1) },
                new Employee { Id = 5, Name = "Eve", ManagerId = 1, DepartmentId = 3, Position = "HR Manager", StartDate = new DateTime(2023, 4, 1) },
                new Employee { Id = 6, Name = "Frank", ManagerId = 5, DepartmentId = 3, Position = "HR Specialist", StartDate = new DateTime(2023, 4, 1) }
            );

            // Add initial admin user
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    PasswordHash = "admin123", // In a real application, this should be hashed
                    Email = "admin@company.com",
                    Role = "Admin"
                }
            );
        }
    }
}