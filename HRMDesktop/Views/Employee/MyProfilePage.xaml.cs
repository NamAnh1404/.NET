using System.Windows;
using System.Windows.Controls;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using HRMDesktop.Models;
using HRMDesktop.Services;

namespace HRMDesktop.Views.Employee
{
    public partial class MyProfilePage : Page
    {
        private readonly UserAccount _account;
        private readonly HRMDesktop.Models.Employee _employee;
        private readonly Action<UserAccount> _accountUpdated;
        public MyProfilePage(UserAccount account, Action<UserAccount> accountUpdated)
        {
            InitializeComponent(); _account = account; _accountUpdated = accountUpdated; _employee = MockDataService.GetEmployee(account.EmployeeId);
            ProfileNameText.Text = account.FullName; InitialText.Text = account.Initial;
            if (_employee == null) return;
            CodeDepartmentText.Text = _employee.Code + "  •  " + _employee.Department; FullNameBox.Text = _employee.FullName; BirthDatePicker.SelectedDate = _employee.DateOfBirth; EmailBox.Text = _employee.Email; PhoneBox.Text = _employee.Phone; DepartmentBox.Text = _employee.Department; PositionBox.Text = _employee.Position;
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_employee == null || string.IsNullOrWhiteSpace(EmailBox.Text)) { MessageBox.Show("Hãy nhập email liên hệ.", "Dữ liệu chưa hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            string email = EmailBox.Text.Trim().ToLower();
            string phone = PhoneBox.Text.Trim();
            if (!Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$")) { MessageBox.Show("Email chưa đúng định dạng.", "Dữ liệu chưa hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (MockDataService.Employees.Any(x => x.Id != _employee.Id && string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase))) { MessageBox.Show("Email này đang được sử dụng bởi nhân viên khác.", "Dữ liệu chưa hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (!string.IsNullOrWhiteSpace(phone) && !Regex.IsMatch(phone, @"^[0-9]{9,11}$")) { MessageBox.Show("Số điện thoại phải gồm 9 đến 11 chữ số.", "Dữ liệu chưa hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            _employee.Email = email; _employee.Phone = string.IsNullOrWhiteSpace(phone) ? "Chưa cập nhật" : phone;
            _account.FullName = _employee.FullName; _account.Email = _employee.Email; ProfileNameText.Text = _employee.FullName;
            InitialText.Text = _account.Initial;
            MockDataService.AddAudit(_account.Username, "Cập nhật thông tin liên hệ", _employee.Code);
            MockDataService.SaveChanges();
            if (_accountUpdated != null) _accountUpdated(_account);
            MessageBox.Show("Đã cập nhật thông tin liên hệ.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
