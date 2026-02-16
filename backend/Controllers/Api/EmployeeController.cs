using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using UmiHealthPOS.DTOs;
using System.Security.Claims;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "superadmin")]
    public class EmployeeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(
            ApplicationDbContext context,
            ILogger<EmployeeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<EmployeeDto>>> GetEmployees([FromQuery] EmployeeFilterDto filter)
        {
            try
            {
                var query = _context.Employees.AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(filter.Search))
                {
                    query = query.Where(e => 
                        e.FirstName.Contains(filter.Search) ||
                        e.LastName.Contains(filter.Search) ||
                        e.Email.Contains(filter.Search) ||
                        e.EmployeeNumber.Contains(filter.Search) ||
                        e.Position.Contains(filter.Search));
                }

                if (!string.IsNullOrEmpty(filter.Position))
                {
                    query = query.Where(e => e.Position == filter.Position);
                }

                if (filter.IsActive.HasValue)
                {
                    query = query.Where(e => e.IsActive == filter.IsActive.Value);
                }

                if (filter.HireDateFrom.HasValue)
                {
                    query = query.Where(e => e.HireDate >= filter.HireDateFrom.Value);
                }

                if (filter.HireDateTo.HasValue)
                {
                    query = query.Where(e => e.HireDate <= filter.HireDateTo.Value);
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var employees = await query
                    .OrderBy(e => e.LastName)
                    .ThenBy(e => e.FirstName)
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(e => new EmployeeDto
                    {
                        Id = e.Id,
                        FirstName = e.FirstName,
                        LastName = e.LastName,
                        Email = e.Email,
                        PhoneNumber = e.PhoneNumber,
                        Position = e.Position,
                        EmployeeNumber = e.EmployeeNumber,
                        HireDate = e.HireDate,
                        Salary = e.Salary,
                        IsActive = e.IsActive,
                        CreatedAt = e.CreatedAt,
                        UpdatedAt = e.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(new PagedResult<EmployeeDto>
                {
                    Data = employees,
                    TotalCount = totalCount,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employees");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<EmployeeStatsDto>> GetEmployeeStats()
        {
            try
            {
                var now = DateTime.UtcNow;
                var monthStart = new DateTime(now.Year, now.Month, 1);

                var query = _context.Employees.AsQueryable();

                var totalEmployees = await query.CountAsync();
                var activeEmployees = await query.CountAsync(e => e.IsActive);
                var inactiveEmployees = await query.CountAsync(e => !e.IsActive);
                var newHiresThisMonth = await query.CountAsync(e => e.HireDate >= monthStart);
                var averageSalary = await query.Where(e => e.IsActive).AverageAsync(e => e.Salary);

                var positionCounts = await query
                    .Where(e => e.IsActive)
                    .GroupBy(e => e.Position)
                    .Select(g => new { Position = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Position, x => x.Count);

                return Ok(new EmployeeStatsDto
                {
                    TotalEmployees = totalEmployees,
                    ActiveEmployees = activeEmployees,
                    InactiveEmployees = inactiveEmployees,
                    NewHiresThisMonth = newHiresThisMonth,
                    AverageSalary = Math.Round(averageSalary, 2),
                    PositionCounts = positionCounts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee stats");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EmployeeDto>> GetEmployee(int id)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);

                if (employee == null)
                {
                    return NotFound(new { error = "Employee not found" });
                }

                return Ok(new EmployeeDto
                {
                    Id = employee.Id,
                    FirstName = employee.FirstName,
                    LastName = employee.LastName,
                    Email = employee.Email,
                    PhoneNumber = employee.PhoneNumber,
                    Position = employee.Position,
                    EmployeeNumber = employee.EmployeeNumber,
                    HireDate = employee.HireDate,
                    Salary = employee.Salary,
                    IsActive = employee.IsActive,
                    CreatedAt = employee.CreatedAt,
                    UpdatedAt = employee.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<EmployeeDto>> CreateEmployee([FromBody] CreateEmployeeRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                // Check if employee number already exists
                if (!string.IsNullOrEmpty(request.EmployeeNumber))
                {
                    var existingEmployee = await _context.Employees
                        .FirstOrDefaultAsync(e => e.EmployeeNumber == request.EmployeeNumber);

                    if (existingEmployee != null)
                    {
                        return BadRequest(new { error = "Employee number already exists" });
                    }
                }

                var employee = new Employee
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    Position = request.Position,
                    EmployeeNumber = request.EmployeeNumber,
                    HireDate = request.HireDate,
                    Salary = request.Salary,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.Employees.AddAsync(employee);
                await _context.SaveChangesAsync();

                var employeeDto = new EmployeeDto
                {
                    Id = employee.Id,
                    FirstName = employee.FirstName,
                    LastName = employee.LastName,
                    Email = employee.Email,
                    PhoneNumber = employee.PhoneNumber,
                    Position = employee.Position,
                    EmployeeNumber = employee.EmployeeNumber,
                    HireDate = employee.HireDate,
                    Salary = employee.Salary,
                    IsActive = employee.IsActive,
                    CreatedAt = employee.CreatedAt,
                    UpdatedAt = employee.UpdatedAt
                };

                return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employeeDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating employee");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<EmployeeDto>> UpdateEmployee(int id, [FromBody] UpdateEmployeeRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                {
                    return NotFound(new { error = "Employee not found" });
                }

                // Check if employee number already exists (excluding this employee)
                if (!string.IsNullOrEmpty(request.EmployeeNumber))
                {
                    var existingEmployee = await _context.Employees
                        .FirstOrDefaultAsync(e => e.EmployeeNumber == request.EmployeeNumber && e.Id != id);

                    if (existingEmployee != null)
                    {
                        return BadRequest(new { error = "Employee number already exists" });
                    }
                }

                employee.FirstName = request.FirstName;
                employee.LastName = request.LastName;
                employee.Email = request.Email;
                employee.PhoneNumber = request.PhoneNumber;
                employee.Position = request.Position;
                employee.EmployeeNumber = request.EmployeeNumber;
                employee.HireDate = request.HireDate;
                employee.Salary = request.Salary;
                employee.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var employeeDto = new EmployeeDto
                {
                    Id = employee.Id,
                    FirstName = employee.FirstName,
                    LastName = employee.LastName,
                    Email = employee.Email,
                    PhoneNumber = employee.PhoneNumber,
                    Position = employee.Position,
                    EmployeeNumber = employee.EmployeeNumber,
                    HireDate = employee.HireDate,
                    Salary = employee.Salary,
                    IsActive = employee.IsActive,
                    CreatedAt = employee.CreatedAt,
                    UpdatedAt = employee.UpdatedAt
                };

                return Ok(employeeDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult> UpdateEmployeeStatus(int id, [FromBody] UpdateEmployeeStatusRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                {
                    return NotFound(new { error = "Employee not found" });
                }

                employee.IsActive = request.IsActive;
                employee.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = $"Employee status updated to {(request.IsActive ? "Active" : "Inactive")}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee status");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteEmployee(int id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                {
                    return NotFound(new { error = "Employee not found" });
                }

                // Check if employee has associated user branches
                var hasUserBranches = await _context.UserBranches
                    .AnyAsync(ub => ub.UserId == employee.Id.ToString());

                if (hasUserBranches)
                {
                    return BadRequest(new { error = "Cannot delete employee with associated user accounts" });
                }

                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Employee deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("positions")]
        public async Task<ActionResult<List<string>>> GetPositions()
        {
            try
            {
                var positions = await _context.Employees
                    .Where(e => e.IsActive)
                    .Select(e => e.Position)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToListAsync();

                return Ok(positions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving positions");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("export")]
        public async Task<ActionResult> ExportEmployees([FromQuery] EmployeeFilterDto filter)
        {
            try
            {
                // Set a large page size for export
                filter.PageSize = 10000;
                filter.Page = 1;

                var result = await GetEmployees(filter);
                if (result.Result is OkObjectResult okResult && okResult.Value is PagedResult<EmployeeDto> pagedResult)
                {
                    var csv = GenerateEmployeeCsv(pagedResult.Data);
                    var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
                    
                    return File(bytes, "text/csv", $"employees_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
                }

                return StatusCode(500, new { error = "Failed to export employees" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting employees");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("import")]
        public async Task<ActionResult> ImportEmployees([FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file uploaded" });
                }

                if (!file.FileName.EndsWith(".csv"))
                {
                    return BadRequest(new { error = "Only CSV files are allowed" });
                }

                var employees = new List<CreateEmployeeRequest>();
                using (var reader = new System.IO.StreamReader(file.OpenReadStream()))
                {
                    var header = reader.ReadLine();
                    if (header == null)
                    {
                        return BadRequest(new { error = "Empty file" });
                    }

                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var values = line.Split(',');
                        if (values.Length >= 6)
                        {
                            employees.Add(new CreateEmployeeRequest
                            {
                                FirstName = values[0]?.Trim() ?? string.Empty,
                                LastName = values[1]?.Trim() ?? string.Empty,
                                Email = values[2]?.Trim() ?? string.Empty,
                                PhoneNumber = values[3]?.Trim() ?? string.Empty,
                                Position = values[4]?.Trim() ?? string.Empty,
                                EmployeeNumber = values[5]?.Trim() ?? string.Empty,
                                Salary = decimal.TryParse(values[6], out var salary) ? salary : 0
                            });
                        }
                    }
                }

                var importedCount = 0;
                var errors = new List<string>();

                foreach (var employee in employees)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(employee.FirstName) || string.IsNullOrEmpty(employee.LastName))
                        {
                            errors.Add($"Skipping invalid employee: {employee.FirstName} {employee.LastName}");
                            continue;
                        }

                        var existingEmployee = await _context.Employees
                            .FirstOrDefaultAsync(e => e.EmployeeNumber == employee.EmployeeNumber);

                        if (existingEmployee != null)
                        {
                            errors.Add($"Employee number {employee.EmployeeNumber} already exists");
                            continue;
                        }

                        var newEmployee = new Employee
                        {
                            FirstName = employee.FirstName,
                            LastName = employee.LastName,
                            Email = employee.Email,
                            PhoneNumber = employee.PhoneNumber,
                            Position = employee.Position,
                            EmployeeNumber = employee.EmployeeNumber,
                            HireDate = employee.HireDate,
                            Salary = employee.Salary,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        await _context.Employees.AddAsync(newEmployee);
                        importedCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error importing employee {employee.FirstName} {employee.LastName}: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = $"Imported {importedCount} employees successfully",
                    importedCount = importedCount,
                    errors = errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing employees");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        private string GenerateEmployeeCsv(List<EmployeeDto> employees)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("First Name,Last Name,Email,Phone Number,Position,Employee Number,Hire Date,Salary,Status,Created At");

            foreach (var employee in employees)
            {
                csv.AppendLine($"\"{employee.FirstName}\"," +
                    $"\"{employee.LastName}\"," +
                    $"\"{employee.Email}\"," +
                    $"\"{employee.PhoneNumber}\"," +
                    $"\"{employee.Position}\"," +
                    $"\"{employee.EmployeeNumber}\"," +
                    $"{employee.HireDate:yyyy-MM-dd}," +
                    $"{employee.Salary}," +
                    $"{(employee.IsActive ? "Active" : "Inactive")}," +
                    $"{employee.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            }

            return csv.ToString();
        }
    }

    public class UpdateEmployeeStatusRequest
    {
        public bool IsActive { get; set; }
    }
}


