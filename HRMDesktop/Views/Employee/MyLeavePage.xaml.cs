using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HRMDesktop.Models;
using HRMDesktop.Services;

namespace HRMDesktop.Views.Employee
{
    public partial class MyLeavePage : Page
    {
        private readonly UserAccount _account;
        public MyLeavePage(UserAccount account)
        {
            InitializeComponent(); _account = account;
            FromDatePicker.DisplayDateStart = DateTime.Today;
            ToDatePicker.DisplayDateStart = DateTime.Today;
            FromDatePicker.SelectedDate = NextWorkingDay(DateTime.Today); ToDatePicker.SelectedDate = NextWorkingDay(DateTime.Today); RefreshData();
        }
        private void RefreshData() { LeaveGrid.ItemsSource = MockDataService.LeaveRequests.Where(x => x.EmployeeId == _account.EmployeeId).OrderByDescending(x => x.Id).ToList(); }
        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            if (!FromDatePicker.SelectedDate.HasValue || !ToDatePicker.SelectedDate.HasValue || string.IsNullOrWhiteSpace(ReasonBox.Text))
            { MessageBox.Show("Hãy nhập đầy đủ thời gian và lý do nghỉ phép.", "Dữ liệu chưa hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            var currentEmployee = MockDataService.GetEmployee(_account.EmployeeId);
            if (currentEmployee == null || currentEmployee.Status != "Đang làm việc")
            { MessageBox.Show("Tài khoản hiện không thuộc nhân viên đang làm việc.", "Không thể gửi đơn", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            DateTime fromDate = FromDatePicker.SelectedDate.Value.Date;
            DateTime toDate = ToDatePicker.SelectedDate.Value.Date;
            if (fromDate < DateTime.Today || toDate < fromDate)
            { MessageBox.Show("Ngày nghỉ phải từ hôm nay trở đi và ngày kết thúc không được trước ngày bắt đầu.", "Thời gian chưa hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (CountWorkingDays(fromDate, toDate) == 0)
            { MessageBox.Show("Khoảng nghỉ phải có ít nhất một ngày làm việc từ thứ Hai đến thứ Sáu.", "Thời gian chưa hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (ReasonBox.Text.Trim().Length < 5)
            { MessageBox.Show("Lý do nghỉ phép cần có ít nhất 5 ký tự.", "Dữ liệu chưa hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            bool overlaps = MockDataService.LeaveRequests.Any(x => x.EmployeeId == _account.EmployeeId &&
                (x.Status == "Chờ duyệt" || x.Status == "Đã duyệt") && fromDate <= x.ToDate.Date && toDate >= x.FromDate.Date);
            if (overlaps)
            { MessageBox.Show("Khoảng thời gian này đang trùng với một đơn chờ duyệt hoặc đã duyệt.", "Đơn nghỉ phép bị trùng", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            var selectedType = LeaveTypeBox.SelectedItem as ComboBoxItem;
            int nextId = MockDataService.LeaveRequests.Count == 0 ? 1 : MockDataService.LeaveRequests.Max(x => x.Id) + 1;
            var employee = MockDataService.GetEmployee(_account.EmployeeId);
            MockDataService.LeaveRequests.Add(new LeaveRequest { Id=nextId, EmployeeId=_account.EmployeeId, EmployeeName=_account.FullName, EmployeeCode=employee == null ? string.Empty : employee.Code, LeaveType=selectedType == null ? "Nghỉ phép năm" : Convert.ToString(selectedType.Content), FromDate=fromDate, ToDate=toDate, SubmittedAt=DateTime.Now, Reason=ReasonBox.Text.Trim(), Status="Chờ duyệt" });
            ReasonBox.Clear(); FromDatePicker.SelectedDate = NextWorkingDay(DateTime.Today); ToDatePicker.SelectedDate = NextWorkingDay(DateTime.Today); RefreshData();
            MessageBox.Show("Đơn nghỉ phép đã được gửi và đang chờ Admin duyệt.", "Gửi đơn thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CancelRequest_Click(object sender, RoutedEventArgs e)
        {
            var request = (sender as Button).Tag as LeaveRequest;
            if (request == null || !request.CanCancel) return;
            if (MessageBox.Show("Bạn muốn hủy đơn nghỉ từ " + request.DateRange + "?", "Xác nhận hủy đơn", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            request.Status = "Đã hủy";
            RefreshData();
        }

        private static int CountWorkingDays(DateTime fromDate, DateTime toDate)
        {
            int total = 0;
            for (DateTime date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday) total++;
            return total;
        }

        private static DateTime NextWorkingDay(DateTime date)
        {
            DateTime next = date.AddDays(1);
            while (next.DayOfWeek == DayOfWeek.Saturday || next.DayOfWeek == DayOfWeek.Sunday) next = next.AddDays(1);
            return next;
        }
    }
}
