using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Services
{
    public interface IEmployeeService
    {
        Task<IEnumerable<Employee>> GetAllEmployeesAsync(string tenantId);
        Task<Employee?> GetEmployeeByIdAsync(int id, string tenantId);
        Task<Employee> CreateEmployeeAsync(Employee employee, string tenantId);
        Task<Employee> UpdateEmployeeAsync(Employee employee, string tenantId);
        Task<bool> DeleteEmployeeAsync(int id, string tenantId);
        Task<string> ResetPasswordAsync(int id, string tenantId);
        Task<string> GetPasswordAsync(int id, string tenantId);
    }

    public class EmployeeService : IEmployeeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordService _passwordService;

        public EmployeeService(ApplicationDbContext context, IPasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        public async Task<IEnumerable<Employee>> GetAllEmployeesAsync(string tenantId)
        {
            return await _context.Employees
                .Where(e => e.TenantId == tenantId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<Employee?> GetEmployeeByIdAsync(int id, string tenantId)
        {
            return await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);
        }

        public async Task<Employee> CreateEmployeeAsync(Employee employee, string tenantId)
        {
            // Generate unique employee number
            employee.EmployeeNumber = await GenerateEmployeeIdAsync();
            employee.TenantId = tenantId;
            employee.CreatedAt = DateTime.UtcNow;
            employee.UpdatedAt = DateTime.UtcNow;

            // Hash password if provided
            if (!string.IsNullOrEmpty(employee.PasswordHash))
            {
                employee.PasswordHash = _passwordService.HashPassword(employee.PasswordHash);
            }

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return employee;
        }

        public async Task<Employee> UpdateEmployeeAsync(Employee employee, string tenantId)
        {
            var existingEmployee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == employee.Id && e.TenantId == tenantId);

            if (existingEmployee == null)
                throw new Exception("Employee not found");

            existingEmployee.FirstName = employee.FirstName;
            existingEmployee.LastName = employee.LastName;
            existingEmployee.Email = employee.Email;
            existingEmployee.Phone = employee.Phone;
            existingEmployee.Position = employee.Position;
            existingEmployee.Role = employee.Role;
            existingEmployee.Status = employee.Status;
            existingEmployee.Salary = employee.Salary;
            existingEmployee.IsActive = employee.IsActive;
            existingEmployee.UpdatedAt = DateTime.UtcNow;

            // Update password if provided
            if (!string.IsNullOrEmpty(employee.PasswordHash))
            {
                existingEmployee.PasswordHash = _passwordService.HashPassword(employee.PasswordHash);
            }

            await _context.SaveChangesAsync();
            return existingEmployee;
        }

        public async Task<bool> DeleteEmployeeAsync(int id, string tenantId)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);

            if (employee == null)
                return false;

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string> ResetPasswordAsync(int id, string tenantId)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);

            if (employee == null)
                throw new Exception("Employee not found");

            var newPassword = _passwordService.GenerateRandomPassword();
            employee.PasswordHash = _passwordService.HashPassword(newPassword);
            employee.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return newPassword;
        }

        public async Task<string> GetPasswordAsync(int id, string tenantId)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);

            if (employee == null)
                throw new Exception("Employee not found");

            // For security reasons, we don't return the actual password
            // Instead, we return whether the employee has a password set
            return string.IsNullOrEmpty(employee.PasswordHash) ? "No password set" : "Password is set";
        }

        private async Task<string> GenerateEmployeeIdAsync()
        {
            string employeeId;
            int counter = 1;

            do
            {
                employeeId = $"EMP{counter:D3}";
                var exists = await _context.Employees
                    .AnyAsync(e => e.EmployeeNumber == employeeId);

                if (!exists)
                    break;

                counter++;
            } while (counter <= 999);

            return employeeId;
        }
    }
}
