using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HRMDesktop.Models;
using HRMDesktop.Services;

namespace HRMDesktop.Views.Employee
{
    public partial class MyAttendancePage : Page
    {
        private readonly UserAccount _account;
        private AttendanceRecord _today;

        public MyAttendancePage(UserAccount account)
        {
            InitializeComponent();
            _account = account;
            TodayText.Text = DateTime.Today.ToString("dd/MM/yyyy");
            for (int offset = 0; offset < 12; offset++)
            {
                DateTime month = DateTime.Today.AddMonths(-offset);
                HistoryMonthBox.Items.Add(new ComboBoxItem { Content = "Tháng " + month.ToString("MM/yyyy"), Tag = month.ToString("MM/yyyy") });
            }
            HistoryMonthBox.SelectedIndex = 0;
            AdjustmentDatePicker.DisplayDateEnd = DateTime.Today;
            RefreshData();
        }

        private void RefreshData()
        {
            _today = MockDataService.Attendance.FirstOrDefault(x => x.EmployeeId == _account.EmployeeId && x.WorkDate.Date == DateTime.Today);
            CheckInText.Text = _today == null ? "--" : _today.CheckIn;
            CheckOutText.Text = _today == null ? "--" : _today.CheckOut;
            StatusText.Text = _today == null ? "Chưa chấm công" : _today.Status;
            ResultText.Text = _today == null ? "--" : _today.AttendanceResult;
            DurationText.Text = _today == null ? "--" : _today.WorkDurationDisplay;

            bool isActive = IsCurrentEmployeeActive();
            bool isOnLeave = IsOnApprovedLeave(DateTime.Today);
            CheckInButton.IsEnabled = isActive && !isOnLeave && (_today == null || _today.CheckIn == "--");
            CheckOutButton.IsEnabled = isActive && _today != null && _today.CheckIn != "--" && _today.CheckOut == "--";
            if (isOnLeave && _today == null)
            {
                StatusText.Text = "Nghỉ phép";
                ResultText.Text = "Có phép";
            }

            ApplyHistoryFilter();
            AdjustmentGrid.ItemsSource = MockDataService.AttendanceAdjustments
                .Where(x => x.EmployeeId == _account.EmployeeId)
                .OrderByDescending(x => x.SubmittedAt)
                .ToList();
        }

        private void HistoryMonth_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (HistoryGrid != null) ApplyHistoryFilter();
        }

        private void ApplyHistoryFilter()
        {
            var selected = HistoryMonthBox.SelectedItem as ComboBoxItem;
            string month = selected == null ? DateTime.Today.ToString("MM/yyyy") : Convert.ToString(selected.Tag);
            HistoryGrid.ItemsSource = MockDataService.Attendance
                .Where(x => x.EmployeeId == _account.EmployeeId && x.WorkDate.ToString("MM/yyyy") == month)
                .OrderByDescending(x => x.WorkDate)
                .ToList();
        }

