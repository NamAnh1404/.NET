using System;
using System.Collections.ObjectModel;
using System.Linq;
using HRMDesktop.Models;

namespace HRMDesktop.Services
{
    public static class HrmDataService
    {
        public static string LastSaveError { get; private set; }
        public static ObservableCollection<Employee> Employees { get; private set; }
        public static ObservableCollection<AttendanceRecord> Attendance { get; private set; }
        public static ObservableCollection<LeaveRequest> LeaveRequests { get; private set; }
        public static ObservableCollection<SalaryRecord> Salaries { get; private set; }
        public static ObservableCollection<AttendanceAdjustmentRequest> AttendanceAdjustments { get; private set; }
        public static ObservableCollection<SalaryHistory> SalaryHistories { get; private set; }
        public static ObservableCollection<EmploymentPeriod> EmploymentPeriods { get; private set; }
        public static ObservableCollection<WorkShift> WorkShifts { get; private set; }
        public static ObservableCollection<Holiday> Holidays { get; private set; }
        public static ObservableCollection<UserCredential> Credentials { get; private set; }
        public static ObservableCollection<AuditLog> AuditLogs { get; private set; }

        static HrmDataService()
        {
            Employees = new ObservableCollection<Employee>
            {
                new Employee { Id=1, Code="NV001", FullName="Nguyễn Văn An", Email="an.nguyen@hrm.local", Phone="0905123456", Department="Công nghệ thông tin", Position="Lập trình viên", DateOfBirth=new DateTime(1998,5,15), HireDate=new DateTime(2024,1,2), AnnualLeaveAllowance=12, BaseSalary=18000000, Status="Đang làm việc" },
                new Employee { Id=2, Code="NV002", FullName="Trần Thị Bình", Email="binh.tran@hrm.local", Phone="0905234567", Department="Nhân sự", Position="Chuyên viên nhân sự", DateOfBirth=new DateTime(1997,9,22), HireDate=new DateTime(2024,2,1), AnnualLeaveAllowance=12, BaseSalary=16000000, Status="Đang làm việc" },
                new Employee { Id=3, Code="NV003", FullName="Lê Hoàng Cường", Email="cuong.le@hrm.local", Phone="0905345678", Department="Marketing", Position="Content Executive", DateOfBirth=new DateTime(1996,2,10), HireDate=new DateTime(2024,3,1), AnnualLeaveAllowance=12, BaseSalary=15000000, Status="Đang làm việc" },
                new Employee { Id=4, Code="NV004", FullName="Phạm Minh Đức", Email="duc.pham@hrm.local", Phone="0905456789", Department="Kinh doanh", Position="Nhân viên kinh doanh", DateOfBirth=new DateTime(1999,11,8), HireDate=new DateTime(2025,1,2), AnnualLeaveAllowance=12, BaseSalary=14000000, Status="Đang làm việc" },
                new Employee { Id=5, Code="NV005", FullName="Võ Thị Như", Email="nhu.vo@hrm.local", Phone="0905567890", Department="Tài chính", Position="Kế toán viên", DateOfBirth=new DateTime(1998,7,30), HireDate=new DateTime(2025,2,3), AnnualLeaveAllowance=12, BaseSalary=17000000, Status="Đang làm việc" }
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

            WorkShifts = new ObservableCollection<WorkShift>
            {
                new WorkShift { Id = 1, Name = "Ca hành chính", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(17, 30, 0), GraceMinutes = 15, IsOvernight = false }
            };
            Holidays = new ObservableCollection<Holiday>
            {
                new Holiday { Date = new DateTime(DateTime.Today.Year, 1, 1), Name = "Tết Dương lịch" },
                new Holiday { Date = new DateTime(DateTime.Today.Year, 4, 30), Name = "Ngày Giải phóng miền Nam" },
                new Holiday { Date = new DateTime(DateTime.Today.Year, 5, 1), Name = "Ngày Quốc tế Lao động" },
                new Holiday { Date = new DateTime(DateTime.Today.Year, 9, 2), Name = "Quốc khánh" }
            };
            SalaryHistories = new ObservableCollection<SalaryHistory>(Employees.Select((employee, index) => new SalaryHistory
            {
                Id = index + 1,
                EmployeeId = employee.Id,
                EffectiveFrom = employee.HireDate,
                BaseSalary = employee.BaseSalary
            }));
            EmploymentPeriods = new ObservableCollection<EmploymentPeriod>(Employees.Select((employee, index) => new EmploymentPeriod
            {
                Id = index + 1,
                EmployeeId = employee.Id,
                StartDate = employee.HireDate,
                EndDate = employee.TerminationDate
            }));
            Credentials = new ObservableCollection<UserCredential>
            {
                PasswordSecurity.CreateCredential(0, "admin", "123", "Admin"),
                PasswordSecurity.CreateCredential(1, "employee", "123", "Employee"),
                PasswordSecurity.CreateCredential(2, "nv002", "123", "Employee"),
                PasswordSecurity.CreateCredential(3, "nv003", "123", "Employee"),
                PasswordSecurity.CreateCredential(4, "nv004", "123", "Employee"),
                PasswordSecurity.CreateCredential(5, "nv005", "123", "Employee")
            };
            AuditLogs = new ObservableCollection<AuditLog>();

            var saved = DatabaseDataService.Load();
            if (saved != null)
            {
                ApplySnapshot(saved);
                EnsureDefaults();
                if (!DatabaseDataService.Save(CreateSnapshot()))
                    throw new InvalidOperationException("Không thể đồng bộ dữ liệu SQL Server: " + DatabaseDataService.LastError);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(DatabaseDataService.LastError))
                    throw new InvalidOperationException("Không thể kết nối HRMDatabase: " + DatabaseDataService.LastError);

                // Chỉ dùng XML cũ cho lần chuyển đổi đầu tiên, sau đó SQL Server là nguồn dữ liệu chính.
                var legacySnapshot = DataPersistenceService.Load();
                if (legacySnapshot != null) ApplySnapshot(legacySnapshot);
                EnsureDefaults();
                if (!DatabaseDataService.Save(CreateSnapshot()))
                    throw new InvalidOperationException("Không thể khởi tạo dữ liệu SQL Server: " + DatabaseDataService.LastError);
            }

        }

