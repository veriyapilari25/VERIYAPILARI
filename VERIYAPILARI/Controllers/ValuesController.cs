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
        private static readonly Stack<Employee> _deletedEmployees = new Stack<Employee>();


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
            var existingEmployee = await _context.Employees
        .                           FirstOrDefaultAsync(e => e.Name == newEmployee.Name && e.DepartmentId == newEmployee.DepartmentId);

            if (existingEmployee != null)
            {
                return Conflict("An employee with the same name already exists in this department.");
            }

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
            if (employee.ManagerId == 0 || employee.ManagerId == 1)
            {
                return BadRequest("Employees with manager ID 0 or 1 cannot be deleted.");
            }
            _deletedEmployees.Push(employee);
            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        [HttpPost("UndoDeleteEmployee")]
        public async Task<IActionResult> UndoDeleteEmployee()
        {
            if (_deletedEmployees.Count == 0)
            {
                return BadRequest("No deleted employees to restore.");
            }

            var employeeToRestore = _deletedEmployees.Pop();

            try
            {
                // Create a new employee with the same properties except ID
                var newEmployee = new Employee
                {
                    // Do NOT set the ID - let the database generate a new one
                    Name = employeeToRestore.Name,
                    ManagerId = employeeToRestore.ManagerId,
                    Manager = employeeToRestore.Manager,
                    Email = employeeToRestore.Email,
                    DepartmentId = employeeToRestore.DepartmentId,
                    Department = employeeToRestore.Department,
                    Position = employeeToRestore.Position,
                    StartDate = employeeToRestore.StartDate,
                };

                // Add the new employee to the context
                _context.Employees.Add(newEmployee);
                await _context.SaveChangesAsync();

                // Return the newly created employee with its new ID
                return Ok(newEmployee);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error restoring employee: {ex.Message}");
            }
        }

        // Get count of employees that can be restored
        [HttpGet("GetUndoCount")]
        public IActionResult GetUndoCount()
        {
            return Ok(new { Count = _deletedEmployees.Count });
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
            public List<string> Skills { get; set; } = new();
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
                    DepartmentName = e.Department?.Name,
                    Skills = e.Skills ?? new List<string>(),
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

        [HttpGet("GetEmployeesBySkill")]
        public async Task<IActionResult> GetEmployeesBySkill([FromQuery] string skill)
        {
            if (string.IsNullOrEmpty(skill))
            {
                return BadRequest("Skill parameter is required.");
            }

            var normalizedSearch = skill.Trim().ToLower();


            var employees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Manager)
                .ToListAsync();

            Dictionary<string, List<Employee>> skillHashTable = new(StringComparer.OrdinalIgnoreCase);
            foreach (var employee in employees)
            {
                if (employee.Skills != null)
                {
                    foreach (var empskill in employee.Skills)
                    {
                        var normalizedSkill = empskill.Trim().ToLower();

                        if (!skillHashTable.ContainsKey(normalizedSkill))
                        {
                            skillHashTable[normalizedSkill] = new List<Employee>();
                        }
                        skillHashTable[normalizedSkill].Add(employee);
                    }
                }
            }
            var matchingEmployees = new List<Employee>();

            foreach (var kvp in skillHashTable)
            {
                if (kvp.Key.Contains(normalizedSearch))  // partial match inside the key
                {
                    matchingEmployees.AddRange(kvp.Value);
                }
            }

            if (matchingEmployees.Any())
            {
                var result = matchingEmployees
                    .Distinct()   // avoid duplicates
                    .Select(e => MapEmployeeToDto(e))
                    .ToList();

                return Ok(result);
            }

            return NotFound($"No employees found with skill: {skill}");
        }
    }
}

