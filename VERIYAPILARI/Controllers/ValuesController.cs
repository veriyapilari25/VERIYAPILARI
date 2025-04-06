﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
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
        // Use a thread-safe collection instead of a static list
        private readonly ApplicationDBContext _context = context;

        [HttpGet("GetEmployees")]
        public async Task<ActionResult<List<Employee>>> GetEmployees()
        {
            var employees = await _context.Employees
                    .Include(e => e.Department)  // Optionally include Department as well
                    //.Include(e => e.Subordinates)  // Optionally include Subordinates if needed
                    .Include(e => e.Manager)  // Include Manager in the query
                    .ToListAsync();
            var employeeDtos = employees.Select(e => new EmployeeDto
            {
                Id = e.Id,
                Name = e.Name,
                Position = e.Position,
                DepartmentName = e.Department?.Name,
                ManagerId = e.ManagerId,
                ManagerName = e.Manager?.Name,
                Subordinates = new List<EmployeeDto>() // leave empty or load separately if needed
            }).ToList();

            return Ok(employeeDtos);
        }

        [HttpGet("GetEmployeeByID/{id}")]
        public async Task<ActionResult<Employee>> GetEmployeeByID(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.Manager)
                .Include(e => e.Department)
                .Include(e => e.Subordinates)
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
                    ManagerName = employee.Name, // or sub.Manager?.Name
                    Subordinates = new List<EmployeeDto>() // To avoid deep recursion
                }).ToList() ?? new()
            };
        }

        [HttpPost("PostEmployee")]
        public async Task<ActionResult<Employee>> PostEmployee(Employee newEmployee)
        {
            if (newEmployee == null)
                return BadRequest("Employee data is required.");

            // Handle empty list case
            newEmployee.StartDate = DateTime.UtcNow; // Auto-assign start date
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
            employee.StartDate = updatedEmployee.StartDate; // You might want to use the provided date instead of UTC now
            employee.DepartmentId = updatedEmployee.DepartmentId;
            employee.Department = updatedEmployee.Department;
            if (updatedEmployee.ManagerId != employee.ManagerId)
            {
                // If the manager is changed, update the manager field and subordinates
                employee.ManagerId = updatedEmployee.ManagerId;
                employee.Manager = updatedEmployee.Manager;

                // Optionally, you could update the subordinates list, depending on your requirements
                if (employee.Manager != null)
                {
                    // Add this employee as a subordinate to the new manager if needed
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
  
        private async Task<List<Employee>> GetSubordinatesRecursive(int managerId)
        {
            var employees = await _context.Employees
                .Where(e => e.ManagerId == managerId)
                .ToListAsync();

            foreach (var emp in employees)
            {
                emp.Subordinates = await GetSubordinatesRecursive(emp.Id);
            }

            return employees;
        }
        [HttpGet("GetDepartmentsEmployees/{id}")]
        public async Task<IActionResult> GetDepartmentsEmployees(int id)
        {
            var departments = await _context.Departments
                                    .Where(d => d.Id == id) // Assuming you want to fetch by Department Id
                                    .Include(d => d.Employees)  // Include employees within the department
                                    .ThenInclude(e => e.Manager)  // Include the manager of each employee
                                    .ToListAsync();

            if (departments == null || !departments.Any())
            {
                return NotFound();
            }

            // You can return only the employees and their managers (without subordinates)
            var departmentDtos = departments.Select(d => new
            {
                DepartmentId = d.Id,
                DepartmentName = d.Name,
                Employees = d.Employees.Select(e => new
                {
                    e.Id,
                    e.Name,
                    e.Position,
                    ManagerName = e.Manager?.Name,  // Only include the manager name
                    ManagerId = e.ManagerId  // Include manager id (or can skip if not needed)

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
            var subordinates = await GetSubordinatesDtoRecursive(managerId);

            if (!subordinates.Any())
            {
                return NotFound("No employees found under this manager.");
            }

            return Ok(subordinates);
        }

        [HttpGet("GetAllManagers")]
        public async Task<ActionResult<IEnumerable<Employee>>> GetAllManagers()
        {
            var managers = await _context.Employees
                .Where(e => e.Subordinates.Any())
                .Include(e => e.Department)  // Optionally include Department as well
                .Include(e => e.Manager)  // Include Manager in the query
                .ToListAsync();

            if (managers == null || !managers.Any())
            {
                return NotFound("No managers found.");
            }
            var employeeDtos = managers.Select(e => new EmployeeDto
            {
                Id = e.Id,
                Name = e.Name,
                Position = e.Position,
                DepartmentName = e.Department?.Name,
                ManagerId = e.ManagerId,
                ManagerName = e.Manager?.Name,
                Subordinates = new List<EmployeeDto>() // leave empty or load separately if needed
            }).ToList();

            return Ok(employeeDtos);
        }


    }
}