        public static Employee GetEmployee(int id) { return Employees.FirstOrDefault(x => x.Id == id); }

        public static void AddAudit(string actor, string action, string details)
        {
            int nextId = AuditLogs.Count == 0 ? 1 : AuditLogs.Max(x => x.Id) + 1;
            AuditLogs.Add(new AuditLog { Id = nextId, CreatedAt = SystemTimeService.Now, Actor = actor, Action = action, Details = details });
        }

        public static bool SaveChanges()
        {
            if (DatabaseDataService.Save(CreateSnapshot()))
            {
                LastSaveError = null;
                return true;
            }

            LastSaveError = string.IsNullOrWhiteSpace(DatabaseDataService.LastError) ? "Không thể lưu dữ liệu vào SQL Server." : DatabaseDataService.LastError;
            string saveError = LastSaveError;
            var persisted = DatabaseDataService.Load();
            if (persisted != null)
            {
                ApplySnapshot(persisted);
                EnsureDefaults();
            }
            LastSaveError = saveError;
            return false;
        }

        private static HrmDataSnapshot CreateSnapshot()
        {
            return new HrmDataSnapshot
            {
                Employees = Employees.ToList(),
                Attendance = Attendance.ToList(),
                LeaveRequests = LeaveRequests.ToList(),
                Salaries = Salaries.ToList(),
                AttendanceAdjustments = AttendanceAdjustments.ToList(),
                SalaryHistories = SalaryHistories.ToList(),
                EmploymentPeriods = EmploymentPeriods.ToList(),
                WorkShifts = WorkShifts.ToList(),
                Holidays = Holidays.ToList(),
                Credentials = Credentials.ToList(),
                AuditLogs = AuditLogs.ToList()
            };
        }