        private void CheckIn_Click(object sender, RoutedEventArgs e)
        {
            var employee = MockDataService.GetEmployee(_account.EmployeeId);
            if (employee == null || employee.Status != "Đang làm việc")
            {
                MessageBox.Show("Tài khoản hiện không thuộc nhân viên đang làm việc.", "Không thể chấm công", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (IsOnApprovedLeave(DateTime.Today))
            {
                MessageBox.Show("Bạn đang có đơn nghỉ phép được duyệt trong hôm nay.", "Không thể chấm công", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (_today != null && _today.CheckIn != "--") return;
            if (MessageBox.Show("Xác nhận chấm công vào lúc " + DateTime.Now.ToString("HH:mm") + "?", "Chấm công vào", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            if (_today == null)
            {
                int nextId = MockDataService.Attendance.Count == 0 ? 1 : MockDataService.Attendance.Max(x => x.Id) + 1;
                _today = new AttendanceRecord { Id = nextId, EmployeeId = employee.Id, EmployeeName = employee.FullName, EmployeeCode = employee.Code, Department = employee.Department, WorkDate = DateTime.Today, CheckIn = DateTime.Now.ToString("HH:mm"), CheckOut = "--", Status = "Đang làm việc" };
                MockDataService.Attendance.Add(_today);
            }
            else
            {
                _today.CheckIn = DateTime.Now.ToString("HH:mm");
                _today.Status = "Đang làm việc";
            }
            RefreshData();
        }

        private void CheckOut_Click(object sender, RoutedEventArgs e)
        {
            if (_today == null || _today.CheckIn == "--" || _today.CheckOut != "--") return;
            if (MessageBox.Show("Xác nhận chấm công ra lúc " + DateTime.Now.ToString("HH:mm") + "? Sau khi xác nhận bạn không thể tự sửa giờ.", "Chấm công ra", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            _today.CheckOut = DateTime.Now.ToString("HH:mm");
            _today.Status = "Đã kết thúc";
            RefreshData();
        }

        private void OpenAdjustment_Click(object sender, RoutedEventArgs e)
        {
            RequestedCheckInBox.Clear();
            RequestedCheckOutBox.Clear();
            AdjustmentReasonBox.Clear();
            AdjustmentErrorText.Visibility = Visibility.Collapsed;
            AdjustmentDatePicker.SelectedDate = DateTime.Today.AddDays(-1);
            AdjustmentOverlay.Visibility = Visibility.Visible;
        }

        private void AdjustmentDate_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!AdjustmentDatePicker.SelectedDate.HasValue || RequestedCheckInBox == null) return;
            var record = MockDataService.Attendance.FirstOrDefault(x => x.EmployeeId == _account.EmployeeId && x.WorkDate.Date == AdjustmentDatePicker.SelectedDate.Value.Date);
            RequestedCheckInBox.Text = record == null || record.CheckIn == "--" ? string.Empty : record.CheckIn;
            RequestedCheckOutBox.Text = record == null || record.CheckOut == "--" ? string.Empty : record.CheckOut;
        }

        private void CloseAdjustment_Click(object sender, RoutedEventArgs e)
        {
            AdjustmentOverlay.Visibility = Visibility.Collapsed;
        }

        private void SubmitAdjustment_Click(object sender, RoutedEventArgs e)
        {
            if (!IsCurrentEmployeeActive()) { ShowAdjustmentError("Tài khoản hiện không thuộc nhân viên đang làm việc."); return; }
            if (!AdjustmentDatePicker.SelectedDate.HasValue) { ShowAdjustmentError("Hãy chọn ngày cần điều chỉnh."); return; }
            DateTime workDate = AdjustmentDatePicker.SelectedDate.Value.Date;
            if (workDate > DateTime.Today) { ShowAdjustmentError("Không thể điều chỉnh ngày trong tương lai."); return; }

            TimeSpan checkIn;
            TimeSpan checkOut = TimeSpan.Zero;
            string checkInText = RequestedCheckInBox.Text.Trim();
            string checkOutText = RequestedCheckOutBox.Text.Trim();
            if (!TryParseTime(checkInText, out checkIn)) { ShowAdjustmentError("Giờ vào phải đúng định dạng HH:mm, ví dụ 08:15."); return; }
            if (!string.IsNullOrWhiteSpace(checkOutText) && !TryParseTime(checkOutText, out checkOut)) { ShowAdjustmentError("Giờ ra phải đúng định dạng HH:mm, ví dụ 17:30."); return; }
            if (!string.IsNullOrWhiteSpace(checkOutText) && checkOut < checkIn) { ShowAdjustmentError("Giờ ra không được sớm hơn giờ vào."); return; }
            if (workDate == DateTime.Today && checkIn > DateTime.Now.TimeOfDay) { ShowAdjustmentError("Giờ vào đề xuất không được nằm trong tương lai."); return; }
            if (AdjustmentReasonBox.Text.Trim().Length < 5) { ShowAdjustmentError("Lý do điều chỉnh cần có ít nhất 5 ký tự."); return; }
            if (MockDataService.AttendanceAdjustments.Any(x => x.EmployeeId == _account.EmployeeId && x.WorkDate.Date == workDate && x.Status == "Chờ duyệt")) { ShowAdjustmentError("Bạn đã có một yêu cầu đang chờ duyệt cho ngày này."); return; }

            var employee = MockDataService.GetEmployee(_account.EmployeeId);
            int nextId = MockDataService.AttendanceAdjustments.Count == 0 ? 1 : MockDataService.AttendanceAdjustments.Max(x => x.Id) + 1;
            MockDataService.AttendanceAdjustments.Add(new AttendanceAdjustmentRequest
            {
                Id = nextId,
                EmployeeId = employee.Id,
                EmployeeName = employee.FullName,
                EmployeeCode = employee.Code,
                Department = employee.Department,
                WorkDate = workDate,
                RequestedCheckIn = checkInText,
                RequestedCheckOut = checkOutText,
                Reason = AdjustmentReasonBox.Text.Trim(),
                SubmittedAt = DateTime.Now,
                Status = "Chờ duyệt"
            });
            AdjustmentOverlay.Visibility = Visibility.Collapsed;
            RefreshData();
            MessageBox.Show("Yêu cầu đã được gửi đến Admin để kiểm tra.", "Gửi yêu cầu thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool IsCurrentEmployeeActive()
        {
            var employee = MockDataService.GetEmployee(_account.EmployeeId);
            return employee != null && employee.Status == "Đang làm việc";
        }

        private bool IsOnApprovedLeave(DateTime date)
        {
            return MockDataService.LeaveRequests.Any(x => x.EmployeeId == _account.EmployeeId && x.Status == "Đã duyệt" && date.Date >= x.FromDate.Date && date.Date <= x.ToDate.Date);
        }

        private void ShowAdjustmentError(string message)
        {
            AdjustmentErrorText.Text = message;
            AdjustmentErrorText.Visibility = Visibility.Visible;
        }

        private static bool TryParseTime(string value, out TimeSpan time)
        {
            DateTime parsed;
            bool valid = DateTime.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed);
            time = valid ? parsed.TimeOfDay : TimeSpan.Zero;
            return valid;
        }
    }
}
