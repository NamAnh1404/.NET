using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Globalization;

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
        public decimal BaseSalary { get; set; }
        public string Status { get; set; }
        public string Initial { get { return string.IsNullOrWhiteSpace(FullName) ? "N" : FullName.Substring(0, 1).ToUpper(); } }
        public string DisplayName { get { return FullName; } }
        public string DisplayCode { get { return Code; } }
        public string SalaryDisplay { get { return BaseSalary.ToString("N0") + " đ"; } }
    }

    public class AttendanceRecord : ObservableModel
    {
        private string _checkIn;
        private string _checkOut;
        private string _status;
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeCode { get; set; }
        public string Department { get; set; }
        public DateTime WorkDate { get; set; }
        public string CheckIn { get { return _checkIn; } set { _checkIn = value; Notify(); Notify("AttendanceResult"); Notify("WorkDurationDisplay"); } }
        public string CheckOut { get { return _checkOut; } set { _checkOut = value; Notify(); Notify("AttendanceResult"); Notify("WorkDurationDisplay"); } }
        public string Status { get { return _status; } set { _status = value; Notify(); Notify("AttendanceResult"); } }
        public string Initial { get { return string.IsNullOrWhiteSpace(EmployeeName) ? "N" : EmployeeName.Substring(0, 1).ToUpper(); } }
        public string DisplayName { get { return EmployeeName; } }
        public string DisplayCode { get { return EmployeeCode; } }
        public string WorkDateDisplay { get { return WorkDate.ToString("dd/MM/yyyy"); } }
        public string WorkDurationDisplay
        {
            get
            {
                TimeSpan checkIn;
                TimeSpan checkOut;
                if (!TryParseTime(CheckIn, out checkIn) || !TryParseTime(CheckOut, out checkOut) || checkOut < checkIn) return "--";
                TimeSpan duration = checkOut - checkIn;
                return ((int)duration.TotalHours).ToString("00") + " giờ " + duration.Minutes.ToString("00") + " phút";
            }
        }
        public string AttendanceResult
        {
            get
            {
                if (Status == "Nghỉ phép") return "Có phép";
                if (Status == "Vắng mặt") return "Vắng mặt";
                TimeSpan checkIn;
                if (!TryParseTime(CheckIn, out checkIn)) return "--";
                bool late = checkIn > new TimeSpan(8, 15, 0);
                TimeSpan checkOut;
                if (!TryParseTime(CheckOut, out checkOut)) return late ? "Đi muộn" : "Đúng giờ";
                bool early = checkOut < new TimeSpan(17, 0, 0);
                if (late && early) return "Đi muộn, về sớm";
                if (late) return "Đi muộn";
                if (early) return "Về sớm";
                return "Đủ công";
            }
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
            get
            {
                int total = 0;
                for (DateTime date = FromDate.Date; date <= ToDate.Date; date = date.AddDays(1))
                {
                    if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday) total++;
                }
                return total;
            }
        }
    }

    public class SalaryRecord : ObservableModel
    {
        private string _status;
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeCode { get; set; }
        public string Month { get; set; }
        public decimal BaseSalary { get; set; }
        public decimal Bonus { get; set; }
        public decimal Deduction { get; set; }
        public string Status { get { return _status; } set { _status = value; Notify(); } }
        public string Initial { get { return string.IsNullOrWhiteSpace(EmployeeName) ? "N" : EmployeeName.Substring(0, 1).ToUpper(); } }
        public string DisplayName { get { return EmployeeName; } }
        public string DisplayCode { get { return string.IsNullOrWhiteSpace(EmployeeCode) ? "NV" + EmployeeId.ToString("000") : EmployeeCode; } }
        public decimal NetSalary { get { return BaseSalary + Bonus - Deduction; } }
        public string BaseSalaryDisplay { get { return BaseSalary.ToString("N0") + " đ"; } }
        public string BonusDisplay { get { return Bonus.ToString("N0") + " đ"; } }
        public string DeductionDisplay { get { return Deduction.ToString("N0") + " đ"; } }
        public string NetSalaryDisplay { get { return NetSalary.ToString("N0") + " đ"; } }
    }

}