        private static void ApplySnapshot(HrmDataSnapshot snapshot)
        {
            if (snapshot.Employees != null && snapshot.Employees.Count > 0) Employees = new ObservableCollection<Employee>(snapshot.Employees);
            if (snapshot.Attendance != null) Attendance = new ObservableCollection<AttendanceRecord>(snapshot.Attendance);
            if (snapshot.LeaveRequests != null) LeaveRequests = new ObservableCollection<LeaveRequest>(snapshot.LeaveRequests);
            if (snapshot.Salaries != null) Salaries = new ObservableCollection<SalaryRecord>(snapshot.Salaries);
            if (snapshot.AttendanceAdjustments != null) AttendanceAdjustments = new ObservableCollection<AttendanceAdjustmentRequest>(snapshot.AttendanceAdjustments);
            if (snapshot.SalaryHistories != null) SalaryHistories = new ObservableCollection<SalaryHistory>(snapshot.SalaryHistories);
            EmploymentPeriods = snapshot.EmploymentPeriods == null ? new ObservableCollection<EmploymentPeriod>() : new ObservableCollection<EmploymentPeriod>(snapshot.EmploymentPeriods);
            if (snapshot.WorkShifts != null && snapshot.WorkShifts.Count > 0) WorkShifts = new ObservableCollection<WorkShift>(snapshot.WorkShifts);
            if (snapshot.Holidays != null) Holidays = new ObservableCollection<Holiday>(snapshot.Holidays);
            if (snapshot.Credentials != null && snapshot.Credentials.Count > 0) Credentials = new ObservableCollection<UserCredential>(snapshot.Credentials);
            if (snapshot.AuditLogs != null) AuditLogs = new ObservableCollection<AuditLog>(snapshot.AuditLogs);
        }

        private static void EnsureDefaults()
        {
            foreach (var employee in Employees)
            {
                if (employee.HireDate == default(DateTime)) employee.HireDate = new DateTime(2024, 1, 2);
                if (employee.AnnualLeaveAllowance <= 0) employee.AnnualLeaveAllowance = 12;
                if (!Credentials.Any(x => x.EmployeeId == employee.Id)) Credentials.Add(PasswordSecurity.CreateCredential(employee.Id, employee.Code.ToLowerInvariant(), "123", "Employee"));
                if (!SalaryHistories.Any(x => x.EmployeeId == employee.Id))
                {
                    int nextId = SalaryHistories.Count == 0 ? 1 : SalaryHistories.Max(x => x.Id) + 1;
                    SalaryHistories.Add(new SalaryHistory { Id = nextId, EmployeeId = employee.Id, EffectiveFrom = employee.HireDate, BaseSalary = employee.BaseSalary });
                }
                if (!EmploymentPeriods.Any(x => x.EmployeeId == employee.Id))
                {
                    int periodId = EmploymentPeriods.Count == 0 ? 1 : EmploymentPeriods.Max(x => x.Id) + 1;
                    EmploymentPeriods.Add(new EmploymentPeriod { Id = periodId, EmployeeId = employee.Id, StartDate = employee.HireDate, EndDate = employee.TerminationDate });
                }
            }
            foreach (var salary in Salaries.Where(x => x.Status == "Đã thanh toán" && !x.PaidAt.HasValue))
            {
                salary.PaidAt = salary.PeriodStart.AddMonths(1).AddDays(-1).AddHours(9);
                salary.PaidBy = "admin";
                salary.PaymentMethod = "Chuyển khoản";
                salary.TransactionReference = "PAY-" + salary.PeriodStart.ToString("yyyyMM") + "-" + salary.EmployeeCode;
            }
            var shift = WorkShifts.First();
            foreach (var record in Attendance)
            {
                if (record.ScheduledStart == TimeSpan.Zero) record.ScheduledStart = shift.StartTime;
                if (record.ScheduledEnd == TimeSpan.Zero) record.ScheduledEnd = shift.EndTime;
                if (record.GraceMinutes <= 0) record.GraceMinutes = shift.GraceMinutes;
            }
        }
    }
}
