using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using HRMDesktop.Models;
using HRMDesktop.Services;

namespace HRMDesktop.Views.Admin
{
    public partial class EmployeesPage : Page
    {
        private HRMDesktop.Models.Employee _editingEmployee;

        public EmployeesPage()
        {
            InitializeComponent();
            UpdateSummary();
            ApplyFilter();
        }

        private void UpdateSummary()
        {
            TotalText.Text = MockDataService.Employees.Count.ToString();
            ActiveText.Text = MockDataService.Employees.Count(x => x.Status == "Đang làm việc").ToString();
            SalaryText.Text = MockDataService.Employees.Sum(x => x.BaseSalary).ToString("N0") + " đ";
        }

        private void Filter_Changed(object sender, EventArgs e) { if (EmployeeGrid != null) ApplyFilter(); }

        private void ApplyFilter()
        {
            string keyword = SearchBox == null ? string.Empty : SearchBox.Text.Trim().ToLower();
            string department = "Tất cả phòng ban";
            if (DepartmentFilter != null && DepartmentFilter.SelectedItem is ComboBoxItem) department = Convert.ToString(((ComboBoxItem)DepartmentFilter.SelectedItem).Content);
            EmployeeGrid.ItemsSource = MockDataService.Employees.Where(x =>
                (string.IsNullOrEmpty(keyword) || (x.FullName ?? string.Empty).ToLower().Contains(keyword) || (x.Code ?? string.Empty).ToLower().Contains(keyword) || (x.Email ?? string.Empty).ToLower().Contains(keyword)) &&
                (department == "Tất cả phòng ban" || x.Department == department)).ToList();
        }

        private void EmployeeName_Click(object sender, RoutedEventArgs e)
        {
            var employee = (sender as Button).Tag as HRMDesktop.Models.Employee;
            if (employee == null) return;
            MessageBox.Show(employee.FullName + "\nMã nhân viên: " + employee.Code + "\nPhòng ban: " + employee.Department + "\nChức vụ: " + employee.Position + "\nEmail: " + employee.Email + "\nSố điện thoại: " + employee.Phone,
                "Chi tiết nhân viên", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddEmployee_Click(object sender, RoutedEventArgs e)
        {
            _editingEmployee = null;
            int id = MockDataService.Employees.Count == 0 ? 1 : MockDataService.Employees.Max(x => x.Id) + 1;
            EmployeeNameBox.Clear();
            EmployeeCodeBox.Text = "NV" + id.ToString("000");
            EmployeeEmailBox.Clear();
            EmployeePhoneBox.Clear();
            EmployeePositionBox.Clear();
            EmployeeSalaryBox.Clear();
            EmployeeBirthDatePicker.SelectedDate = new DateTime(2000, 1, 1);
            EmployeeDepartmentBox.SelectedIndex = 0;
            EmployeeStatusBox.SelectedIndex = 0;
            EmployeeFormTitle.Text = "Thêm nhân viên";
            SaveEmployeeButton.Content = "Thêm nhân viên";
            EmployeeFormError.Visibility = Visibility.Collapsed;
            EmployeeFormOverlay.Visibility = Visibility.Visible;
            EmployeeNameBox.Focus();
        }

        private void EditEmployee_Click(object sender, RoutedEventArgs e)
        {
            var employee = (sender as Button).Tag as HRMDesktop.Models.Employee;
            if (employee == null) return;
            _editingEmployee = employee;
            EmployeeNameBox.Text = employee.FullName;
            EmployeeCodeBox.Text = employee.Code;
            EmployeeEmailBox.Text = employee.Email;
            EmployeePhoneBox.Text = employee.Phone == "Chưa cập nhật" ? string.Empty : employee.Phone;
            EmployeePositionBox.Text = employee.Position;
            EmployeeSalaryBox.Text = employee.BaseSalary.ToString("0");
            EmployeeBirthDatePicker.SelectedDate = employee.DateOfBirth;
            SelectComboBoxItem(EmployeeDepartmentBox, employee.Department);
            SelectComboBoxItem(EmployeeStatusBox, employee.Status);
            EmployeeFormTitle.Text = "Cập nhật nhân viên";
            SaveEmployeeButton.Content = "Lưu thay đổi";
            EmployeeFormError.Visibility = Visibility.Collapsed;
            EmployeeFormOverlay.Visibility = Visibility.Visible;
            EmployeeNameBox.Focus();
        }

        private void CancelEmployee_Click(object sender, RoutedEventArgs e)
        {
            EmployeeFormOverlay.Visibility = Visibility.Collapsed;
        }

        private void SaveEmployee_Click(object sender, RoutedEventArgs e)
        {
            string fullName = EmployeeNameBox.Text.Trim();
            string code = EmployeeCodeBox.Text.Trim().ToUpper();
            string email = EmployeeEmailBox.Text.Trim().ToLower();
            string phone = EmployeePhoneBox.Text.Trim();
            string position = EmployeePositionBox.Text.Trim();
            string department = GetSelectedContent(EmployeeDepartmentBox);
            string status = GetSelectedContent(EmployeeStatusBox);
            decimal salary;
            string salaryInput = EmployeeSalaryBox.Text.Replace(".", string.Empty).Replace(",", string.Empty).Replace(" ", string.Empty);

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(position) || !EmployeeBirthDatePicker.SelectedDate.HasValue)
            {
                ShowFormError("Vui lòng nhập đầy đủ các trường có dấu *.");
                return;
            }
            if (!Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
            {
                ShowFormError("Email chưa đúng định dạng.");
                return;
            }
            if (MockDataService.Employees.Any(x => x != _editingEmployee && string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase)))
            {
                ShowFormError("Mã nhân viên đã tồn tại.");
                return;
            }
            if (MockDataService.Employees.Any(x => x != _editingEmployee && string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase)))
            {
                ShowFormError("Email đã được sử dụng bởi nhân viên khác.");
                return;
            }
            if (!decimal.TryParse(salaryInput, out salary) || salary < 0)
            {
                ShowFormError("Lương cơ bản phải là một số hợp lệ.");
                return;
            }
            if (EmployeeBirthDatePicker.SelectedDate.Value.Date >= DateTime.Today)
            {
                ShowFormError("Ngày sinh phải nhỏ hơn ngày hiện tại.");
                return;
            }
            if (!string.IsNullOrWhiteSpace(phone) && !Regex.IsMatch(phone, @"^[0-9]{9,11}$"))
            {
                ShowFormError("Số điện thoại phải gồm 9 đến 11 chữ số.");
                return;
            }
            if (EmployeeBirthDatePicker.SelectedDate.Value.Date > DateTime.Today.AddYears(-16))
            {
                ShowFormError("Nhân viên phải từ đủ 16 tuổi.");
                return;
            }

            if (_editingEmployee == null)
            {
                int id = MockDataService.Employees.Count == 0 ? 1 : MockDataService.Employees.Max(x => x.Id) + 1;
                MockDataService.Employees.Add(new HRMDesktop.Models.Employee
                {
                    Id = id, Code = code, FullName = fullName, Email = email,
                    Phone = string.IsNullOrWhiteSpace(phone) ? "Chưa cập nhật" : phone,
                    Department = department, Position = position,
                    DateOfBirth = EmployeeBirthDatePicker.SelectedDate.Value,
                    BaseSalary = salary, Status = status
                });
            }
            else
            {
                _editingEmployee.Code = code;
                _editingEmployee.FullName = fullName;
                _editingEmployee.Email = email;
                _editingEmployee.Phone = string.IsNullOrWhiteSpace(phone) ? "Chưa cập nhật" : phone;
                _editingEmployee.Department = department;
                _editingEmployee.Position = position;
                _editingEmployee.DateOfBirth = EmployeeBirthDatePicker.SelectedDate.Value;
                _editingEmployee.BaseSalary = salary;
                _editingEmployee.Status = status;
                foreach (var item in MockDataService.Attendance.Where(x => x.EmployeeId == _editingEmployee.Id)) { item.EmployeeName = fullName; item.EmployeeCode = code; item.Department = department; }
                foreach (var item in MockDataService.AttendanceAdjustments.Where(x => x.EmployeeId == _editingEmployee.Id)) { item.EmployeeName = fullName; item.EmployeeCode = code; item.Department = department; }
                foreach (var item in MockDataService.LeaveRequests.Where(x => x.EmployeeId == _editingEmployee.Id)) { item.EmployeeName = fullName; item.EmployeeCode = code; }
                foreach (var item in MockDataService.Salaries.Where(x => x.EmployeeId == _editingEmployee.Id)) { item.EmployeeName = fullName; item.EmployeeCode = code; }
                EmployeeGrid.Items.Refresh();
            }

            EmployeeFormOverlay.Visibility = Visibility.Collapsed;
            UpdateSummary();
            ApplyFilter();
            MessageBox.Show(_editingEmployee == null ? "Đã thêm nhân viên " + fullName + "." : "Đã cập nhật hồ sơ " + fullName + ".", "Quản lý nhân viên", MessageBoxButton.OK, MessageBoxImage.Information);
            _editingEmployee = null;
        }

