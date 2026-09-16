using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HRMDesktop.Models;
using HRMDesktop.Views;
using HRMDesktop.Views.Admin;
using HRMDesktop.Views.Employee;

namespace HRMDesktop
{
    public partial class MainWindow : Window
    {
        private readonly UserAccount _account;
        private Button _activeButton;

        public MainWindow(UserAccount account)
        {
            InitializeComponent();
            _account = account;
            PortalLabel.Text = account.IsAdmin ? "Cổng quản trị" : "Cổng nhân viên";
            FullNameText.Text = account.FullName;
            RoleText.Text = account.RoleDisplay;
            AvatarText.Text = account.Initial;
            CurrentDateText.Text = DateTime.Today.ToString("dddd, dd/MM/yyyy");
            BuildNavigation();
            Navigate("dashboard", "Tổng quan");
        }

        private void BuildNavigation()
        {
            AddNav("▦", "Tổng quan", "dashboard");
            AddSectionLabel(_account.IsAdmin ? "NGHIỆP VỤ QUẢN TRỊ" : "TIỆN ÍCH CÁ NHÂN");
            if (_account.IsAdmin)
            {
                AddNav("♙", "Quản lý nhân viên", "employees");
                AddNav("▤", "Lương và thưởng", "salary");
                AddNav("✓", "Duyệt nghỉ phép", "leave-approval");
                AddNav("◷", "Quản lý chấm công", "attendance-admin");
                AddNav("▥", "Báo cáo thống kê", "reports");
            }
            else
            {
                AddNav("◷", "Chấm công", "my-attendance");
                AddNav("✓", "Nghỉ phép", "my-leave");
                AddNav("▤", "Phiếu lương", "my-salary");
                AddNav("♙", "Hồ sơ cá nhân", "my-profile");
            }
        }

        private void AddNav(string icon, string label, string key)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            panel.Children.Add(new TextBlock { Text = icon, Width = 36, FontSize = 17, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
            panel.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
            var button = new Button { Content = panel, Tag = key, Style = (Style)FindResource("NavButton") };
            button.Click += Navigation_Click;
            NavigationPanel.Children.Add(button);
        }

        private void AddSectionLabel(string label)
        {
            NavigationPanel.Children.Add(new TextBlock
            {
                Text = label,
                Foreground = new SolidColorBrush(Color.FromRgb(125, 145, 166)),
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(30, 15, 12, 7)
            });
        }

        private void Navigation_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;
            string key = Convert.ToString(button.Tag);
            var panel = button.Content as StackPanel;
            string title = panel == null || panel.Children.Count < 2 ? "HRM Desktop" : ((TextBlock)panel.Children[1]).Text;
            Navigate(key, title);
        }

        private void Navigate(string key, string title)
        {
            HeaderTitle.Text = title;
            SetActiveNavigation(key);
            switch (key)
            {
                case "employees": MainFrame.Content = new EmployeesPage(); break;
                case "salary": MainFrame.Content = new SalaryPage(); break;
                case "leave-approval": MainFrame.Content = new LeaveApprovalPage(); break;
                case "attendance-admin": MainFrame.Content = new AttendanceManagementPage(); break;
                case "reports": MainFrame.Content = new ReportsPage(); break;
                case "my-attendance": MainFrame.Content = new MyAttendancePage(_account); break;
                case "my-leave": MainFrame.Content = new MyLeavePage(_account); break;
                case "my-salary": MainFrame.Content = new MySalaryPage(_account); break;
                case "my-profile": MainFrame.Content = new MyProfilePage(_account, RefreshAccountHeader); break;
                default: MainFrame.Content = new DashboardPage(_account); break;
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            HeaderTitle.Text = "Cài đặt";
            SetActiveButton(SettingsButton);
            MainFrame.Content = new SettingsPage(_account);
        }

        private void SetActiveNavigation(string key)
        {
            foreach (var child in NavigationPanel.Children)
            {
                var button = child as Button;
                if (button != null && Convert.ToString(button.Tag) == key)
                {
                    SetActiveButton(button);
                    return;
                }
            }
        }

        private void SetActiveButton(Button button)
        {
            if (_activeButton != null)
            {
                _activeButton.ClearValue(Button.BackgroundProperty);
                _activeButton.ClearValue(Button.ForegroundProperty);
            }
            _activeButton = button;
            if (_activeButton != null)
            {
                _activeButton.Background = new SolidColorBrush(Color.FromRgb(232, 246, 243));
                _activeButton.Foreground = new SolidColorBrush(Color.FromRgb(15, 118, 110));
            }
        }

        private void RefreshAccountHeader(UserAccount account)
        {
            FullNameText.Text = account.FullName;
            AvatarText.Text = account.Initial;
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Bạn có chắc chắn muốn đăng xuất?", "Xác nhận đăng xuất", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            new LoginWindow().Show();
            Close();
        }
    }
}
