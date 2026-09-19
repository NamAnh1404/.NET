using System.Windows;
using System.Windows.Input;
using HRMDesktop.Services;

namespace HRMDesktop
{
    public partial class LoginWindow : Window
    {
        public LoginWindow() { InitializeComponent(); }

        private void Login_Click(object sender, RoutedEventArgs e) { TryLogin(); }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) TryLogin();
        }

        private void TryLogin()
        {
            var account = AuthService.Login(UsernameBox.Text, PasswordBox.Password);
            if (account == null)
            {
                ErrorText.Text = string.IsNullOrWhiteSpace(AuthService.LastError) ? "Tên đăng nhập hoặc mật khẩu không đúng." : AuthService.LastError;
                ErrorText.Visibility = Visibility.Visible;
                PasswordBox.SelectAll();
                PasswordBox.Focus();
                return;
            }

            ErrorText.Visibility = Visibility.Collapsed;
            new MainWindow(account).Show();
            Close();
        }
    }
}
