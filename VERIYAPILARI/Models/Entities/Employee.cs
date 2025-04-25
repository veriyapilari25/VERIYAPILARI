using System.ComponentModel.DataAnnotations.Schema;

namespace VERIYAPILARI.Models.Entities
{
    public class Employee
    {

        public int Id { get; set; }
        public required string Name { get; set; }

        [ForeignKey("ManagerId")]
        [InverseProperty("Subordinates")]
        public int? ManagerId { get; set; }
        public Employee? Manager { get; set; }

        [InverseProperty("Manager")]
        public ICollection<Employee> Subordinates { get; set; } = new List<Employee>();

        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public required string Position { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Now;

        public string? Email { get; set; }

    }

}
