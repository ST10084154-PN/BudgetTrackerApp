using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using BudgetTrackerApp.Models;
using BudgetTrackerApp.Services;
using Microsoft.Maui.Controls;

namespace BudgetTrackerApp.ViewModels
{
    public class GoalsViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private DateTime _selectedMonth = DateTime.Today;
        private decimal _minSpendingGoal;
        private decimal _maxSpendingGoal;
        private string _statusMessage;
        private Goal _currentGoal; // Holds the loaded goal for the selected month

        public DateTime SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                if (SetProperty(ref _selectedMonth, value))
                {
                    // When month changes, load the goal for the new month
                    Task.Run(async () => await LoadGoalForSelectedMonth());
                }
            }
        }

        public decimal MinSpendingGoal
        {
            get => _minSpendingGoal;
            set => SetProperty(ref _minSpendingGoal, value);
        }

        public decimal MaxSpendingGoal
        {
            get => _maxSpendingGoal;
            set => SetProperty(ref _maxSpendingGoal, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // Display formatted month/year string
        public string SelectedMonthDisplay => SelectedMonth.ToString("MMMM yyyy", CultureInfo.CurrentCulture);

        public ICommand SaveGoalCommand { get; }
        public ICommand PreviousMonthCommand { get; }
        public ICommand NextMonthCommand { get; }


        public GoalsViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            SaveGoalCommand = new Command(async () => await SaveGoal());
            PreviousMonthCommand = new Command(() => ChangeMonth(-1));
            NextMonthCommand = new Command(() => ChangeMonth(1));

            // Load initial goal
            Task.Run(async () => await LoadGoalForSelectedMonth());
        }

        private void ChangeMonth(int monthOffset)
        {
            SelectedMonth = SelectedMonth.AddMonths(monthOffset);
            OnPropertyChanged(nameof(SelectedMonthDisplay)); // Update display string
            // Loading goal is triggered by the SelectedMonth setter
        }

        public async Task LoadGoalForSelectedMonth()
        {
            StatusMessage = "Loading goal...";
            await _databaseService.InitializeAsync();
            _currentGoal = await _databaseService.GetGoalAsync(SelectedMonth.Year, SelectedMonth.Month);

            if (_currentGoal != null)
            {
                MinSpendingGoal = _currentGoal.MinSpendingGoal;
                MaxSpendingGoal = _currentGoal.MaxSpendingGoal;
                StatusMessage = $"Goal loaded for {SelectedMonthDisplay}.";
            }
            else
            {
                // Reset fields if no goal exists for this month
                MinSpendingGoal = 0;
                MaxSpendingGoal = 0;
                StatusMessage = $"No goal set for {SelectedMonthDisplay}. Enter values and save.";
                _currentGoal = null; // Ensure we know we need to insert later
            }
            OnPropertyChanged(nameof(MinSpendingGoal)); // Ensure UI updates
            OnPropertyChanged(nameof(MaxSpendingGoal));
        }

        private async Task SaveGoal()
        {
            StatusMessage = "Saving...";
            await _databaseService.InitializeAsync();

            Goal goalToSave;
            if (_currentGoal != null)
            {
                // Update existing goal
                goalToSave = _currentGoal;
                goalToSave.MinSpendingGoal = MinSpendingGoal;
                goalToSave.MaxSpendingGoal = MaxSpendingGoal;
            }
            else
            {
                // Create new goal
                goalToSave = new Goal
                {
                    Year = SelectedMonth.Year,
                    Month = SelectedMonth.Month,
                    MinSpendingGoal = MinSpendingGoal,
                    MaxSpendingGoal = MaxSpendingGoal
                    // Add UserId if implementing per-user goals
                };
            }


            int result = await _databaseService.SaveGoalAsync(goalToSave);

            if (result > 0)
            {
                _currentGoal = goalToSave; // Update the local reference after saving
                StatusMessage = $"Goal for {SelectedMonthDisplay} saved successfully.";
            }
            else
            {
                StatusMessage = "Failed to save goal.";
            }
        }
    }
}
