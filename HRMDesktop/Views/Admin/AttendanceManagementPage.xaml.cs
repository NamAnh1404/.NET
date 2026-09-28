using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HRMDesktop.Models;
using HRMDesktop.Services;

namespace HRMDesktop.Views.Admin
{
    public partial class AttendanceManagementPage : Page
    {
        public AttendanceManagementPage()
        {
            InitializeComponent();
            WorkDatePicker.SelectedDate = SystemTimeService.Today;
            WorkDatePicker.DisplayDateEnd = SystemTimeService.Today;
            ApplyFilter();
        }

        private void Filter_Changed(object sender, EventArgs e)
        {
            if (AttendanceGrid != null && AdjustmentGrid != null) ApplyFilter();
        }

        private void ApplyFilter()
        {
            DateTime date = WorkDatePicker == null || !WorkDatePicker.SelectedDate.HasValue ? SystemTimeService.Today : WorkDatePicker.SelectedDate.Value.Date;
            string keyword = SearchBox == null ? string.Empty : SearchBox.Text.Trim().ToLower();
            string department = "Tất cả phòng ban";
            if (DepartmentFilter != null && DepartmentFilter.SelectedItem is ComboBoxItem) department = Convert.ToString(((ComboBoxItem)DepartmentFilter.SelectedItem).Content);

            var records = HrmDataService.Attendance.Where(x => x.WorkDate.Date == date).ToList();
            var rows = HrmDataService.Employees
                .Where(x => HrmBusinessService.IsEmployedOn(x, date) && (HrmBusinessService.IsWorkingDay(date) || records.Any(record => record.EmployeeId == x.Id)))
                .Select(employee =>
                {
                    var record = records.FirstOrDefault(x => x.EmployeeId == employee.Id);
                    if (record != null) return record;
                    bool onLeave = HrmBusinessService.HasApprovedLeave(employee.Id, date);
                    return new AttendanceRecord
                    {
                        EmployeeId = employee.Id,
                        EmployeeName = employee.FullName,
                        EmployeeCode = employee.Code,
                        Department = employee.Department,
                        WorkDate = date,
                        CheckIn = "--",
                        CheckOut = "--",
                        Status = onLeave ? "Nghỉ phép" : (date < SystemTimeService.Today ? "Vắng mặt" : "Chưa chấm công")
                    };
                })
                .Where(x => MatchesFilter(x.EmployeeName, x.EmployeeCode, x.Department, keyword, department))
                .OrderBy(x => x.EmployeeName)
                .ToList();

            AttendanceGrid.ItemsSource = rows;
            CheckedText.Text = rows.Count(x => x.CheckIn != "--").ToString();
            WorkingText.Text = rows.Count(x => x.Status == "Đang làm việc").ToString();
            MissingText.Text = rows.Count(x => x.Status == "Vắng mặt" || x.Status == "Chưa chấm công").ToString();

            var adjustments = HrmDataService.AttendanceAdjustments
                .Where(x => MatchesFilter(x.EmployeeName, x.EmployeeCode, x.Department, keyword, department))
                .OrderBy(x => x.Status == "Chờ duyệt" ? 0 : 1)
                .ThenByDescending(x => x.SubmittedAt)
                .ToList();
            AdjustmentGrid.ItemsSource = adjustments;
            PendingAdjustmentText.Text = adjustments.Count(x => x.Status == "Chờ duyệt").ToString();
        }

        private void ApproveAdjustment_Click(object sender, RoutedEventArgs e)
        {
            UpdateAdjustment((sender as Button).Tag as AttendanceAdjustmentRequest, true);
        }

        private void RejectAdjustment_Click(object sender, RoutedEventArgs e)
        {
            UpdateAdjustment((sender as Button).Tag as AttendanceAdjustmentRequest, false);
        }

        private void ViewAdjustmentDetails_Click(object sender, RoutedEventArgs e)
        {
            var request = (sender as Button).Tag as AttendanceAdjustmentRequest;
            if (request == null) return;
            var original = HrmDataService.Attendance.FirstOrDefault(x => x.EmployeeId == request.EmployeeId && x.WorkDate.Date == request.WorkDate.Date);
            AdjustmentDetailEmployeeText.Text = request.EmployeeName + "  •  " + request.DisplayCode + "  •  " + request.Department;
            AdjustmentDetailDateText.Text = request.WorkDateDisplay;
            AdjustmentDetailStatusText.Text = request.Status;
            AdjustmentDetailOriginalText.Text = original == null ? "Chưa có bản ghi" : original.CheckIn + " - " + original.CheckOut;
            AdjustmentDetailRequestedText.Text = request.RequestedTimeDisplay;
            AdjustmentDetailReasonText.Text = request.Reason;
            AdjustmentDetailSubmittedText.Text = request.SubmittedAtDisplay;
            AdjustmentDetailReviewedText.Text = request.ReviewedAt.HasValue ? (request.ReviewedBy ?? "Admin") + " • " + request.ReviewedAt.Value.ToString("dd/MM/yyyy HH:mm") : "Chưa xử lý";
            AdjustmentDetailOverlay.Visibility = Visibility.Visible;
        }

