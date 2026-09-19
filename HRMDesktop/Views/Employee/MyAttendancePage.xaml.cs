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
            TodayText.Text = SystemTimeService.Today.ToString("dd/MM/yyyy");
            for (int offset = 0; offset < 12; offset++)
            {
                DateTime month = SystemTimeService.Today.AddMonths(-offset);
                HistoryMonthBox.Items.Add(new ComboBoxItem { Content = "Tháng " + month.ToString("MM/yyyy"), Tag = month.ToString("MM/yyyy") });
            }
            HistoryMonthBox.SelectedIndex = 0;
            AdjustmentDatePicker.DisplayDateEnd = SystemTimeService.Today;
            RefreshData();
        }

        private void RefreshData()
        {
            _today = MockDataService.Attendance.FirstOrDefault(x => x.EmployeeId == _account.EmployeeId && x.WorkDate.Date == SystemTimeService.Today);
            CheckInText.Text = _today == null ? "--" : _today.CheckIn;
            CheckOutText.Text = _today == null ? "--" : _today.CheckOut;
            StatusText.Text = _today == null ? "Chưa chấm công" : _today.Status;
            ResultText.Text = _today == null ? "--" : _today.AttendanceResult;
            DurationText.Text = _today == null ? "--" : _today.WorkDurationDisplay;

            bool isActive = IsCurrentEmployeeActive();
            bool isOnLeave = IsOnApprovedLeave(SystemTimeService.Today);
            bool isWorkingDay = HrmBusinessService.IsWorkingDay(SystemTimeService.Today);
            CheckInButton.IsEnabled = isActive && isWorkingDay && !isOnLeave && (_today == null || !_today.CheckInAt.HasValue);
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
            string month = selected == null ? SystemTimeService.Today.ToString("MM/yyyy") : Convert.ToString(selected.Tag);
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
            if (IsOnApprovedLeave(SystemTimeService.Today))
            {
                MessageBox.Show("Bạn đang có đơn nghỉ phép được duyệt trong hôm nay.", "Không thể chấm công", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!HrmBusinessService.IsWorkingDay(SystemTimeService.Today))
            {
                MessageBox.Show("Hôm nay là ngày nghỉ hoặc ngày lễ, không thuộc ca hành chính.", "Không thể chấm công", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (_today != null && _today.CheckIn != "--") return;
            DateTime now = SystemTimeService.Now;
            if (MessageBox.Show("Xác nhận chấm công vào lúc " + now.ToString("HH:mm") + "?", "Chấm công vào", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            if (_today == null)
            {
                int nextId = MockDataService.Attendance.Count == 0 ? 1 : MockDataService.Attendance.Max(x => x.Id) + 1;
                _today = HrmBusinessService.CreateAttendanceRecord(nextId, employee, SystemTimeService.Today, now, null);
                MockDataService.Attendance.Add(_today);
            }
            else
            {
                _today.CheckInAt = now;
                _today.Status = "Đang làm việc";
            }
            MockDataService.AddAudit(_account.Username, "Chấm công vào", now.ToString("dd/MM/yyyy HH:mm:ss"));
            MockDataService.SaveChanges();
            RefreshData();
        }

        private void CheckOut_Click(object sender, RoutedEventArgs e)
        {
            if (_today == null || _today.CheckIn == "--" || _today.CheckOut != "--") return;
            DateTime now = SystemTimeService.Now;
            if (MessageBox.Show("Xác nhận chấm công ra lúc " + now.ToString("HH:mm") + "? Sau khi xác nhận bạn không thể tự sửa giờ.", "Chấm công ra", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            if (_today.CheckInAt.HasValue && now < _today.CheckInAt.Value) { MessageBox.Show("Giờ ra không hợp lệ. Hãy gửi yêu cầu điều chỉnh.", "Không thể chấm công", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            _today.CheckOutAt = now;
            _today.Status = "Đã kết thúc";
            MockDataService.AddAudit(_account.Username, "Chấm công ra", now.ToString("dd/MM/yyyy HH:mm:ss"));
            MockDataService.SaveChanges();
            RefreshData();
        }

        private void OpenAdjustment_Click(object sender, RoutedEventArgs e)
        {
            RequestedCheckInBox.Clear();
            RequestedCheckOutBox.Clear();
            AdjustmentReasonBox.Clear();
            AdjustmentErrorText.Visibility = Visibility.Collapsed;
            AdjustmentDatePicker.SelectedDate = SystemTimeService.Today.AddDays(-1);
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
            if (workDate > SystemTimeService.Today) { ShowAdjustmentError("Không thể điều chỉnh ngày trong tương lai."); return; }
            if (!HrmBusinessService.IsWorkingDay(workDate)) { ShowAdjustmentError("Ngày đã chọn không phải ngày làm việc của ca hành chính."); return; }
            var employee = MockDataService.GetEmployee(_account.EmployeeId);
            if (!HrmBusinessService.IsEmployedOn(employee, workDate)) { ShowAdjustmentError("Ngày đã chọn nằm ngoài thời gian làm việc của nhân viên."); return; }

            string checkInText = RequestedCheckInBox.Text.Trim();
            string checkOutText = RequestedCheckOutBox.Text.Trim();
            DateTime checkInAt;
            DateTime? checkOutAt;
            string timeError;
            if (!HrmBusinessService.TryBuildAttendanceTimes(workDate, checkInText, checkOutText, out checkInAt, out checkOutAt, out timeError)) { ShowAdjustmentError(timeError); return; }
            if (AdjustmentReasonBox.Text.Trim().Length < 5) { ShowAdjustmentError("Lý do điều chỉnh cần có ít nhất 5 ký tự."); return; }
            if (MockDataService.AttendanceAdjustments.Any(x => x.EmployeeId == _account.EmployeeId && x.WorkDate.Date == workDate && x.Status == "Chờ duyệt")) { ShowAdjustmentError("Bạn đã có một yêu cầu đang chờ duyệt cho ngày này."); return; }

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
                SubmittedAt = SystemTimeService.Now,
                Status = "Chờ duyệt"
            });
            MockDataService.AddAudit(_account.Username, "Gửi điều chỉnh chấm công", workDate.ToString("dd/MM/yyyy"));
            MockDataService.SaveChanges();
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
            return HrmBusinessService.HasApprovedLeave(_account.EmployeeId, date);
        }

        private void ShowAdjustmentError(string message)
        {
            AdjustmentErrorText.Text = message;
            AdjustmentErrorText.Visibility = Visibility.Visible;
        }

    }
}
