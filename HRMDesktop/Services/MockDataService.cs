using System;
using System.Collections.ObjectModel;
using System.Linq;
using HRMDesktop.Models;

namespace HRMDesktop.Services
{
    public static class MockDataService
    {
        public static ObservableCollection<Employee> Employees { get; private set; }
        public static ObservableCollection<AttendanceRecord> Attendance { get; private set; }
        public static ObservableCollection<LeaveRequest> LeaveRequests { get; private set; }
        public static ObservableCollection<SalaryRecord> Salaries { get; private set; }
        public static ObservableCollection<AttendanceAdjustmentRequest> AttendanceAdjustments { get; private set; }

        static MockDataService()
        {
            Employees = new ObservableCollection<Employee>
            {
                new Employee { Id=1, Code="NV001", FullName="Nguyễn Văn An", Email="an.nguyen@hrm.local", Phone="0905123456", Department="Công nghệ thông tin", Position="Lập trình viên", DateOfBirth=new DateTime(1998,5,15), BaseSalary=18000000, Status="Đang làm việc" },
                new Employee { Id=2, Code="NV002", FullName="Trần Thị Bình", Email="binh.tran@hrm.local", Phone="0905234567", Department="Nhân sự", Position="Chuyên viên nhân sự", DateOfBirth=new DateTime(1997,9,22), BaseSalary=16000000, Status="Đang làm việc" },
                new Employee { Id=3, Code="NV003", FullName="Lê Hoàng Cường", Email="cuong.le@hrm.local", Phone="0905345678", Department="Marketing", Position="Content Executive", DateOfBirth=new DateTime(1996,2,10), BaseSalary=15000000, Status="Đang làm việc" },
                new Employee { Id=4, Code="NV004", FullName="Phạm Minh Đức", Email="duc.pham@hrm.local", Phone="0905456789", Department="Kinh doanh", Position="Nhân viên kinh doanh", DateOfBirth=new DateTime(1999,11,8), BaseSalary=14000000, Status="Đang làm việc" },
                new Employee { Id=5, Code="NV005", FullName="Võ Thị Như", Email="nhu.vo@hrm.local", Phone="0905567890", Department="Tài chính", Position="Kế toán viên", DateOfBirth=new DateTime(1998,7,30), BaseSalary=17000000, Status="Đang làm việc" }
            };

            Attendance = new ObservableCollection<AttendanceRecord>
            {
                new AttendanceRecord { Id=1, EmployeeId=2, EmployeeName="Trần Thị Bình", EmployeeCode="NV002", Department="Nhân sự", WorkDate=DateTime.Today, CheckIn="08:15", CheckOut="17:32", Status="Đã kết thúc" },
                new AttendanceRecord { Id=2, EmployeeId=3, EmployeeName="Lê Hoàng Cường", EmployeeCode="NV003", Department="Marketing", WorkDate=DateTime.Today, CheckIn="08:25", CheckOut="--", Status="Đang làm việc" },
                new AttendanceRecord { Id=3, EmployeeId=1, EmployeeName="Nguyễn Văn An", EmployeeCode="NV001", Department="Công nghệ thông tin", WorkDate=DateTime.Today.AddDays(-1), CheckIn="08:05", CheckOut="17:36", Status="Đã kết thúc" },
                new AttendanceRecord { Id=4, EmployeeId=1, EmployeeName="Nguyễn Văn An", EmployeeCode="NV001", Department="Công nghệ thông tin", WorkDate=DateTime.Today.AddDays(-2), CheckIn="08:22", CheckOut="17:20", Status="Đã kết thúc" }
            };

            AttendanceAdjustments = new ObservableCollection<AttendanceAdjustmentRequest>
            {
                new AttendanceAdjustmentRequest { Id=1, EmployeeId=4, EmployeeName="Phạm Minh Đức", EmployeeCode="NV004", Department="Kinh doanh", WorkDate=DateTime.Today.AddDays(-1), RequestedCheckIn="08:10", RequestedCheckOut="17:25", Reason="Quên chấm công khi đi gặp khách hàng", SubmittedAt=DateTime.Today.AddHours(8), Status="Chờ duyệt" }
            };

            LeaveRequests = new ObservableCollection<LeaveRequest>
            {
                new LeaveRequest { Id=1, EmployeeId=1, EmployeeName="Nguyễn Văn An", EmployeeCode="NV001", LeaveType="Nghỉ phép năm", FromDate=DateTime.Today.AddDays(3), ToDate=DateTime.Today.AddDays(4), SubmittedAt=DateTime.Today.AddDays(-1).AddHours(9), Reason="Giải quyết việc gia đình", Status="Chờ duyệt" },
                new LeaveRequest { Id=2, EmployeeId=2, EmployeeName="Trần Thị Bình", EmployeeCode="NV002", LeaveType="Nghỉ ốm", FromDate=DateTime.Today.AddDays(-4), ToDate=DateTime.Today.AddDays(-3), SubmittedAt=DateTime.Today.AddDays(-6).AddHours(8), ReviewedAt=DateTime.Today.AddDays(-5).AddHours(10), Reason="Điều trị theo chỉ định", Status="Đã duyệt" }
            };

            Salaries = new ObservableCollection<SalaryRecord>
            {
                new SalaryRecord { Id=1, EmployeeId=1, EmployeeName="Nguyễn Văn An", EmployeeCode="NV001", Month=DateTime.Today.ToString("MM/yyyy"), BaseSalary=18000000, Bonus=2500000, Deduction=750000, Status="Đã thanh toán" },
                new SalaryRecord { Id=2, EmployeeId=2, EmployeeName="Trần Thị Bình", EmployeeCode="NV002", Month=DateTime.Today.ToString("MM/yyyy"), BaseSalary=16000000, Bonus=1800000, Deduction=600000, Status="Chờ thanh toán" },
                new SalaryRecord { Id=3, EmployeeId=3, EmployeeName="Lê Hoàng Cường", EmployeeCode="NV003", Month=DateTime.Today.ToString("MM/yyyy"), BaseSalary=15000000, Bonus=1200000, Deduction=500000, Status="Chờ thanh toán" },
                new SalaryRecord { Id=4, EmployeeId=1, EmployeeName="Nguyễn Văn An", EmployeeCode="NV001", Month=DateTime.Today.AddMonths(-1).ToString("MM/yyyy"), BaseSalary=18000000, Bonus=1800000, Deduction=500000, Status="Đã thanh toán" },
                new SalaryRecord { Id=5, EmployeeId=2, EmployeeName="Trần Thị Bình", EmployeeCode="NV002", Month=DateTime.Today.AddMonths(-1).ToString("MM/yyyy"), BaseSalary=16000000, Bonus=1200000, Deduction=350000, Status="Đã thanh toán" },
                new SalaryRecord { Id=6, EmployeeId=1, EmployeeName="Nguyễn Văn An", EmployeeCode="NV001", Month=DateTime.Today.AddMonths(-2).ToString("MM/yyyy"), BaseSalary=18000000, Bonus=1000000, Deduction=400000, Status="Đã thanh toán" }
            };

        }

        public static Employee GetEmployee(int id) { return Employees.FirstOrDefault(x => x.Id == id); }
    }
}