        private void CloseAdjustmentDetails_Click(object sender, RoutedEventArgs e)
        {
            AdjustmentDetailOverlay.Visibility = Visibility.Collapsed;
        }

        private void UpdateAdjustment(AttendanceAdjustmentRequest request, bool approve)
        {
            if (request == null || !request.CanReview) return;
            var employee = HrmDataService.GetEmployee(request.EmployeeId);
            if (employee == null)
            {
                MessageBox.Show("Không tìm thấy hồ sơ nhân viên của yêu cầu này.", "Điều chỉnh chấm công", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (approve && !HrmBusinessService.IsEmployedOn(employee, request.WorkDate))
            {
                MessageBox.Show("Ngày điều chỉnh nằm ngoài thời gian làm việc của nhân viên.", "Điều chỉnh chấm công", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (approve && !HrmBusinessService.IsWorkingDay(request.WorkDate))
            {
                MessageBox.Show("Ngày điều chỉnh không thuộc ngày làm việc của ca hành chính.", "Điều chỉnh chấm công", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (approve && HrmDataService.LeaveRequests.Any(x => x.EmployeeId == request.EmployeeId && x.Status == "Đã duyệt" && request.WorkDate.Date >= x.FromDate.Date && request.WorkDate.Date <= x.ToDate.Date))
            {
                MessageBox.Show("Không thể duyệt vì nhân viên đã được duyệt nghỉ phép trong ngày này.", "Điều chỉnh chấm công", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string action = approve ? "duyệt" : "từ chối";
            if (MessageBox.Show("Xác nhận " + action + " yêu cầu điều chỉnh của " + request.EmployeeName + " ngày " + request.WorkDateDisplay + "?", "Xác nhận xử lý", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            if (approve)
            {
                DateTime checkInAt;
                DateTime? checkOutAt;
                string validationError;
                if (!HrmBusinessService.TryBuildAttendanceTimes(request.WorkDate, request.RequestedCheckIn, request.RequestedCheckOut, out checkInAt, out checkOutAt, out validationError))
                {
                    MessageBox.Show(validationError, "Dữ liệu điều chỉnh không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var record = HrmDataService.Attendance.FirstOrDefault(x => x.EmployeeId == request.EmployeeId && x.WorkDate.Date == request.WorkDate.Date);
                if (record == null)
                {
                    int nextId = HrmDataService.Attendance.Count == 0 ? 1 : HrmDataService.Attendance.Max(x => x.Id) + 1;
                    record = HrmBusinessService.CreateAttendanceRecord(nextId, employee, request.WorkDate, checkInAt, checkOutAt);
                    HrmDataService.Attendance.Add(record);
                }
                else
                {
                    record.CheckInAt = checkInAt;
                    record.CheckOutAt = checkOutAt;
                }
                record.Status = !record.CheckOutAt.HasValue ? (record.WorkDate.Date == SystemTimeService.Today ? "Đang làm việc" : "Thiếu giờ ra") : "Đã kết thúc";
                request.Status = "Đã duyệt";
            }
            else
            {
                request.Status = "Từ chối";
            }
            request.ReviewedAt = SystemTimeService.Now;
            request.ReviewedBy = "admin";
            HrmDataService.AddAudit("admin", action + " điều chỉnh chấm công", request.EmployeeCode + " - " + request.WorkDateDisplay);
            HrmDataService.SaveChanges();
            ApplyFilter();
            MessageBox.Show("Yêu cầu đã được cập nhật: " + request.Status + ".", "Điều chỉnh chấm công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static bool MatchesFilter(string name, string code, string employeeDepartment, string keyword, string department)
        {
            bool matchesKeyword = string.IsNullOrEmpty(keyword) || (name ?? string.Empty).ToLower().Contains(keyword) || (code ?? string.Empty).ToLower().Contains(keyword);
            bool matchesDepartment = department == "Tất cả phòng ban" || employeeDepartment == department;
            return matchesKeyword && matchesDepartment;
        }
    }
}
