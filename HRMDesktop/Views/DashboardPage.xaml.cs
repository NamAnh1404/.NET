using System.Linq;
using System;
using System.Windows.Controls;
using HRMDesktop.Models;
using HRMDesktop.Services;

namespace HRMDesktop.Views
{
    public partial class DashboardPage : Page
    {
        public DashboardPage(UserAccount account)
        {
            InitializeComponent();
            WelcomeText.Text = "Xin chào, " + account.FullName;
            SubtitleText.Text = account.IsAdmin ? "Theo dõi nhanh tình hình nhân sự trong hôm nay." : "Tổng hợp thông tin công việc và quyền lợi của bạn.";
            if (account.IsAdmin)
            {
                var activeEmployees = HrmDataService.Employees.Where(x => x.Status == "Đang làm việc" && HrmBusinessService.IsEmployedOn(x, SystemTimeService.Today)).ToList();
                var currentMonth = SystemTimeService.Today.ToString("MM/yyyy");
                RecentList.ItemsSource = HrmDataService.Attendance.OrderByDescending(x => x.WorkDate).ThenByDescending(x => x.Id).Take(5).ToList();
                PendingLeaveText.Text = HrmDataService.LeaveRequests.Count(x => x.Status == "Chờ duyệt") + " đơn";
                OnlineText.Text = HrmDataService.Attendance.Where(x => x.WorkDate.Date == SystemTimeService.Today && x.Status == "Đang làm việc" && activeEmployees.Any(employee => employee.Id == x.EmployeeId)).Select(x => x.EmployeeId).Distinct().Count() + " người";
                Metric1Label.Text = "Tổng nhân viên"; Metric1Value.Text = activeEmployees.Count.ToString(); Metric1Note.Text = "Đang làm việc trong hệ thống";
                Metric2Label.Text = "Có mặt hôm nay"; Metric2Value.Text = HrmDataService.Attendance.Where(x => x.WorkDate.Date == SystemTimeService.Today && x.CheckInAt.HasValue && activeEmployees.Any(employee => employee.Id == x.EmployeeId)).Select(x => x.EmployeeId).Distinct().Count().ToString(); Metric2Note.Text = "Đã ghi nhận giờ vào";
                Metric3Label.Text = "Tổng quỹ lương"; Metric3Value.Text = HrmDataService.Salaries.Where(x => x.Month == currentMonth).Sum(x => x.NetSalary).ToString("N0") + " đ"; Metric3Note.Text = "Tháng " + currentMonth;
                Metric4Label.Text = "Chờ phê duyệt"; Metric4Value.Text = HrmDataService.LeaveRequests.Count(x => x.Status == "Chờ duyệt").ToString(); Metric4Note.Text = "Đơn nghỉ phép";
                DashboardNote.Text = "Các chỉ số được tổng hợp từ dữ liệu nhân viên, chấm công, nghỉ phép và bảng lương.";
            }
            else
            {
                var today = HrmDataService.Attendance.FirstOrDefault(x => x.EmployeeId == account.EmployeeId && x.WorkDate.Date == SystemTimeService.Today);
                var salary = HrmDataService.Salaries.FirstOrDefault(x => x.EmployeeId == account.EmployeeId && x.Month == SystemTimeService.Today.ToString("MM/yyyy"));
                var myLeaves = HrmDataService.LeaveRequests.Where(x => x.EmployeeId == account.EmployeeId).ToList();
                RecentList.ItemsSource = HrmDataService.Attendance.Where(x => x.EmployeeId == account.EmployeeId).OrderByDescending(x => x.WorkDate).ThenByDescending(x => x.Id).Take(5).ToList();
                PendingLeaveLabel.Text = "Đơn của bạn đang chờ duyệt";
                PendingLeaveText.Text = myLeaves.Count(x => x.Status == "Chờ duyệt") + " đơn";
                OnlineLabel.Text = "Trạng thái chấm công hôm nay";
                OnlineText.Text = today == null ? "Chưa chấm công" : today.Status;
                Metric1Label.Text = "Giờ vào hôm nay"; Metric1Value.Text = today == null ? "--" : today.CheckIn; Metric1Note.Text = today == null ? "Chưa chấm công" : today.Status;
                Metric2Label.Text = "Giờ ra hôm nay"; Metric2Value.Text = today == null ? "--" : today.CheckOut; Metric2Note.Text = "Cập nhật theo phiên làm việc";
                Metric3Label.Text = "Thực nhận tháng này"; Metric3Value.Text = salary == null ? "--" : salary.NetSalaryDisplay; Metric3Note.Text = salary == null ? "Chưa có bảng lương" : salary.Status;
                Metric4Label.Text = "Đơn nghỉ phép"; Metric4Value.Text = myLeaves.Count.ToString(); Metric4Note.Text = myLeaves.Count(x => x.Status == "Chờ duyệt") + " đơn đang chờ";
                DashboardNote.Text = "Bạn có thể chấm công, gửi đơn nghỉ phép, xem phiếu lương và cập nhật hồ sơ từ menu bên trái.";
            }
        }
    }
}
