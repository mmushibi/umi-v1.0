using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Services
{
    public interface IEmployeeService
    {
        Task<IEnumerable<Employee>> GetAllEmployeesAsync(int tenantId);
        Task<Employee?> GetEmployeeByIdAsync(int id, int tenantId);
        Task<Employee> CreateEmployeeAsync(Employee employee, int tenantId);
        Task<Employee> UpdateEmployeeAsync(Employee employee, int tenantId);
        Task<bool> DeleteEmployeeAsync(int id, int tenantId);
        Task<string> ResetPasswordAsync(int id, int tenantId);
        Task<string> GetPasswordAsync(int id, int tenantId);
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

        public async Task<IEnumerable<Employee>> GetAllEmployeesAsync(int tenantId)
        {
            return await _context.Employees
                .Where(e => e.TenantId == tenantId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<Employee?> GetEmployeeByIdAsync(int id, int tenantId)
        {
            return await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);
        }

        public async Task<Employee> CreateEmployeeAsync(Employee employee, int tenantId)
        {
            // Generate unique employee number
            employee.EmployeeNumber = await GenerateEmployeeIdAsync();
            employee.TenantId = tenantId.ToString();
            employee.CreatedAt = DateTime.UtcNow;
            employee.UpdatedAt = DateTime.UtcNow;
            
            // Note: Employee entity doesn't have Password property
            // Password handling would need to be implemented separately
            
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
            
            return employee;
        }

        public async Task<Employee> UpdateEmployeeAsync(Employee employee, int tenantId)
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
            // Note: Employee doesn't have Avatar, LicenseNumber, ZambiaRegNumber properties
            existingEmployee.UpdatedAt = DateTime.UtcNow;

            // Note: Employee entity doesn't have Password property
            // Password update would need to be implemented separately

            await _context.SaveChangesAsync();
            return existingEmployee;
        }

        public async Task<bool> DeleteEmployeeAsync(int id, int tenantId)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);

            if (employee == null)
                return false;

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string> ResetPasswordAsync(int id, int tenantId)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);

            if (employee == null)
                throw new Exception("Employee not found");

            // Note: Employee entity doesn't have Password property
            // Password reset would need to be implemented separately
            var newPassword = _passwordService.GenerateRandomPassword();
            // employee.Password = _passwordService.HashPassword(newPassword); // Commented out as property doesn't exist
            employee.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return newPassword;
        }

        public async Task<string> GetPasswordAsync(int id, int tenantId)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);

            if (employee == null)
                throw new Exception("Employee not found");

            // Note: Employee entity doesn't have Password property
            // Password retrieval would need to be implemented separately
            return "Password not available - property doesn't exist";
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
