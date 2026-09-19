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
        private bool _showArchived;

        public EmployeesPage()
        {
            InitializeComponent();
            UpdateSummary();
            ApplyFilter();
        }

        private void UpdateSummary()
        {
            TotalText.Text = MockDataService.Employees.Count.ToString();
            var active = MockDataService.Employees.Where(x => x.Status == "Đang làm việc" && HrmBusinessService.IsEmployedOn(x, SystemTimeService.Today)).ToList();
            ActiveText.Text = active.Count.ToString();
            SalaryText.Text = active.Sum(x => x.BaseSalary).ToString("N0") + " đ";
        }

        private void Filter_Changed(object sender, EventArgs e) { if (EmployeeGrid != null) ApplyFilter(); }

        private void ApplyFilter()
        {
            string keyword = SearchBox == null ? string.Empty : SearchBox.Text.Trim().ToLower();
            string department = "Tất cả phòng ban";
            if (DepartmentFilter != null && DepartmentFilter.SelectedItem is ComboBoxItem) department = Convert.ToString(((ComboBoxItem)DepartmentFilter.SelectedItem).Content);
            EmployeeGrid.ItemsSource = MockDataService.Employees.Where(x =>
                (_showArchived ? x.Status == "Đã nghỉ việc" : x.Status != "Đã nghỉ việc") &&
                (string.IsNullOrEmpty(keyword) || (x.FullName ?? string.Empty).ToLower().Contains(keyword) || (x.Code ?? string.Empty).ToLower().Contains(keyword) || (x.Email ?? string.Empty).ToLower().Contains(keyword)) &&
                (department == "Tất cả phòng ban" || x.Department == department)).ToList();
        }

        private void ToggleArchive_Click(object sender, RoutedEventArgs e)
        {
            _showArchived = !_showArchived;
            ArchiveToggleButton.Content = _showArchived ? "Quay lại nhân sự" : "Hồ sơ lưu trữ";
            ApplyFilter();
        }

        private void EmployeeName_Click(object sender, RoutedEventArgs e)
        {
            var employee = (sender as Button).Tag as HRMDesktop.Models.Employee;
            if (employee == null) return;
            string employment = "\nNgày vào làm: " + employee.HireDateDisplay + (employee.TerminationDate.HasValue ? "\nNgày nghỉ việc: " + employee.TerminationDate.Value.ToString("dd/MM/yyyy") : string.Empty) + "\nTrạng thái: " + employee.Status;
            MessageBox.Show(employee.FullName + "\nMã nhân viên: " + employee.Code + "\nPhòng ban: " + employee.Department + "\nChức vụ: " + employee.Position + "\nEmail: " + employee.Email + "\nSố điện thoại: " + employee.Phone + employment,
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
            EmployeeHireDatePicker.SelectedDate = SystemTimeService.Today;
            AnnualLeaveAllowanceBox.Text = "12";
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
            EmployeeHireDatePicker.SelectedDate = employee.HireDate;
            AnnualLeaveAllowanceBox.Text = employee.AnnualLeaveAllowance.ToString();
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
            int annualLeaveAllowance;
            string salaryInput = EmployeeSalaryBox.Text.Replace(".", string.Empty).Replace(",", string.Empty).Replace(" ", string.Empty);

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(position) || !EmployeeBirthDatePicker.SelectedDate.HasValue || !EmployeeHireDatePicker.SelectedDate.HasValue)
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
            if (EmployeeHireDatePicker.SelectedDate.Value.Date > SystemTimeService.Today)
            {
                ShowFormError("Ngày vào làm không được nằm trong tương lai.");
                return;
            }
            if (!int.TryParse(AnnualLeaveAllowanceBox.Text.Trim(), out annualLeaveAllowance) || annualLeaveAllowance < 0 || annualLeaveAllowance > 30)
            {
                ShowFormError("Số ngày phép năm phải từ 0 đến 30 ngày.");
                return;
            }

            if (_editingEmployee == null)
            {
                int id = MockDataService.Employees.Count == 0 ? 1 : MockDataService.Employees.Max(x => x.Id) + 1;
                var employee = new HRMDesktop.Models.Employee
                {
                    Id = id, Code = code, FullName = fullName, Email = email,
                    Phone = string.IsNullOrWhiteSpace(phone) ? "Chưa cập nhật" : phone,
                    Department = department, Position = position,
                    DateOfBirth = EmployeeBirthDatePicker.SelectedDate.Value,
                    HireDate = EmployeeHireDatePicker.SelectedDate.Value.Date,
                    AnnualLeaveAllowance = annualLeaveAllowance,
                    BaseSalary = salary, Status = status,
                    TerminationDate = status == "Đã nghỉ việc" ? (DateTime?)SystemTimeService.Today : (DateTime?)null
                };
                MockDataService.Employees.Add(employee);
                int employmentPeriodId = MockDataService.EmploymentPeriods.Count == 0 ? 1 : MockDataService.EmploymentPeriods.Max(x => x.Id) + 1;
                MockDataService.EmploymentPeriods.Add(new EmploymentPeriod { Id = employmentPeriodId, EmployeeId = id, StartDate = employee.HireDate, EndDate = employee.TerminationDate });
                int salaryHistoryId = MockDataService.SalaryHistories.Count == 0 ? 1 : MockDataService.SalaryHistories.Max(x => x.Id) + 1;
                MockDataService.SalaryHistories.Add(new SalaryHistory { Id = salaryHistoryId, EmployeeId = id, EffectiveFrom = employee.HireDate, BaseSalary = salary });
                MockDataService.Credentials.Add(PasswordSecurity.CreateCredential(id, code.ToLowerInvariant(), "123", "Employee"));
                MockDataService.Credentials.Last().IsLocked = status != "Đang làm việc";
                MockDataService.AddAudit("admin", "Thêm nhân viên", code + " - " + fullName);
            }
            else
            {
                decimal oldSalary = _editingEmployee.BaseSalary;
                _editingEmployee.Code = code;
                _editingEmployee.FullName = fullName;
                _editingEmployee.Email = email;
                _editingEmployee.Phone = string.IsNullOrWhiteSpace(phone) ? "Chưa cập nhật" : phone;
                _editingEmployee.Department = department;
                _editingEmployee.Position = position;
                _editingEmployee.DateOfBirth = EmployeeBirthDatePicker.SelectedDate.Value;
                _editingEmployee.HireDate = EmployeeHireDatePicker.SelectedDate.Value.Date;
                _editingEmployee.AnnualLeaveAllowance = annualLeaveAllowance;
                _editingEmployee.BaseSalary = salary;
                _editingEmployee.Status = status;
                _editingEmployee.TerminationDate = status == "Đã nghỉ việc" ? (DateTime?)(_editingEmployee.TerminationDate ?? SystemTimeService.Today) : (DateTime?)null;
                if (status == "Đã nghỉ việc")
                {
                    var openEmploymentPeriod = MockDataService.EmploymentPeriods.Where(x => x.EmployeeId == _editingEmployee.Id && !x.EndDate.HasValue).OrderByDescending(x => x.StartDate).FirstOrDefault();
                    if (openEmploymentPeriod != null) openEmploymentPeriod.EndDate = SystemTimeService.Today;
                }
                var credential = MockDataService.Credentials.FirstOrDefault(x => x.EmployeeId == _editingEmployee.Id);
                if (credential != null) credential.IsLocked = status != "Đang làm việc";
                if (oldSalary != salary)
                {
                    int historyId = MockDataService.SalaryHistories.Count == 0 ? 1 : MockDataService.SalaryHistories.Max(x => x.Id) + 1;
                    MockDataService.SalaryHistories.Add(new SalaryHistory { Id = historyId, EmployeeId = _editingEmployee.Id, EffectiveFrom = new DateTime(SystemTimeService.Today.Year, SystemTimeService.Today.Month, 1), BaseSalary = salary });
                }
                MockDataService.AddAudit("admin", "Cập nhật nhân viên", code + " - " + fullName);
                EmployeeGrid.Items.Refresh();
            }

            MockDataService.SaveChanges();
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
            if (employee.Status == "Đã nghỉ việc") { MessageBox.Show("Nhân viên này đã được cho nghỉ việc. Lịch sử vẫn được giữ lại để tra cứu.", "Quản lý nhân viên", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            if (MessageBox.Show("Cho nhân viên " + employee.FullName + " nghỉ việc từ hôm nay? Tài khoản sẽ bị khóa nhưng toàn bộ lịch sử chấm công, nghỉ phép và lương vẫn được giữ.", "Xác nhận nghỉ việc", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            employee.Status = "Đã nghỉ việc";
            employee.TerminationDate = SystemTimeService.Today;
            var openPeriod = MockDataService.EmploymentPeriods.Where(x => x.EmployeeId == employee.Id && !x.EndDate.HasValue).OrderByDescending(x => x.StartDate).FirstOrDefault();
            if (openPeriod != null) openPeriod.EndDate = SystemTimeService.Today;
            var account = MockDataService.Credentials.FirstOrDefault(x => x.EmployeeId == employee.Id);
            if (account != null) account.IsLocked = true;
            MockDataService.AddAudit("admin", "Cho nghỉ việc", employee.Code + " - " + employee.FullName);
            MockDataService.SaveChanges();
            UpdateSummary();
            ApplyFilter();
            MessageBox.Show("Đã khóa tài khoản và chuyển nhân viên sang trạng thái đã nghỉ việc. Dữ liệu lịch sử được bảo toàn.", "Quản lý nhân viên", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RestoreEmployee_Click(object sender, RoutedEventArgs e)
        {
            var employee = (sender as Button).Tag as HRMDesktop.Models.Employee;
            if (employee == null || employee.Status != "Đã nghỉ việc") return;
            if (MessageBox.Show("Khôi phục " + employee.FullName + " làm việc từ hôm nay? Tài khoản đăng nhập sẽ được mở lại và một giai đoạn công tác mới được tạo.", "Khôi phục nhân viên", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            employee.Status = "Đang làm việc";
            employee.TerminationDate = null;
            int nextId = MockDataService.EmploymentPeriods.Count == 0 ? 1 : MockDataService.EmploymentPeriods.Max(x => x.Id) + 1;
            MockDataService.EmploymentPeriods.Add(new EmploymentPeriod { Id = nextId, EmployeeId = employee.Id, StartDate = SystemTimeService.Today });
            var account = MockDataService.Credentials.FirstOrDefault(x => x.EmployeeId == employee.Id);
            if (account != null) account.IsLocked = false;
            MockDataService.AddAudit("admin", "Khôi phục nhân viên", employee.Code + " - " + employee.FullName);
            MockDataService.SaveChanges();
            UpdateSummary();
            ApplyFilter();
            MessageBox.Show("Đã khôi phục nhân viên và mở lại tài khoản đăng nhập.", "Quản lý nhân viên", MessageBoxButton.OK, MessageBoxImage.Information);
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
