using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using BudgetTrackerApp.Services;
using Microsoft.Maui.Controls;
using System.Linq; // For Sum()

namespace BudgetTrackerApp.ViewModels
{
    // Simple model to hold category spending data for the view
    public class CategorySpending : BaseViewModel
    {
        private string _categoryName;
        private decimal _totalAmount;

        public string CategoryName
        {
            get => _categoryName;
            set => SetProperty(ref _categoryName, value);
        }
        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }
    }

    public class ReportsViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private DateTime _startDate = DateTime.Today.AddMonths(-1); // Default: last month
        private DateTime _endDate = DateTime.Today; // Default: today
        private ObservableCollection<CategorySpending> _spendingByCategory;
        private decimal _totalSpending;
        private string _statusMessage;

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                {
                    LoadReportDataCommand.Execute(null); // Reload data when date changes
                }
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                {
                    LoadReportDataCommand.Execute(null); // Reload data when date changes
                }
            }
        }

        public ObservableCollection<CategorySpending> SpendingByCategory
        {
            get => _spendingByCategory;
            set => SetProperty(ref _spendingByCategory, value);
        }

        public decimal TotalSpending
        {
            get => _totalSpending;
            set => SetProperty(ref _totalSpending, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand LoadReportDataCommand { get; }

        public ReportsViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            SpendingByCategory = new ObservableCollection<CategorySpending>();
            LoadReportDataCommand = new Command(async () => await LoadReportData());

            // Load initial data
            Task.Run(async () => await LoadReportData());
        }

        public async Task LoadReportData()
        {
            StatusMessage = "Loading report data...";
            await _databaseService.InitializeAsync();

            try
            {
                var spendingData = await _databaseService.GetSpendingByCategoryAsync(StartDate, EndDate);

                SpendingByCategory.Clear();
                decimal currentTotal = 0;

                foreach (var kvp in spendingData.OrderBy(x => x.Key)) // Order by category name
                {
                    SpendingByCategory.Add(new CategorySpending { CategoryName = kvp.Key, TotalAmount = kvp.Value });
                    currentTotal += kvp.Value;
                }

                TotalSpending = currentTotal; // Update total spending
                StatusMessage = $"Report loaded for {StartDate:d} to {EndDate:d}.";
            }
            catch (Exception ex)
            {
                // Log the exception ex
                StatusMessage = "Error loading report data.";
                TotalSpending = 0;
                SpendingByCategory.Clear();
            }
            OnPropertyChanged(nameof(SpendingByCategory));
            OnPropertyChanged(nameof(TotalSpending));
        }
    }
}
