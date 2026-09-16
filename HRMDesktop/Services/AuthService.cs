using HRMDesktop.Models;

namespace HRMDesktop.Services
{
    public static class AuthService
    {
        public static UserAccount Login(string username, string password)
        {
            username = (username ?? string.Empty).Trim().ToLower();
            if (username == "admin" && password == "123")
            {
                return new UserAccount { EmployeeId = 0, Username = "admin", FullName = "Quản trị viên HRM", Role = "Admin", Department = "Nhân sự", Email = "admin@hrm.local" };
            }
            if ((username == "employee" || username == "user") && password == "123")
            {
                var employee = MockDataService.GetEmployee(1);
                if (employee == null || employee.Status != "Đang làm việc") return null;
                return new UserAccount { EmployeeId = employee.Id, Username = "employee", FullName = employee.FullName, Role = "Employee", Department = employee.Department, Email = employee.Email };
            }
            return null;
        }
    }
}
