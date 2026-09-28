using System;
using System.Linq;
using System.Globalization;
using System.Windows.Controls;
using HRMDesktop.Models;
using HRMDesktop.Services;

namespace HRMDesktop.Views.Employee
{
    public partial class MySalaryPage : Page
    {
        private readonly UserAccount _account;

        public MySalaryPage(UserAccount account)
        {
            InitializeComponent();
            _account = account;
            var months = HrmDataService.Salaries.Where(x => x.EmployeeId == account.EmployeeId)
                .Select(x => x.Month).Distinct()
                .OrderByDescending(x => DateTime.ParseExact(x, "MM/yyyy", CultureInfo.InvariantCulture))
                .ToList();
            foreach (string monthValue in months)
            {
                MonthBox.Items.Add(new ComboBoxItem { Content = "Tháng " + monthValue, Tag = monthValue });
            }
            if (MonthBox.Items.Count > 0) MonthBox.SelectedIndex = 0;
            ApplyMonthFilter();
        }

        private void MonthBox_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (SalaryGrid != null) ApplyMonthFilter();
        }

        private void ApplyMonthFilter()
        {
            var selected = MonthBox.SelectedItem as ComboBoxItem;
            string month = selected == null ? SystemTimeService.Today.ToString("MM/yyyy") : Convert.ToString(selected.Tag);
            var rows = HrmDataService.Salaries.Where(x => x.EmployeeId == _account.EmployeeId && x.Month == month).ToList();
            SalaryGrid.ItemsSource = rows;
            var current = rows.FirstOrDefault();
            BaseText.Text = current == null ? "--" : current.BaseSalaryDisplay;
            BonusText.Text = current == null ? "--" : current.BonusDisplay;
            DeductionText.Text = current == null ? "--" : current.DeductionDisplay;
            NetText.Text = current == null ? "--" : current.NetSalaryDisplay;
        }
    }
}
