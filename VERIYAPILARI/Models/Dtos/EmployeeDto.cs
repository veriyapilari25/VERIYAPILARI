namespace VERIYAPILARI.Models.Dtos
{

        public class EmployeeDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Position { get; set; }
            public string? DepartmentName { get; set; }
            public int? ManagerId { get; set; }
            public string? ManagerName { get; set; }

            public List<EmployeeDto> Subordinates { get; set; } = new();
        }
    }

