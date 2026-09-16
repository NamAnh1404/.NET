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
            LeaveGrid.ItemsSource = MockDataService.LeaveRequests.Where(x =>
                (string.IsNullOrEmpty(keyword) || (x.EmployeeName ?? string.Empty).ToLower().Contains(keyword) || (x.EmployeeCode ?? string.Empty).ToLower().Contains(keyword) || (x.Reason ?? string.Empty).ToLower().Contains(keyword) || (x.LeaveType ?? string.Empty).ToLower().Contains(keyword)) &&
                (status == "Tất cả trạng thái" || x.Status == status))
                .OrderBy(x => x.Status == "Chờ duyệt" ? 0 : 1)
                .ThenByDescending(x => x.SubmittedAt)
                .ToList();
        }
        private void Approve_Click(object sender, RoutedEventArgs e) { UpdateStatus((sender as Button).Tag as LeaveRequest, "Đã duyệt"); }
        private void Reject_Click(object sender, RoutedEventArgs e) { UpdateStatus((sender as Button).Tag as LeaveRequest, "Từ chối"); }
        private void UpdateStatus(LeaveRequest request, string status)
        {
            if (request == null || !request.CanReview) return;
            var employee = MockDataService.GetEmployee(request.EmployeeId);
            if (employee == null || employee.Status != "Đang làm việc")
            {
                MessageBox.Show("Không thể xử lý vì nhân viên không còn ở trạng thái đang làm việc.", "Duyệt nghỉ phép", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (status == "Đã duyệt" && MockDataService.LeaveRequests.Any(x => x.Id != request.Id && x.EmployeeId == request.EmployeeId && x.Status == "Đã duyệt" && request.FromDate.Date <= x.ToDate.Date && request.ToDate.Date >= x.FromDate.Date))
            {
                MessageBox.Show("Nhân viên đã có một đơn được duyệt trùng khoảng thời gian này.", "Đơn nghỉ phép bị trùng", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string action = status == "Đã duyệt" ? "duyệt" : "từ chối";
            if (MessageBox.Show("Xác nhận " + action + " đơn nghỉ của " + request.EmployeeName + "?", "Xác nhận xử lý", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            request.Status = status;
            request.ReviewedAt = DateTime.Now;
            ApplyFilter();
            MessageBox.Show("Đơn của " + request.EmployeeName + " đã được cập nhật: " + status + ".", "Duyệt nghỉ phép", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
