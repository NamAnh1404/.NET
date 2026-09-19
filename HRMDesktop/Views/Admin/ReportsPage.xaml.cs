using System.Linq;
using System;
using System.Windows.Controls;
using System.Windows.Media;
using HRMDesktop.Services;

namespace HRMDesktop.Views.Admin
{
    public partial class ReportsPage : Page
    {
        public ReportsPage()
        {
            InitializeComponent();
            var activeEmployees = MockDataService.Employees.Where(x => x.Status == "Đang làm việc" && HrmBusinessService.IsEmployedOn(x, SystemTimeService.Today)).ToList();
            foreach (var group in activeEmployees.GroupBy(x => x.Department).OrderByDescending(x => x.Count()))
            {
                var header = new Grid { Margin = new System.Windows.Thickness(0, 0, 0, 7) };
                header.Children.Add(new TextBlock { Text = group.Key, Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)) });
                header.Children.Add(new TextBlock { Text = group.Count() + " nhân viên", HorizontalAlignment = System.Windows.HorizontalAlignment.Right, FontWeight = System.Windows.FontWeights.SemiBold });
                DepartmentBars.Children.Add(header);
                DepartmentBars.Children.Add(new ProgressBar { Value = group.Count(), Maximum = Math.Max(1, activeEmployees.Count), Height = 9, Foreground = (Brush)FindResource("PrimaryBrush"), Margin = new System.Windows.Thickness(0, 0, 0, 18) });
            }
            string currentMonth = SystemTimeService.Today.ToString("MM/yyyy");
            PayrollText.Text = MockDataService.Salaries.Where(x => x.Month == currentMonth).Sum(x => x.NetSalary).ToString("N0") + " đ";
            LeaveText.Text = MockDataService.LeaveRequests.Count(x => x.Status == "Chờ duyệt") + " đơn";
            int approvedLeave = activeEmployees.Count(employee => HrmBusinessService.HasApprovedLeave(employee.Id, SystemTimeService.Today));
            int expected = HrmBusinessService.IsWorkingDay(SystemTimeService.Today) ? Math.Max(0, activeEmployees.Count - approvedLeave) : 0;
            int present = MockDataService.Attendance.Where(x => activeEmployees.Any(employee => employee.Id == x.EmployeeId) && x.WorkDate.Date == SystemTimeService.Today && x.CheckInAt.HasValue).Select(x => x.EmployeeId).Distinct().Count();
            AttendanceText.Text = !HrmBusinessService.IsWorkingDay(SystemTimeService.Today) ? "Không áp dụng" : (expected == 0 ? 0 : Math.Min(100, present * 100 / expected)) + "%";
            DepartmentCountText.Text = activeEmployees.Select(x => x.Department).Distinct().Count() + " phòng";
        }
    }
}
