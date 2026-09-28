using System;
using System.Globalization;
using System.Linq;
using HRMDesktop.Models;

namespace HRMDesktop.Services
{
    public static class HrmBusinessService
    {
        public static WorkShift DefaultShift
        {
            get { return HrmDataService.WorkShifts.First(); }
        }

        public static bool IsWorkingDay(DateTime date)
        {
            date = date.Date;
            return date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday &&
                   !HrmDataService.Holidays.Any(x => x.Date.Date == date);
        }

        public static int CountWorkingDays(DateTime fromDate, DateTime toDate)
        {
            int total = 0;
            for (DateTime date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
                if (IsWorkingDay(date)) total++;
            return total;
        }

        public static bool IsEmployedOn(Employee employee, DateTime date)
        {
            if (employee == null) return false;
            var periods = HrmDataService.EmploymentPeriods.Where(x => x.EmployeeId == employee.Id).ToList();
            if (periods.Count > 0) return periods.Any(x => x.StartDate.Date <= date.Date && (!x.EndDate.HasValue || x.EndDate.Value.Date >= date.Date));
            DateTime hireDate = employee.HireDate == default(DateTime) ? DateTime.MinValue : employee.HireDate.Date;
            return hireDate <= date.Date && (!employee.TerminationDate.HasValue || employee.TerminationDate.Value.Date >= date.Date);
        }

        public static bool IsEmployedDuringMonth(Employee employee, DateTime month)
        {
            DateTime start = new DateTime(month.Year, month.Month, 1);
            DateTime end = start.AddMonths(1).AddDays(-1);
            if (employee == null) return false;
            var periods = HrmDataService.EmploymentPeriods.Where(x => x.EmployeeId == employee.Id).ToList();
            if (periods.Count > 0) return periods.Any(x => x.StartDate.Date <= end && (!x.EndDate.HasValue || x.EndDate.Value.Date >= start));
            return employee.HireDate.Date <= end && (!employee.TerminationDate.HasValue || employee.TerminationDate.Value.Date >= start);
        }

        public static decimal GetBaseSalary(int employeeId, DateTime period)
        {
            var history = HrmDataService.SalaryHistories
                .Where(x => x.EmployeeId == employeeId && x.EffectiveFrom.Date <= period.Date)
                .OrderByDescending(x => x.EffectiveFrom)
                .FirstOrDefault();
            var employee = HrmDataService.GetEmployee(employeeId);
            return history == null ? (employee == null ? 0 : employee.BaseSalary) : history.BaseSalary;
        }

        public static int GetAnnualLeaveUsed(int employeeId, int year, int excludingRequestId = 0)
        {
            return HrmDataService.LeaveRequests
                .Where(x => x.EmployeeId == employeeId && x.Id != excludingRequestId && x.Status == "Đã duyệt" && x.LeaveType == "Nghỉ phép năm" && x.FromDate.Year == year)
                .Sum(x => CountWorkingDays(x.FromDate, x.ToDate));
        }

        public static int GetAnnualLeaveRemaining(Employee employee, int year)
        {
            if (employee == null) return 0;
            return Math.Max(0, employee.AnnualLeaveAllowance - GetAnnualLeaveUsed(employee.Id, year));
        }

        public static int GetAnnualLeaveAvailable(Employee employee, int year, int excludingRequestId = 0)
        {
            if (employee == null) return 0;
            return Math.Max(0, employee.AnnualLeaveAllowance - GetAnnualLeaveCommittedDays(employee.Id, year, excludingRequestId));
        }

        public static int GetAnnualLeaveCommittedDays(int employeeId, int year, int excludingRequestId = 0)
        {
            return HrmDataService.LeaveRequests
                .Where(x => x.EmployeeId == employeeId && x.Id != excludingRequestId &&
                            (x.Status == "Chờ duyệt" || x.Status == "Đã duyệt") &&
                            x.LeaveType == "Nghỉ phép năm" && x.FromDate.Year == year)
                .Sum(x => CountWorkingDays(x.FromDate, x.ToDate));
        }

        public static bool HasApprovedLeave(int employeeId, DateTime date)
        {
            return HrmDataService.LeaveRequests.Any(x => x.EmployeeId == employeeId && x.Status == "Đã duyệt" && date.Date >= x.FromDate.Date && date.Date <= x.ToDate.Date);
        }

        public static bool TryParseTime(string value, out TimeSpan time)
        {
            DateTime parsed;
            bool valid = DateTime.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed);
            time = valid ? parsed.TimeOfDay : TimeSpan.Zero;
            return valid;
        }

        public static bool TryBuildAttendanceTimes(DateTime workDate, string checkInText, string checkOutText, out DateTime checkInAt, out DateTime? checkOutAt, out string error)
        {
            checkInAt = default(DateTime);
            checkOutAt = null;
            error = null;
            TimeSpan checkIn;
            TimeSpan checkOut;
            if (!TryParseTime(checkInText, out checkIn)) { error = "Giờ vào phải đúng định dạng HH:mm."; return false; }
            checkInAt = workDate.Date.Add(checkIn);
            if (!string.IsNullOrWhiteSpace(checkOutText))
            {
                if (!TryParseTime(checkOutText, out checkOut)) { error = "Giờ ra phải đúng định dạng HH:mm."; return false; }
                checkOutAt = workDate.Date.Add(checkOut);
                if (DefaultShift.IsOvernight && checkOutAt.Value <= checkInAt) checkOutAt = checkOutAt.Value.AddDays(1);
                if (!DefaultShift.IsOvernight && checkOutAt.Value < checkInAt) { error = "Giờ ra không được sớm hơn giờ vào."; return false; }
            }
            DateTime now = SystemTimeService.Now;
            if (checkInAt > now || (checkOutAt.HasValue && checkOutAt.Value > now)) { error = "Thời gian đề xuất không được nằm trong tương lai."; return false; }
            return true;
        }

        public static AttendanceRecord CreateAttendanceRecord(int id, Employee employee, DateTime workDate, DateTime checkInAt, DateTime? checkOutAt)
        {
            var shift = DefaultShift;
            return new AttendanceRecord
            {
                Id = id,
                EmployeeId = employee.Id,
                EmployeeName = employee.FullName,
                EmployeeCode = employee.Code,
                Department = employee.Department,
                WorkDate = workDate.Date,
                CheckInAt = checkInAt,
                CheckOutAt = checkOutAt,
                ScheduledStart = shift.StartTime,
                ScheduledEnd = shift.EndTime,
                GraceMinutes = shift.GraceMinutes,
                IsOvernightShift = shift.IsOvernight,
                Status = checkOutAt.HasValue ? "Đã kết thúc" : (workDate.Date == SystemTimeService.Today ? "Đang làm việc" : "Thiếu giờ ra")
            };
        }
    }
}
