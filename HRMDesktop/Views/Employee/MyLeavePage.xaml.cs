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
            FromDatePicker.DisplayDateStart = SystemTimeService.Today;
            ToDatePicker.DisplayDateStart = SystemTimeService.Today;
            FromDatePicker.SelectedDate = NextWorkingDay(SystemTimeService.Today); ToDatePicker.SelectedDate = NextWorkingDay(SystemTimeService.Today); RefreshData();
        }
        private void RefreshData()
        {
            LeaveGrid.ItemsSource = HrmDataService.LeaveRequests.Where(x => x.EmployeeId == _account.EmployeeId).OrderByDescending(x => x.Id).ToList();
            var employee = HrmDataService.GetEmployee(_account.EmployeeId);
            LeaveBalanceText.Text = HrmBusinessService.GetAnnualLeaveAvailable(employee, SystemTimeService.Today.Year) + " ngày";
        }
        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            if (!FromDatePicker.SelectedDate.HasValue || !ToDatePicker.SelectedDate.HasValue || string.IsNullOrWhiteSpace(ReasonBox.Text))
            { MessageBox.Show("Hãy nhập đầy đủ thời gian và lý do nghỉ phép.", "Dữ liệu chưa hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            var currentEmployee = HrmDataService.GetEmployee(_account.EmployeeId);
            if (currentEmployee == null || currentEmployee.Status != "Đang làm việc")
            { MessageBox.Show("Tài khoản hiện không thuộc nhân viên đang làm việc.", "Không thể gửi đơn", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            DateTime fromDate = FromDatePicker.SelectedDate.Value.Date;
            DateTime toDate = ToDatePicker.SelectedDate.Value.Date;
            if (fromDate < SystemTimeService.Today || toDate < fromDate)
            { MessageBox.Show("Ngày nghỉ phải từ hôm nay trở đi và ngày kết thúc không được trước ngày bắt đầu.", "Thời gian chưa hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            int requestedDays = HrmBusinessService.CountWorkingDays(fromDate, toDate);
            if (requestedDays == 0)
            { MessageBox.Show("Khoảng nghỉ không có ngày làm việc hợp lệ hoặc trùng ngày lễ.", "Thời gian chưa hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (ReasonBox.Text.Trim().Length < 5)
            { MessageBox.Show("Lý do nghỉ phép cần có ít nhất 5 ký tự.", "Dữ liệu chưa hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            bool overlaps = HrmDataService.LeaveRequests.Any(x => x.EmployeeId == _account.EmployeeId &&
                (x.Status == "Chờ duyệt" || x.Status == "Đã duyệt") && fromDate <= x.ToDate.Date && toDate >= x.FromDate.Date);
            if (overlaps)
            { MessageBox.Show("Khoảng thời gian này đang trùng với một đơn chờ duyệt hoặc đã duyệt.", "Đơn nghỉ phép bị trùng", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            var selectedType = LeaveTypeBox.SelectedItem as ComboBoxItem;
            string leaveType = selectedType == null ? "Nghỉ phép năm" : Convert.ToString(selectedType.Content);
            if (leaveType == "Nghỉ phép năm" && fromDate.Year != toDate.Year)
            { MessageBox.Show("Đơn nghỉ phép năm không được kéo dài qua hai năm. Hãy tách thành hai đơn để tính đúng số dư từng năm.", "Thời gian chưa hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (leaveType == "Nghỉ phép năm" && requestedDays > HrmBusinessService.GetAnnualLeaveAvailable(currentEmployee, fromDate.Year))
            { MessageBox.Show("Số ngày yêu cầu vượt quá số phép năm còn lại.", "Không đủ phép năm", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            bool alreadyWorked = HrmDataService.Attendance.Any(x => x.EmployeeId == _account.EmployeeId && x.CheckInAt.HasValue && x.WorkDate.Date >= fromDate && x.WorkDate.Date <= toDate);
            if (alreadyWorked)
            { MessageBox.Show("Khoảng nghỉ có ngày đã phát sinh chấm công. Hãy chọn thời gian khác hoặc liên hệ Admin.", "Dữ liệu bị xung đột", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            int nextId = HrmDataService.LeaveRequests.Count == 0 ? 1 : HrmDataService.LeaveRequests.Max(x => x.Id) + 1;
            var employee = HrmDataService.GetEmployee(_account.EmployeeId);
            HrmDataService.LeaveRequests.Add(new LeaveRequest { Id=nextId, EmployeeId=_account.EmployeeId, EmployeeName=_account.FullName, EmployeeCode=employee == null ? string.Empty : employee.Code, LeaveType=leaveType, FromDate=fromDate, ToDate=toDate, SubmittedAt=SystemTimeService.Now, Reason=ReasonBox.Text.Trim(), Status="Chờ duyệt" });
            HrmDataService.AddAudit(_account.Username, "Gửi đơn nghỉ phép", fromDate.ToString("dd/MM/yyyy") + " - " + toDate.ToString("dd/MM/yyyy"));
            if (!HrmDataService.SaveChanges()) { ShowSaveError(); RefreshData(); return; }
            ReasonBox.Clear(); FromDatePicker.SelectedDate = NextWorkingDay(SystemTimeService.Today); ToDatePicker.SelectedDate = NextWorkingDay(SystemTimeService.Today); RefreshData();
            MessageBox.Show("Đơn nghỉ phép đã được gửi và đang chờ Admin duyệt.", "Gửi đơn thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CancelRequest_Click(object sender, RoutedEventArgs e)
        {
            var request = (sender as Button).Tag as LeaveRequest;
            if (request == null || !request.CanCancel) return;
            if (request.FromDate.Date <= SystemTimeService.Today) { MessageBox.Show("Không thể tự hủy đơn khi ngày nghỉ đã bắt đầu. Hãy liên hệ Admin để xử lý.", "Không thể hủy đơn", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (MessageBox.Show("Bạn muốn hủy đơn nghỉ từ " + request.DateRange + "?", "Xác nhận hủy đơn", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            request.Status = "Đã hủy";
            request.CancelledAt = SystemTimeService.Now;
            HrmDataService.AddAudit(_account.Username, "Hủy đơn nghỉ phép", request.DateRange);
            if (!HrmDataService.SaveChanges()) { ShowSaveError(); RefreshData(); return; }
            RefreshData();
        }

        private static void ShowSaveError()
        {
            MessageBox.Show("Không thể lưu thay đổi vào SQL Server. Dữ liệu đã được khôi phục.\n\n" + HrmDataService.LastSaveError, "Lỗi lưu dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private static DateTime NextWorkingDay(DateTime date)
        {
            DateTime next = date.AddDays(1);
            while (!HrmBusinessService.IsWorkingDay(next)) next = next.AddDays(1);
            return next;
        }
    }
}