        private void DeleteEmployee_Click(object sender, RoutedEventArgs e)
        {
            var employee = (sender as Button).Tag as HRMDesktop.Models.Employee;
            if (employee == null) return;
            if (MessageBox.Show("Xóa nhân viên " + employee.FullName + " và toàn bộ dữ liệu liên quan?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            foreach (var item in MockDataService.Attendance.Where(x => x.EmployeeId == employee.Id).ToList()) MockDataService.Attendance.Remove(item);
            foreach (var item in MockDataService.AttendanceAdjustments.Where(x => x.EmployeeId == employee.Id).ToList()) MockDataService.AttendanceAdjustments.Remove(item);
            foreach (var item in MockDataService.LeaveRequests.Where(x => x.EmployeeId == employee.Id).ToList()) MockDataService.LeaveRequests.Remove(item);
            foreach (var item in MockDataService.Salaries.Where(x => x.EmployeeId == employee.Id).ToList()) MockDataService.Salaries.Remove(item);
            MockDataService.Employees.Remove(employee);
            UpdateSummary();
            ApplyFilter();
            MessageBox.Show("Đã xóa hồ sơ nhân viên.", "Quản lý nhân viên", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowFormError(string message)
        {
            EmployeeFormError.Text = message;
            EmployeeFormError.Visibility = Visibility.Visible;
        }

        private static string GetSelectedContent(ComboBox comboBox)
        {
            var item = comboBox.SelectedItem as ComboBoxItem;
            return item == null ? string.Empty : Convert.ToString(item.Content);
        }

        private static void SelectComboBoxItem(ComboBox comboBox, string value)
        {
            foreach (var item in comboBox.Items.OfType<ComboBoxItem>())
            {
                if (string.Equals(Convert.ToString(item.Content), value, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
            comboBox.SelectedIndex = 0;
        }
    }
}
