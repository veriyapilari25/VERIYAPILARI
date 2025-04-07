namespace VERIYAPILARI.Models.Entities
{
    public class Employee
    {

        public int Id { get; set; }
        public required string Name { get; set; }

        // Self-referencing relationship for hierarchical structure
        public int? ManagerId { get; set; }
        public Employee? Manager { get; set; }
        public ICollection<Employee> Subordinates { get; set; } = new List<Employee>();

        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public required string Position { get; set; }
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
    }

    }
