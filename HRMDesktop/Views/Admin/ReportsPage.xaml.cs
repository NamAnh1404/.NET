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
            var activeEmployees = MockDataService.Employees.Where(x => x.Status == "Đang làm việc").ToList();
            foreach (var group in activeEmployees.GroupBy(x => x.Department).OrderByDescending(x => x.Count()))
            {
                var header = new Grid { Margin = new System.Windows.Thickness(0, 0, 0, 7) };
                header.Children.Add(new TextBlock { Text = group.Key, Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)) });
                header.Children.Add(new TextBlock { Text = group.Count() + " nhân viên", HorizontalAlignment = System.Windows.HorizontalAlignment.Right, FontWeight = System.Windows.FontWeights.SemiBold });
                DepartmentBars.Children.Add(header);
                DepartmentBars.Children.Add(new ProgressBar { Value = group.Count(), Maximum = Math.Max(1, activeEmployees.Count), Height = 9, Foreground = (Brush)FindResource("PrimaryBrush"), Margin = new System.Windows.Thickness(0, 0, 0, 18) });
            }
            string currentMonth = DateTime.Today.ToString("MM/yyyy");
            PayrollText.Text = MockDataService.Salaries.Where(x => x.Month == currentMonth).Sum(x => x.NetSalary).ToString("N0") + " đ";
            LeaveText.Text = MockDataService.LeaveRequests.Count(x => x.Status == "Chờ duyệt") + " đơn";
            int approvedLeave = activeEmployees.Count(employee => MockDataService.LeaveRequests.Any(x => x.EmployeeId == employee.Id && x.Status == "Đã duyệt" && DateTime.Today >= x.FromDate.Date && DateTime.Today <= x.ToDate.Date));
            int expected = Math.Max(0, activeEmployees.Count - approvedLeave);
            int present = MockDataService.Attendance.Count(x => activeEmployees.Any(employee => employee.Id == x.EmployeeId) && x.WorkDate.Date == DateTime.Today && x.CheckIn != "--");
            AttendanceText.Text = (expected == 0 ? 0 : Math.Min(100, present * 100 / expected)) + "%";
            DepartmentCountText.Text = activeEmployees.Select(x => x.Department).Distinct().Count() + " phòng";
        }
    }
}
