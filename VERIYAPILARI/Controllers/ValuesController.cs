using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Security.AccessControl;
using System.Threading.Tasks;
using System.Transactions;
using VERIYAPILARI.Data;
using VERIYAPILARI.Models.Dtos;
using VERIYAPILARI.Models.Entities;

namespace VERIYAPILARI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController(ApplicationDBContext context) : ControllerBase
    {
        private readonly ApplicationDBContext _context = context;

        [HttpGet("GetEmployees")]
        public async Task<ActionResult<List<Employee>>> GetEmployees()
        {
            var employees = await _context.Employees
                    .Include(e => e.Department)
                    .Include(e => e.Manager)
                    .ToListAsync();

            var employeeDtos = employees.Select(e =>
            MapEmployeeToDto(e)).ToList();

            return Ok(employeeDtos);
        }

        [HttpGet("GetAllEmployeesWithSubordinates")]
        public async Task<IActionResult> GetAllEmployeesWithSubordinates()
        {
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Manager)
                .ToListAsync();

            var employeeDtos = new List<EmployeeDto>();

            foreach (var employee in employees)
            {
                var dto = MapEmployeeToDto(employee);
                dto.Subordinates = await GetSubordinatesDtoRecursive(employee.Id);
                employeeDtos.Add(dto);
            }

            return Ok(employeeDtos);
        }



        [HttpGet("GetEmployeeByID/{id}")]
        public async Task<ActionResult<Employee>> GetEmployeeByID(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.Manager)
                .Include(e => e.Department)
                .Include(e => e.Subordinates!)
                .ThenInclude(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (employee == null)
            {
                return NotFound();
            }
            var employeeDto = MapEmployeeWithSubordinatesToDto(employee);
            return Ok(employeeDto);
        }
        private EmployeeDto MapEmployeeWithSubordinatesToDto(Employee employee)
        {
            return new EmployeeDto
            {
                Id = employee.Id,
                Name = employee.Name,
                Position = employee.Position,
                DepartmentName = employee.Department?.Name,
                ManagerId = employee.ManagerId,
                ManagerName = employee.Manager?.Name,
                Subordinates = employee.Subordinates?.Select(sub => new EmployeeDto
                {
                    Id = sub.Id,
                    Name = sub.Name,
                    Position = sub.Position,
                    DepartmentName = sub.Department?.Name,
                    ManagerId = sub.ManagerId,
                    ManagerName = employee.Name,
                    Subordinates = new List<EmployeeDto>() // To avoid deep recursion
                }).ToList() ?? new()
            };
        }

        [HttpPost("PostEmployee")]
        public async Task<ActionResult<Employee>> PostEmployee(Employee newEmployee)
        {
            if (newEmployee == null)
                return BadRequest("Employee data is required.");

            newEmployee.StartDate = DateTime.UtcNow;
            _context.Employees.Add(newEmployee);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetEmployeeByID), new { id = newEmployee.Id }, newEmployee);
        }

        [HttpPut("UpdateEmployee/{id}")]
        public async Task<IActionResult> UpdateEmployee(int id, Employee updatedEmployee)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return NotFound();

            employee.Name = updatedEmployee.Name;
            employee.Position = updatedEmployee.Position;
            employee.StartDate = updatedEmployee.StartDate;
            employee.DepartmentId = updatedEmployee.DepartmentId;
            employee.Department = updatedEmployee.Department;
            if (updatedEmployee.ManagerId != employee.ManagerId)
            {
                employee.ManagerId = updatedEmployee.ManagerId;
                employee.Manager = updatedEmployee.Manager;

                if (employee.Manager != null)
                {
                    employee.Manager.Subordinates.Add(employee);
                }
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }
        [HttpDelete("DeleteEmployee/{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return NotFound();
            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("GetDepartmentsEmployees/{id}")]
        public async Task<IActionResult> GetDepartmentsEmployees(int id)
        {
            var departments = await _context.Departments
                                    .Where(d => d.Id == id)
                                    .Include(d => d.Employees)
                                    .ThenInclude(e => e.Manager)
                                    .ToListAsync();

            if (departments == null || !departments.Any())
            {
                return NotFound();
            }


            var departmentDtos = departments.Select(d => new
            {
                DepartmentId = d.Id,
                DepartmentName = d.Name,
                Employees = d.Employees.Select(e => new
                {
                    e.Id,
                    e.Name,
                    e.Position,
                    ManagerName = e.Manager?.Name,
                    ManagerId = e.ManagerId

                }).ToList()
            }).ToList();

            return Ok(departmentDtos);

        }

        private EmployeeDto MapEmployeeToDto(Employee employee)
        {
            return new EmployeeDto
            {
                Id = employee.Id,
                Name = employee.Name,
                Position = employee.Position,
                DepartmentName = employee.Department?.Name,
                ManagerId = employee.ManagerId,
                ManagerName = employee.Manager?.Name
            };
        }
        private async Task<List<EmployeeDto>> GetSubordinatesDtoRecursive(int managerId)
        {
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Manager)
                .Where(e => e.ManagerId == managerId)
                .ToListAsync();

            var result = new List<EmployeeDto>();
            foreach (var emp in employees)
            {
                var dto = MapEmployeeToDto(emp);
                dto.Subordinates = await GetSubordinatesDtoRecursive(emp.Id);
                result.Add(dto);
            }

            return result;
        }
        [HttpGet("GetAllSubordinates/{managerId}")]
        public async Task<IActionResult> GetAllSubordinates(int managerId)
        {
            var manager = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Manager)
                .FirstOrDefaultAsync(e => e.Id == managerId);


            if (manager == null)
            {
                return NotFound("Manager not found.");
            }

            var managerDto = MapEmployeeToDto(manager);
            managerDto.Subordinates = await GetSubordinatesDtoRecursive(managerId);

            return Ok(managerDto);
        }

        [HttpGet("GetAllManagers")]
        public async Task<ActionResult<IEnumerable<Employee>>> GetAllManagers()
        {
            var managers = await _context.Employees
                .Where(e => e.Subordinates.Any())
                .Include(e => e.Department)
                .Include(e => e.Manager)
                .ToListAsync();

            if (managers == null || !managers.Any())
            {
                return NotFound("No managers found.");
            }
            var employeeDtos = managers.Select(e => MapEmployeeToDto(e)).ToList();

            return Ok(employeeDtos);
        }

        [HttpGet("GetEmailByName")]
        public async Task<IActionResult> GetEmailByName(string name)
        {
            var employees = await _context.Employees.ToListAsync();

            Dictionary<string, Employee> emailHashTable = new();
            foreach (var emp in employees)
            {
                if (!String.IsNullOrEmpty(name))
                    emailHashTable[emp.Name] = emp;
            }
            if (emailHashTable.TryGetValue(name, out var result))
            {
                return Ok(result.Email);
            }
            return NotFound("The employee you searching for not found!");
        }


        public class GraphNode
        {
            public int Id { get; set; }
            public string? Name { get; set; }

            public List<GraphNode>? Subordinates { get; set; } = new();

        }

        public class GraphBuilder
        {
            public Dictionary<int, GraphNode> BuildEmployeeGraph(List<Employee> employees)
            {
                var graph = new Dictionary<int, GraphNode>();
                foreach (var emp in employees)
                {
                    graph[emp.Id] = new GraphNode
                    {
                        Id = emp.Id,
                        Name = emp.Name

                    };

                }

                foreach (var emp in employees)
                {
                    if (emp.ManagerId.HasValue && graph.ContainsKey(emp.ManagerId.Value))
                    {
                        var managerNode = graph[emp.ManagerId.Value];
                        managerNode.Subordinates.Add(graph[emp.Id]);
                    }
                }
                return graph;
            }
        }

        [HttpGet("EmployeeGraph")]
        public async Task<IActionResult> GetEmployeeGraph()
        {
            var employees = await _context.Employees.ToListAsync();
            var graphBuilder = new GraphBuilder();
            var graph = graphBuilder.BuildEmployeeGraph(employees);

            return Ok(graph.Values);
        }

        [HttpGet("GetEmployeesByLevel")]
        public async Task<IActionResult> GetEmployeesByLevel()
        {
            var topLevelEmployees = await _context.Employees
                .Where(e => e.ManagerId == null)
                .Include(e => e.Department)
                .ToListAsync();

            if (!topLevelEmployees.Any())
            {
                return NotFound("No top-level employees found.");
            }

            List<List<EmployeeDto>> hierarchyByLevel = new();

            Queue<Employee> queue = new Queue<Employee>();

            foreach (var emp in topLevelEmployees)
            {
                queue.Enqueue(emp);
            }

            while (queue.Count > 0)
            {
                int levelSize = queue.Count;
                List<EmployeeDto> currentLevel = new();

                for (int i = 0; i < levelSize; i++)
                {
                    var current = queue.Dequeue();
                    currentLevel.Add(MapEmployeeToDto(current));

                    var subordinates = await _context.Employees
                        .Where(e => e.ManagerId == current.Id)
                        .Include(e => e.Department)
                        .ToListAsync();

                    foreach (var sub in subordinates)
                    {
                        queue.Enqueue(sub);
                    }
                }

                hierarchyByLevel.Add(currentLevel);
            }

            return Ok(new
            {
                TotalLevels = hierarchyByLevel.Count,
                Hierarchy = hierarchyByLevel
            }
            );
        }
        public class EmployeeTreeNode
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string? Position { get; set; }
            public string? DepartmentName { get; set; }

            public List<EmployeeTreeNode> Subordinates { get; set; } = new();
        }
        private async Task<List<EmployeeTreeNode>> BuildEmployeeTree()
        {
            var employees = await _context.Employees
                .Include(e => e.Department)
                .ToListAsync();

            var nodeLookup = employees.ToDictionary(
                e => e.Id,
                e => new EmployeeTreeNode
                {
                    Id = e.Id,
                    Name = e.Name,
                    Position = e.Position,
                    DepartmentName = e.Department?.Name
                });

            List<EmployeeTreeNode> roots = new();

            foreach (var emp in employees)
            {
                if (emp.ManagerId.HasValue)
                {
                    if (nodeLookup.TryGetValue(emp.ManagerId.Value, out var managerNode))
                    {
                        managerNode.Subordinates.Add(nodeLookup[emp.Id]);
                    }
                }
                else
                {
                    roots.Add(nodeLookup[emp.Id]);
                }
            }

            return roots;
        }
        [HttpGet("EmployeeTree")]
        public async Task<IActionResult> GetEmployeeTree()
        {
            var tree = await BuildEmployeeTree();
            return Ok(tree);
        }

    }
}

