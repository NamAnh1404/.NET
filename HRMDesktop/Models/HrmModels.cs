using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Xml.Serialization;

namespace HRMDesktop.Models
{
    public class ObservableModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void Notify([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class UserAccount
    {
        public UserAccount()
        {
            AttendanceNotificationEnabled = true;
            LeaveNotificationEnabled = true;
            SalaryNotificationEnabled = true;
        }

        public int EmployeeId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string Department { get; set; }
        public string Email { get; set; }
        public bool AttendanceNotificationEnabled { get; set; }
        public bool LeaveNotificationEnabled { get; set; }
        public bool SalaryNotificationEnabled { get; set; }
        public bool IsAdmin { get { return Role == "Admin"; } }
        public string RoleDisplay { get { return IsAdmin ? "Quản trị hệ thống" : "Nhân viên"; } }
        public string Initial { get { return string.IsNullOrWhiteSpace(FullName) ? "H" : FullName.Substring(0, 1).ToUpper(); } }
    }

    public class Employee : ObservableModel
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime HireDate { get; set; }
        public DateTime? TerminationDate { get; set; }
        public int AnnualLeaveAllowance { get; set; }
        public decimal BaseSalary { get; set; }
        public string Status { get; set; }
        public string Initial { get { return string.IsNullOrWhiteSpace(FullName) ? "N" : FullName.Substring(0, 1).ToUpper(); } }
        public string DisplayName { get { return FullName; } }
        public string DisplayCode { get { return Code; } }
        public string SalaryDisplay { get { return BaseSalary.ToString("N0") + " đ"; } }
        public string HireDateDisplay { get { return HireDate.ToString("dd/MM/yyyy"); } }
    }

    public class AttendanceRecord : ObservableModel
    {
        private DateTime? _checkInAt;
        private DateTime? _checkOutAt;
        private string _status;
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeCode { get; set; }
        public string Department { get; set; }
        public DateTime WorkDate { get; set; }
        public DateTime? CheckInAt { get { return _checkInAt; } set { _checkInAt = value; Notify(); Notify("CheckIn"); Notify("AttendanceResult"); Notify("WorkDurationDisplay"); } }
        public DateTime? CheckOutAt { get { return _checkOutAt; } set { _checkOutAt = value; Notify(); Notify("CheckOut"); Notify("AttendanceResult"); Notify("WorkDurationDisplay"); } }
        public TimeSpan ScheduledStart { get; set; }
        public TimeSpan ScheduledEnd { get; set; }
        public int GraceMinutes { get; set; }
        public bool IsOvernightShift { get; set; }
        [XmlIgnore]
        public string CheckIn
        {
            get { return CheckInAt.HasValue ? CheckInAt.Value.ToString("HH:mm") : "--"; }
            set { CheckInAt = ParseWorkTime(value, false); }
        }
        [XmlIgnore]
        public string CheckOut
        {
            get { return CheckOutAt.HasValue ? CheckOutAt.Value.ToString("HH:mm") : "--"; }
            set { CheckOutAt = ParseWorkTime(value, true); }
        }
        public string Status { get { return _status; } set { _status = value; Notify(); Notify("AttendanceResult"); } }
        public string Initial { get { return string.IsNullOrWhiteSpace(EmployeeName) ? "N" : EmployeeName.Substring(0, 1).ToUpper(); } }
        public string DisplayName { get { return EmployeeName; } }
        public string DisplayCode { get { return EmployeeCode; } }
        public string WorkDateDisplay { get { return WorkDate.ToString("dd/MM/yyyy"); } }
        public string WorkDurationDisplay
        {
            get
            {
                if (!CheckInAt.HasValue || !CheckOutAt.HasValue || CheckOutAt.Value < CheckInAt.Value) return "--";
                TimeSpan duration = CheckOutAt.Value - CheckInAt.Value;
                return ((int)duration.TotalHours).ToString("00") + " giờ " + duration.Minutes.ToString("00") + " phút";
            }
        }
        public string AttendanceResult
        {
            get
            {
                if (Status == "Nghỉ phép") return "Có phép";
                if (Status == "Vắng mặt") return "Vắng mặt";
                if (!CheckInAt.HasValue) return "--";
                TimeSpan start = ScheduledStart == TimeSpan.Zero ? new TimeSpan(8, 0, 0) : ScheduledStart;
                TimeSpan end = ScheduledEnd == TimeSpan.Zero ? new TimeSpan(17, 30, 0) : ScheduledEnd;
                int grace = GraceMinutes <= 0 ? 15 : GraceMinutes;
                DateTime scheduledStartAt = WorkDate.Date.Add(start);
                DateTime scheduledEndAt = WorkDate.Date.Add(end);
                if (IsOvernightShift || end <= start) scheduledEndAt = scheduledEndAt.AddDays(1);
                bool late = CheckInAt.Value > scheduledStartAt.AddMinutes(grace);
                if (!CheckOutAt.HasValue) return late ? "Đi muộn" : "Đúng giờ";
                bool early = CheckOutAt.Value < scheduledEndAt;
                if (late && early) return "Đi muộn, về sớm";
                if (late) return "Đi muộn";
                if (early) return "Về sớm";
                return "Đủ công";
            }
        }

        private DateTime? ParseWorkTime(string value, bool isCheckOut)
        {
            TimeSpan parsed;
            if (!TryParseTime(value, out parsed)) return null;
            DateTime result = WorkDate.Date.Add(parsed);
            if (isCheckOut && CheckInAt.HasValue && (IsOvernightShift || result < CheckInAt.Value)) result = result.AddDays(1);
            return result;
        }

        private static bool TryParseTime(string value, out TimeSpan time)
        {
            DateTime parsed;
            bool valid = DateTime.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed);
            time = valid ? parsed.TimeOfDay : TimeSpan.Zero;
            return valid;
        }
    }

