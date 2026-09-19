using System.Windows;
using System.Windows.Controls;
using HRMDesktop.Models;
using HRMDesktop.Services;
using System.Linq;

namespace HRMDesktop.Views
{
    public partial class SettingsPage : Page
    {
        public SettingsPage(UserAccount account)
        {
            InitializeComponent();
            NameBox.Text = account.FullName;
            EmailBox.Text = account.Email;
            AttendanceNotification.IsChecked = account.AttendanceNotificationEnabled;
            LeaveNotification.IsChecked = account.LeaveNotificationEnabled;
            SalaryNotification.IsChecked = account.SalaryNotificationEnabled;
            _account = account;
            if (account.IsAdmin)
            {
                AttendanceNotification.Content = "Thông báo khi nhân viên chưa chấm công";
                LeaveNotification.Content = "Thông báo khi có đơn nghỉ phép mới";
                SalaryNotification.Content = "Thông báo khi còn phiếu lương chờ thanh toán";
            }
        }

        private readonly UserAccount _account;

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _account.AttendanceNotificationEnabled = AttendanceNotification.IsChecked == true;
            _account.LeaveNotificationEnabled = LeaveNotification.IsChecked == true;
            _account.SalaryNotificationEnabled = SalaryNotification.IsChecked == true;
            var credential = MockDataService.Credentials.FirstOrDefault(x => x.Username == _account.Username);
            if (credential != null)
            {
                credential.AttendanceNotificationEnabled = _account.AttendanceNotificationEnabled;
                credential.LeaveNotificationEnabled = _account.LeaveNotificationEnabled;
                credential.SalaryNotificationEnabled = _account.SalaryNotificationEnabled;
            }
            MockDataService.SaveChanges();
            MessageBox.Show("Đã lưu cài đặt thông báo.", "Cài đặt", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
