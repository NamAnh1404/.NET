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
            WorkDatePicker.SelectedDate = DateTime.Today;
            WorkDatePicker.DisplayDateEnd = DateTime.Today;
            ApplyFilter();
        }

        private void Filter_Changed(object sender, EventArgs e)
        {
            if (AttendanceGrid != null && AdjustmentGrid != null) ApplyFilter();
        }

        private void ApplyFilter()
        {
            DateTime date = WorkDatePicker == null || !WorkDatePicker.SelectedDate.HasValue ? DateTime.Today : WorkDatePicker.SelectedDate.Value.Date;
            string keyword = SearchBox == null ? string.Empty : SearchBox.Text.Trim().ToLower();
            string department = "Tất cả phòng ban";
            if (DepartmentFilter != null && DepartmentFilter.SelectedItem is ComboBoxItem) department = Convert.ToString(((ComboBoxItem)DepartmentFilter.SelectedItem).Content);

            var records = MockDataService.Attendance.Where(x => x.WorkDate.Date == date).ToList();
            var rows = MockDataService.Employees
                .Where(x => x.Status == "Đang làm việc")
                .Select(employee =>
                {
                    var record = records.FirstOrDefault(x => x.EmployeeId == employee.Id);
                    if (record != null) return record;
                    bool onLeave = MockDataService.LeaveRequests.Any(x => x.EmployeeId == employee.Id && x.Status == "Đã duyệt" && date >= x.FromDate.Date && date <= x.ToDate.Date);
                    return new AttendanceRecord
                    {
                        EmployeeId = employee.Id,
                        EmployeeName = employee.FullName,
                        EmployeeCode = employee.Code,
                        Department = employee.Department,
                        WorkDate = date,
                        CheckIn = "--",
                        CheckOut = "--",
                        Status = onLeave ? "Nghỉ phép" : (date < DateTime.Today ? "Vắng mặt" : "Chưa chấm công")
                    };
                })
                .Where(x => MatchesFilter(x.EmployeeName, x.EmployeeCode, x.Department, keyword, department))
                .OrderBy(x => x.EmployeeName)
                .ToList();

            AttendanceGrid.ItemsSource = rows;
            CheckedText.Text = rows.Count(x => x.CheckIn != "--").ToString();
            WorkingText.Text = rows.Count(x => x.Status == "Đang làm việc").ToString();
            MissingText.Text = rows.Count(x => x.Status == "Vắng mặt" || x.Status == "Chưa chấm công").ToString();

            var adjustments = MockDataService.AttendanceAdjustments
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

        private void UpdateAdjustment(AttendanceAdjustmentRequest request, bool approve)
        {
            if (request == null || !request.CanReview) return;
            var employee = MockDataService.GetEmployee(request.EmployeeId);
            if (employee == null)
            {
                MessageBox.Show("Không tìm thấy hồ sơ nhân viên của yêu cầu này.", "Điều chỉnh chấm công", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (approve && MockDataService.LeaveRequests.Any(x => x.EmployeeId == request.EmployeeId && x.Status == "Đã duyệt" && request.WorkDate.Date >= x.FromDate.Date && request.WorkDate.Date <= x.ToDate.Date))
            {
                MessageBox.Show("Không thể duyệt vì nhân viên đã được duyệt nghỉ phép trong ngày này.", "Điều chỉnh chấm công", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string action = approve ? "duyệt" : "từ chối";
            if (MessageBox.Show("Xác nhận " + action + " yêu cầu điều chỉnh của " + request.EmployeeName + " ngày " + request.WorkDateDisplay + "?", "Xác nhận xử lý", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            if (approve)
            {
                var record = MockDataService.Attendance.FirstOrDefault(x => x.EmployeeId == request.EmployeeId && x.WorkDate.Date == request.WorkDate.Date);
                if (record == null)
                {
                    int nextId = MockDataService.Attendance.Count == 0 ? 1 : MockDataService.Attendance.Max(x => x.Id) + 1;
                    record = new AttendanceRecord
                    {
                        Id = nextId,
                        EmployeeId = employee.Id,
                        EmployeeName = employee.FullName,
                        EmployeeCode = employee.Code,
                        Department = employee.Department,
                        WorkDate = request.WorkDate.Date,
                        CheckIn = request.RequestedCheckIn,
                        CheckOut = string.IsNullOrWhiteSpace(request.RequestedCheckOut) ? "--" : request.RequestedCheckOut
                    };
                    MockDataService.Attendance.Add(record);
                }
                else
                {
                    record.CheckIn = request.RequestedCheckIn;
                    if (!string.IsNullOrWhiteSpace(request.RequestedCheckOut)) record.CheckOut = request.RequestedCheckOut;
                }
                record.Status = record.CheckOut == "--" ? (record.WorkDate.Date == DateTime.Today ? "Đang làm việc" : "Thiếu giờ ra") : "Đã kết thúc";
                request.Status = "Đã duyệt";
            }
            else
            {
                request.Status = "Từ chối";
            }
            request.ReviewedAt = DateTime.Now;
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
