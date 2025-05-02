using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VERIYAPILARI.Data;
using VERIYAPILARI.Models.Entities;

namespace VERIYAPILARI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public DepartmentController(ApplicationDBContext context)
        {
            _context = context;
        }

        // GET: api/Departments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Department>>> GetDepartments()
        {
            try
            {
                var departments = await _context.Departments.ToListAsync();
                return Ok(departments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/Departments/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Department>> GetDepartment(int id)
        {
            try
            {
                var department = await _context.Departments.FindAsync(id);

                if (department == null)
                {
                    return NotFound("Department not found");
                }

                return Ok(department);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/Departments/{id}/Employees
        [HttpGet("{id}/Employees")]
        public async Task<IActionResult> GetDepartmentEmployees(int id)
        {
            try
            {
                var department = await _context.Departments
                    .Where(d => d.Id == id)
                    .Include(d => d.Employees)
                    .ThenInclude(e => e.Manager)
                    .FirstOrDefaultAsync();

                if (department == null)
                {
                    return NotFound("Department not found");
                }

                var departmentDto = new
                {
                    DepartmentId = department.Id,
                    DepartmentName = department.Name,
                    Employees = department.Employees.Select(e => new
                    {
                        e.Id,
                        e.Name,
                        e.Position,
                        ManagerName = e.Manager?.Name,
                        ManagerId = e.ManagerId
                    }).ToList()
                };

                return Ok(departmentDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // POST: api/Departments
        [HttpPost]
        public async Task<ActionResult<Department>> CreateDepartment(Department department)
        {
            try
            {
                _context.Departments.Add(department);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetDepartment), new { id = department.Id }, department);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PUT: api/Departments/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDepartment(int id, Department department)
        {
            if (id != department.Id)
            {
                return BadRequest("ID mismatch");
            }

            try
            {
                _context.Entry(department).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DepartmentExists(id))
                {
                    return NotFound("Department not found");
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // DELETE: api/Departments/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            try
            {
                var department = await _context.Departments.FindAsync(id);
                if (department == null)
                {
                    return NotFound("Department not found");
                }

                // Check if there are employees in this department
                var hasEmployees = await _context.Employees.AnyAsync(e => e.DepartmentId == id);
                if (hasEmployees)
                {
                    return BadRequest("Cannot delete department that has employees assigned to it.");
                }

                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private bool DepartmentExists(int id)
        {
            return _context.Departments.Any(e => e.Id == id);
        }
    }
}