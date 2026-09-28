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
            PasswordSection.Visibility = account.IsAdmin ? Visibility.Collapsed : Visibility.Visible;
            if (account.IsAdmin)
            {
                AttendanceNotification.Content = "Thông báo khi nhân viên chưa chấm công";
                LeaveNotification.Content = "Thông báo khi có đơn nghỉ phép mới";
                SalaryNotification.Content = "Thông báo khi còn phiếu lương chờ thanh toán";
            }
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            PasswordErrorText.Visibility = Visibility.Collapsed;
            var credential = HrmDataService.Credentials.FirstOrDefault(x => x.Username == _account.Username);
            if (credential == null)
            {
                ShowPasswordError("Không tìm thấy tài khoản đăng nhập.");
                return;
            }
            if (!PasswordSecurity.Verify(CurrentPasswordBox.Password, credential))
            {
                ShowPasswordError("Mật khẩu hiện tại không đúng.");
                CurrentPasswordBox.SelectAll();
                CurrentPasswordBox.Focus();
                return;
            }
            string newPassword = NewPasswordBox.Password;
            if (newPassword.Length < 6)
            {
                ShowPasswordError("Mật khẩu mới phải có ít nhất 6 ký tự.");
                NewPasswordBox.Focus();
                return;
            }
            if (PasswordSecurity.Verify(newPassword, credential))
            {
                ShowPasswordError("Mật khẩu mới phải khác mật khẩu hiện tại.");
                return;
            }
            if (newPassword != ConfirmPasswordBox.Password)
            {
                ShowPasswordError("Xác nhận mật khẩu mới chưa khớp.");
                ConfirmPasswordBox.SelectAll();
                ConfirmPasswordBox.Focus();
                return;
            }

            string oldSalt = credential.PasswordSalt;
            string oldHash = credential.PasswordHash;
            PasswordSecurity.UpdatePassword(credential, newPassword);
            int auditCount = HrmDataService.AuditLogs.Count;
            HrmDataService.AddAudit(_account.Username, "Đổi mật khẩu", "Nhân viên tự cập nhật mật khẩu đăng nhập");
            if (!HrmDataService.SaveChanges())
            {
                credential.PasswordSalt = oldSalt;
                credential.PasswordHash = oldHash;
                while (HrmDataService.AuditLogs.Count > auditCount) HrmDataService.AuditLogs.RemoveAt(HrmDataService.AuditLogs.Count - 1);
                ShowPasswordError("Không thể lưu mật khẩu mới. " + HrmDataService.LastSaveError);
                return;
            }

            CurrentPasswordBox.Clear();
            NewPasswordBox.Clear();
            ConfirmPasswordBox.Clear();
            MessageBox.Show("Đổi mật khẩu thành công. Hãy sử dụng mật khẩu mới ở lần đăng nhập tiếp theo.", "Đổi mật khẩu", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowPasswordError(string message)
        {
            PasswordErrorText.Text = message;
            PasswordErrorText.Visibility = Visibility.Visible;
        }

        private readonly UserAccount _account;

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _account.AttendanceNotificationEnabled = AttendanceNotification.IsChecked == true;
            _account.LeaveNotificationEnabled = LeaveNotification.IsChecked == true;
            _account.SalaryNotificationEnabled = SalaryNotification.IsChecked == true;
            var credential = HrmDataService.Credentials.FirstOrDefault(x => x.Username == _account.Username);
            if (credential != null)
            {
                credential.AttendanceNotificationEnabled = _account.AttendanceNotificationEnabled;
                credential.LeaveNotificationEnabled = _account.LeaveNotificationEnabled;
                credential.SalaryNotificationEnabled = _account.SalaryNotificationEnabled;
            }
            if (!HrmDataService.SaveChanges())
            {
                var persistedCredential = HrmDataService.Credentials.FirstOrDefault(x => x.Username == _account.Username);
                if (persistedCredential != null)
                {
                    _account.AttendanceNotificationEnabled = persistedCredential.AttendanceNotificationEnabled;
                    _account.LeaveNotificationEnabled = persistedCredential.LeaveNotificationEnabled;
                    _account.SalaryNotificationEnabled = persistedCredential.SalaryNotificationEnabled;
                    AttendanceNotification.IsChecked = _account.AttendanceNotificationEnabled;
                    LeaveNotification.IsChecked = _account.LeaveNotificationEnabled;
                    SalaryNotification.IsChecked = _account.SalaryNotificationEnabled;
                }
                MessageBox.Show("Không thể lưu cài đặt vào SQL Server. Dữ liệu đã được khôi phục.\n\n" + HrmDataService.LastSaveError, "Lỗi lưu dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            MessageBox.Show("Đã lưu cài đặt thông báo.", "Cài đặt", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
