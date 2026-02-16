using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UmiHealthPOS.Services;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly IAuthService _authService;

        public EmployeeController(IEmployeeService employeeService, IAuthService authService)
        {
            _employeeService = employeeService;
            _authService = authService;
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployees()
        {
            try
            {
                var tenantId = _authService.GetCurrentTenantId();
                var employees = await _employeeService.GetAllEmployeesAsync(tenantId);
                return Ok(employees);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEmployee(int id)
        {
            try
            {
                var tenantId = _authService.GetCurrentTenantId();
                var employee = await _employeeService.GetEmployeeByIdAsync(id, tenantId);
                
                if (employee == null)
                    return NotFound(new { error = "Employee not found" });
                
                return Ok(employee);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateEmployee([FromBody] Employee employee)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { error = "Invalid employee data" });

                var tenantId = _authService.GetCurrentTenantId();
                var createdEmployee = await _employeeService.CreateEmployeeAsync(employee, tenantId);
                return CreatedAtAction(nameof(GetEmployee), new { id = createdEmployee.Id }, createdEmployee);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] Employee employee)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { error = "Invalid employee data" });

                var tenantId = _authService.GetCurrentTenantId();
                employee.Id = id;
                var updatedEmployee = await _employeeService.UpdateEmployeeAsync(employee, tenantId);
                return Ok(updatedEmployee);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            try
            {
                var tenantId = _authService.GetCurrentTenantId();
                var result = await _employeeService.DeleteEmployeeAsync(id, tenantId);
                
                if (!result)
                    return NotFound(new { error = "Employee not found" });
                
                return Ok(new { message = "Employee deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id)
        {
            try
            {
                var tenantId = _authService.GetCurrentTenantId();
                var newPassword = await _employeeService.ResetPasswordAsync(id, tenantId);
                return Ok(new { password = newPassword });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("{id}/view-password")]
        public async Task<IActionResult> ViewPassword(int id)
        {
            try
            {
                var tenantId = _authService.GetCurrentTenantId();
                var password = await _employeeService.GetPasswordAsync(id, tenantId);
                return Ok(new { password });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
