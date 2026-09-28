using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HRMDesktop.Models;
using HRMDesktop.Services;

namespace HRMDesktop.Views.Admin
{
    public partial class LeaveApprovalPage : Page
    {
        public LeaveApprovalPage() { InitializeComponent(); ApplyFilter(); }
        private void Filter_Changed(object sender, EventArgs e) { if (LeaveGrid != null) ApplyFilter(); }
        private void ApplyFilter()
        {
            string keyword = SearchBox == null ? "" : SearchBox.Text.Trim().ToLower();
            string status = "Tất cả trạng thái";
            if (StatusFilter != null && StatusFilter.SelectedItem is ComboBoxItem) status = Convert.ToString(((ComboBoxItem)StatusFilter.SelectedItem).Content);
            LeaveGrid.ItemsSource = HrmDataService.LeaveRequests.Where(x =>
                (string.IsNullOrEmpty(keyword) || (x.EmployeeName ?? string.Empty).ToLower().Contains(keyword) || (x.EmployeeCode ?? string.Empty).ToLower().Contains(keyword) || (x.Reason ?? string.Empty).ToLower().Contains(keyword) || (x.LeaveType ?? string.Empty).ToLower().Contains(keyword)) &&
                (status == "Tất cả trạng thái" || x.Status == status))
                .OrderBy(x => x.Status == "Chờ duyệt" ? 0 : 1)
                .ThenByDescending(x => x.SubmittedAt)
                .ToList();
        }
        private void Approve_Click(object sender, RoutedEventArgs e) { UpdateStatus((sender as Button).Tag as LeaveRequest, "Đã duyệt"); }
        private void Reject_Click(object sender, RoutedEventArgs e) { UpdateStatus((sender as Button).Tag as LeaveRequest, "Từ chối"); }
        private void ViewLeaveDetails_Click(object sender, RoutedEventArgs e)
        {
            var request = (sender as Button).Tag as LeaveRequest;
            if (request == null) return;
            LeaveDetailEmployeeText.Text = request.EmployeeName + "  •  " + request.DisplayCode;
            LeaveDetailTypeText.Text = request.LeaveType;
            LeaveDetailStatusText.Text = request.Status;
            LeaveDetailDatesText.Text = request.DateRange;
            LeaveDetailDaysText.Text = request.TotalDays + " ngày";
            LeaveDetailReasonText.Text = request.Reason;
            LeaveDetailSubmittedText.Text = request.SubmittedAtDisplay;
            LeaveDetailReviewedText.Text = request.ReviewedAt.HasValue ? (request.ReviewedBy ?? "Admin") + " • " + request.ReviewedAt.Value.ToString("dd/MM/yyyy HH:mm") : (request.CancelledAt.HasValue ? "Đã hủy lúc " + request.CancelledAt.Value.ToString("dd/MM/yyyy HH:mm") : "Chưa xử lý");
            LeaveDetailOverlay.Visibility = Visibility.Visible;
        }
        private void CloseLeaveDetails_Click(object sender, RoutedEventArgs e) { LeaveDetailOverlay.Visibility = Visibility.Collapsed; }
        private void UpdateStatus(LeaveRequest request, string status)
        {
            if (request == null || !request.CanReview) return;
            var employee = HrmDataService.GetEmployee(request.EmployeeId);
            if (employee == null || employee.Status != "Đang làm việc")
            {
                MessageBox.Show("Không thể xử lý vì nhân viên không còn ở trạng thái đang làm việc.", "Duyệt nghỉ phép", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (status == "Đã duyệt" && HrmDataService.LeaveRequests.Any(x => x.Id != request.Id && x.EmployeeId == request.EmployeeId && x.Status == "Đã duyệt" && request.FromDate.Date <= x.ToDate.Date && request.ToDate.Date >= x.FromDate.Date))
            {
                MessageBox.Show("Nhân viên đã có một đơn được duyệt trùng khoảng thời gian này.", "Đơn nghỉ phép bị trùng", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (status == "Đã duyệt" && HrmDataService.Attendance.Any(x => x.EmployeeId == request.EmployeeId && x.CheckInAt.HasValue && x.WorkDate.Date >= request.FromDate.Date && x.WorkDate.Date <= request.ToDate.Date))
            {
                MessageBox.Show("Không thể duyệt vì khoảng nghỉ đã có ngày phát sinh chấm công.", "Dữ liệu bị xung đột", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int requestedDays = HrmBusinessService.CountWorkingDays(request.FromDate, request.ToDate);
            if (status == "Đã duyệt" && request.LeaveType == "Nghỉ phép năm" && request.FromDate.Year != request.ToDate.Year)
            {
                MessageBox.Show("Đơn nghỉ phép năm kéo dài qua hai năm nên không thể tính đúng số dư. Hãy yêu cầu nhân viên tách đơn.", "Thời gian chưa hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (status == "Đã duyệt" && request.LeaveType == "Nghỉ phép năm" && requestedDays > HrmBusinessService.GetAnnualLeaveRemaining(employee, request.FromDate.Year))
            {
                MessageBox.Show("Nhân viên không còn đủ số ngày phép năm cho yêu cầu này.", "Không đủ phép năm", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string action = status == "Đã duyệt" ? "duyệt" : "từ chối";
            if (MessageBox.Show("Xác nhận " + action + " đơn nghỉ của " + request.EmployeeName + "?", "Xác nhận xử lý", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            request.Status = status;
            request.ReviewedAt = SystemTimeService.Now;
            request.ReviewedBy = "admin";
            HrmDataService.AddAudit("admin", action + " đơn nghỉ phép", request.EmployeeCode + " - " + request.DateRange);
            HrmDataService.SaveChanges();
            ApplyFilter();
            MessageBox.Show("Đơn của " + request.EmployeeName + " đã được cập nhật: " + status + ".", "Duyệt nghỉ phép", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