    public class AttendanceAdjustmentRequest : ObservableModel
    {
        private string _status;
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeCode { get; set; }
        public string Department { get; set; }
        public DateTime WorkDate { get; set; }
        public string RequestedCheckIn { get; set; }
        public string RequestedCheckOut { get; set; }
        public string Reason { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string ReviewedBy { get; set; }
        public string Status { get { return _status; } set { _status = value; Notify(); Notify("CanReview"); } }
        public bool CanReview { get { return Status == "Chờ duyệt"; } }
        public string Initial { get { return string.IsNullOrWhiteSpace(EmployeeName) ? "N" : EmployeeName.Substring(0, 1).ToUpper(); } }
        public string DisplayName { get { return EmployeeName; } }
        public string DisplayCode { get { return EmployeeCode; } }
        public string WorkDateDisplay { get { return WorkDate.ToString("dd/MM/yyyy"); } }
        public string RequestedTimeDisplay { get { return RequestedCheckIn + " - " + (string.IsNullOrWhiteSpace(RequestedCheckOut) ? "--" : RequestedCheckOut); } }
        public string SubmittedAtDisplay { get { return SubmittedAt.ToString("dd/MM/yyyy HH:mm"); } }
    }

    public class LeaveRequest : ObservableModel
    {
        private string _status;
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeCode { get; set; }
        public string LeaveType { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string ReviewedBy { get; set; }
        public string Reason { get; set; }
        public string Status { get { return _status; } set { _status = value; Notify(); Notify("CanReview"); Notify("CanCancel"); } }
        public bool CanReview { get { return Status == "Chờ duyệt"; } }
        public bool CanCancel { get { return Status == "Chờ duyệt"; } }
        public string Initial { get { return string.IsNullOrWhiteSpace(EmployeeName) ? "N" : EmployeeName.Substring(0, 1).ToUpper(); } }
        public string DisplayName { get { return EmployeeName; } }
        public string DisplayCode { get { return string.IsNullOrWhiteSpace(EmployeeCode) ? "NV" + EmployeeId.ToString("000") : EmployeeCode; } }
        public string DateRange { get { return FromDate.ToString("dd/MM/yyyy") + " - " + ToDate.ToString("dd/MM/yyyy"); } }
        public string SubmittedAtDisplay { get { return SubmittedAt == default(DateTime) ? "--" : SubmittedAt.ToString("dd/MM/yyyy HH:mm"); } }
        public int TotalDays
        {
            get { return HRMDesktop.Services.HrmBusinessService.CountWorkingDays(FromDate, ToDate); }
        }
    }

    public class SalaryRecord : ObservableModel
    {
        private string _status;
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeCode { get; set; }
        public DateTime PeriodStart { get; set; }
        [XmlIgnore]
        public string Month
        {
            get { return (PeriodStart == default(DateTime) ? HRMDesktop.Services.SystemTimeService.Today : PeriodStart).ToString("MM/yyyy"); }
            set
            {
                DateTime parsed;
                PeriodStart = DateTime.TryParseExact("01/" + value, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed)
                    ? parsed.Date : HRMDesktop.Services.SystemTimeService.Today.AddDays(1 - HRMDesktop.Services.SystemTimeService.Today.Day);
            }
        }
        public decimal BaseSalary { get; set; }
        public decimal Bonus { get; set; }
        public decimal Deduction { get; set; }
        public DateTime? PaidAt { get; set; }
        public string PaidBy { get; set; }
        public string PaymentMethod { get; set; }
        public string TransactionReference { get; set; }
        public string Status { get { return _status; } set { _status = value; Notify(); } }
        public string Initial { get { return string.IsNullOrWhiteSpace(EmployeeName) ? "N" : EmployeeName.Substring(0, 1).ToUpper(); } }
        public string DisplayName { get { return EmployeeName; } }
        public string DisplayCode { get { return string.IsNullOrWhiteSpace(EmployeeCode) ? "NV" + EmployeeId.ToString("000") : EmployeeCode; } }
        public decimal NetSalary { get { return BaseSalary + Bonus - Deduction; } }
        public string BaseSalaryDisplay { get { return BaseSalary.ToString("N0") + " đ"; } }
        public string BonusDisplay { get { return Bonus.ToString("N0") + " đ"; } }
        public string DeductionDisplay { get { return Deduction.ToString("N0") + " đ"; } }
        public string NetSalaryDisplay { get { return NetSalary.ToString("N0") + " đ"; } }
        public string PaymentDisplay { get { return PaidAt.HasValue ? PaidAt.Value.ToString("dd/MM/yyyy HH:mm") : "--"; } }
    }

    public class SalaryHistory
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public decimal BaseSalary { get; set; }
    }

    public class EmploymentPeriod
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class WorkShift
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int GraceMinutes { get; set; }
        public bool IsOvernight { get; set; }
    }

    public class Holiday
    {
        public DateTime Date { get; set; }
        public string Name { get; set; }
    }

    public class UserCredential
    {
        public int EmployeeId { get; set; }
        public string Username { get; set; }
        public string PasswordSalt { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
        public bool IsLocked { get; set; }
        public bool AttendanceNotificationEnabled { get; set; }
        public bool LeaveNotificationEnabled { get; set; }
        public bool SalaryNotificationEnabled { get; set; }
    }

    public class AuditLog
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Actor { get; set; }
        public string Action { get; set; }
        public string Details { get; set; }
    }

}
