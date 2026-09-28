using System.Linq;
using HRMDesktop.Models;

namespace HRMDesktop.Services
{
    public static class AuthService
    {
        public static string LastError { get; private set; }

        public static UserAccount Login(string username, string password)
        {
            LastError = null;
            username = (username ?? string.Empty).Trim().ToLower();
            var credential = HrmDataService.Credentials.FirstOrDefault(x => x.Username == username);
            if (credential == null)
            {
                LastError = "Tên đăng nhập không tồn tại.";
                return null;
            }
            if (credential.IsLocked)
            {
                LastError = "Tài khoản đã bị khóa. Hãy liên hệ Admin để kiểm tra trạng thái nhân sự.";
                return null;
            }
            if (!PasswordSecurity.Verify(password, credential))
            {
                LastError = "Mật khẩu không đúng.";
                return null;
            }
            if (credential.Role == "Admin")
            {
                return BuildAccount(credential, "Quản trị viên HRM", "Nhân sự", "admin@hrm.local");
            }
            var employee = HrmDataService.GetEmployee(credential.EmployeeId);
            if (employee == null || employee.Status != "Đang làm việc" || !HrmBusinessService.IsEmployedOn(employee, SystemTimeService.Today))
            {
                LastError = "Tài khoản không thuộc nhân viên đang làm việc.";
                return null;
            }
            return BuildAccount(credential, employee.FullName, employee.Department, employee.Email);
        }

        private static UserAccount BuildAccount(UserCredential credential, string fullName, string department, string email)
        {
            return new UserAccount
            {
                EmployeeId = credential.EmployeeId,
                Username = credential.Username,
                FullName = fullName,
                Role = credential.Role,
                Department = department,
                Email = email,
                AttendanceNotificationEnabled = credential.AttendanceNotificationEnabled,
                LeaveNotificationEnabled = credential.LeaveNotificationEnabled,
                SalaryNotificationEnabled = credential.SalaryNotificationEnabled
            };
        }
    }
}
