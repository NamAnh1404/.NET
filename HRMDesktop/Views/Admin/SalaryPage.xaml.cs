using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HRMDesktop.Models;
using HRMDesktop.Services;

namespace HRMDesktop.Views.Admin
{
    public partial class SalaryPage : Page
    {
        private SalaryRecord _editingSalary;

        public SalaryPage()
        {
            InitializeComponent();
            for (int offset = 0; offset < 12; offset++)
            {
                DateTime month = DateTime.Today.AddMonths(-offset);
                MonthFilter.Items.Add(new ComboBoxItem { Content = "Tháng " + month.ToString("MM/yyyy"), Tag = month.ToString("MM/yyyy") });
            }
            MonthFilter.SelectedIndex = 0;
            ApplyMonthFilter();
        }
        private void RefreshSummary()
        {
            var rows = SalaryGrid.ItemsSource as System.Collections.Generic.IEnumerable<SalaryRecord> ?? Enumerable.Empty<SalaryRecord>();
            TotalPayrollText.Text = rows.Sum(x => x.NetSalary).ToString("N0") + " đ";
            TotalBonusText.Text = rows.Sum(x => x.Bonus).ToString("N0") + " đ";
            PendingText.Text = rows.Count(x => x.Status == "Chờ thanh toán") + " phiếu";
            SalaryGrid.Items.Refresh();
        }

        private void MonthFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (SalaryGrid != null) ApplyMonthFilter();
        }

        private void ApplyMonthFilter()
        {
            var selected = MonthFilter.SelectedItem as ComboBoxItem;
            string month = selected == null ? DateTime.Today.ToString("MM/yyyy") : Convert.ToString(selected.Tag);
            SalaryPeriodTitle.Text = "Bảng lương tháng " + month;
            SalaryGrid.ItemsSource = MockDataService.Salaries.Where(x => x.Month == month).OrderBy(x => x.EmployeeName).ToList();
            SalaryGrid.SelectedItem = null;
            RefreshSummary();
        }
        private void PaySelected_Click(object sender, RoutedEventArgs e)
        {
            var selected = SalaryGrid.SelectedItem as SalaryRecord;
            if (selected == null) { MessageBox.Show("Hãy chọn một phiếu lương cần xử lý.", "Lương và thưởng", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            if (selected.Status == "Đã thanh toán") { MessageBox.Show("Phiếu lương này đã được thanh toán trước đó.", "Lương và thưởng", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            selected.Status = "Đã thanh toán";
            RefreshSummary();
            MessageBox.Show("Đã cập nhật trạng thái thanh toán cho " + selected.EmployeeName + ".", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string SelectedMonth
        {
            get
            {
                var selected = MonthFilter.SelectedItem as ComboBoxItem;
                return selected == null ? DateTime.Today.ToString("MM/yyyy") : Convert.ToString(selected.Tag);
            }
        }

        private void GeneratePayroll_Click(object sender, RoutedEventArgs e)
        {
            string month = SelectedMonth;
            var employees = MockDataService.Employees.Where(x => x.Status == "Đang làm việc" && !MockDataService.Salaries.Any(s => s.EmployeeId == x.Id && s.Month == month)).ToList();
            if (employees.Count == 0)
            {
                MessageBox.Show("Tất cả nhân viên đang làm việc đã có phiếu lương trong tháng " + month + ".", "Tạo bảng lương", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (MessageBox.Show("Tạo " + employees.Count + " phiếu lương còn thiếu cho tháng " + month + "?", "Xác nhận tạo bảng lương", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            int nextId = MockDataService.Salaries.Count == 0 ? 1 : MockDataService.Salaries.Max(x => x.Id) + 1;
            foreach (var employee in employees)
            {
                MockDataService.Salaries.Add(new SalaryRecord { Id = nextId++, EmployeeId = employee.Id, EmployeeName = employee.FullName, EmployeeCode = employee.Code, Month = month, BaseSalary = employee.BaseSalary, Bonus = 0, Deduction = 0, Status = "Chờ thanh toán" });
            }
            ApplyMonthFilter();
            MessageBox.Show("Đã tạo " + employees.Count + " phiếu lương. Hãy cập nhật thưởng và khấu trừ trước khi thanh toán.", "Tạo bảng lương thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EditSalary_Click(object sender, RoutedEventArgs e)
        {
            var selected = SalaryGrid.SelectedItem as SalaryRecord;
            if (selected == null) { MessageBox.Show("Hãy chọn một phiếu lương cần chỉnh sửa.", "Lương và thưởng", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            if (selected.Status == "Đã thanh toán") { MessageBox.Show("Không thể sửa phiếu lương đã thanh toán.", "Lương và thưởng", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            _editingSalary = selected;
            SalaryEmployeeText.Text = selected.EmployeeName + "  •  " + selected.Month + "  •  Lương cơ bản " + selected.BaseSalaryDisplay;
            BonusBox.Text = selected.Bonus.ToString("0");
            DeductionBox.Text = selected.Deduction.ToString("0");
            SalaryFormError.Visibility = Visibility.Collapsed;
            SalaryFormOverlay.Visibility = Visibility.Visible;
            BonusBox.Focus();
        }

        private void CancelSalaryEdit_Click(object sender, RoutedEventArgs e)
        {
            SalaryFormOverlay.Visibility = Visibility.Collapsed;
            _editingSalary = null;
        }

        private void SaveSalaryEdit_Click(object sender, RoutedEventArgs e)
        {
            if (_editingSalary == null) return;
            decimal bonus;
            decimal deduction;
            string bonusInput = BonusBox.Text.Replace(".", string.Empty).Replace(",", string.Empty).Replace(" ", string.Empty);
            string deductionInput = DeductionBox.Text.Replace(".", string.Empty).Replace(",", string.Empty).Replace(" ", string.Empty);
            if (!decimal.TryParse(bonusInput, out bonus) || bonus < 0 || !decimal.TryParse(deductionInput, out deduction) || deduction < 0)
            {
                SalaryFormError.Text = "Thưởng và khấu trừ phải là số không âm.";
                SalaryFormError.Visibility = Visibility.Visible;
                return;
            }
            if (deduction > _editingSalary.BaseSalary + bonus)
            {
                SalaryFormError.Text = "Khấu trừ không được lớn hơn tổng lương và thưởng.";
                SalaryFormError.Visibility = Visibility.Visible;
                return;
            }
            _editingSalary.Bonus = bonus;
            _editingSalary.Deduction = deduction;
            SalaryFormOverlay.Visibility = Visibility.Collapsed;
            _editingSalary = null;
            RefreshSummary();
            MessageBox.Show("Đã cập nhật thưởng và khấu trừ.", "Lương và thưởng", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
