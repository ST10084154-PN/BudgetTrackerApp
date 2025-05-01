using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using BudgetTrackerApp.Models;
using BudgetTrackerApp.Services;
using Microsoft.Maui.Controls; // For Command, Shell navigation etc.
using System; // For DateTime

namespace BudgetTrackerApp.ViewModels
{
    // Base class for ViewModels to implement INotifyPropertyChanged
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Helper method to set properties and raise PropertyChanged event
        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    // --- Login ViewModel ---
    public class LoginViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private string _username;
        private string _password; 
        private string _loginMessage;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string LoginMessage
        {
            get => _loginMessage;
            set => SetProperty(ref _loginMessage, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand GoToRegisterCommand { get; } // Command to navigate to registration

        public LoginViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            LoginCommand = new Command(async () => await OnLogin());
            GoToRegisterCommand = new Command(async () => await OnGoToRegister()); // Placeholder for navigation
        }

        private async Task OnLogin()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                LoginMessage = "Please enter username and password.";
                return;
            }

            await _databaseService.InitializeAsync(); // Ensure DB is ready
            var user = await _databaseService.GetUserAsync(Username);


            if (user != null && user.PasswordHash == Password) 
            {
                LoginMessage = "Login Successful!";

                await Shell.Current.GoToAsync("//MainPage/ExpensesPage"); 
            }
            else
            {
                LoginMessage = "Invalid username or password.";
            }
        }

        private async Task OnGoToRegister()
        {
            LoginMessage = ""; // Clear message
                               // Navigate to a Registration Page
            await Shell.Current.GoToAsync("RegisterPage"); 
        }
    }


    // --- Expense List ViewModel ---
    public class ExpenseListViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private ObservableCollection<Expense> _expenses;
        private DateTime _startDate = DateTime.Today.AddMonths(-1); // Default: last month
        private DateTime _endDate = DateTime.Today; // Default: today

        public ObservableCollection<Expense> Expenses
        {
            get => _expenses;
            set => SetProperty(ref _expenses, value);
        }

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                    LoadExpensesCommand.Execute(null); // Reload expenses when date changes
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                    LoadExpensesCommand.Execute(null); // Reload expenses when date changes
            }
        }

        public ICommand LoadExpensesCommand { get; }
        public ICommand AddExpenseCommand { get; }
        public ICommand ViewExpenseDetailsCommand { get; } // Command to view details/photo

        public ExpenseListViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            Expenses = new ObservableCollection<Expense>();
            LoadExpensesCommand = new Command(async () => await LoadExpenses());
            AddExpenseCommand = new Command(async () => await GoToAddExpense());
            ViewExpenseDetailsCommand = new Command<Expense>(async (expense) => await GoToExpenseDetails(expense));

            // Load expenses initially
            // Task.Run(async () => await LoadExpenses()); // Or call from page OnAppearing
        }

        public async Task LoadExpenses()
        {
            await _databaseService.InitializeAsync();
            var expenseList = await _databaseService.GetExpensesByPeriodAsync(StartDate, EndDate);
            Expenses.Clear();
            foreach (var expense in expenseList)
            {
                Expenses.Add(expense);
            }
            OnPropertyChanged(nameof(Expenses)); // Notify UI
        }

        private async Task GoToAddExpense()
        {
            // Navigate to the Add Expense Page
            await Shell.Current.GoToAsync("AddExpensePage"); // Define route
        }

        private async Task GoToExpenseDetails(Expense expense)
        {
            if (expense == null) return;
            // Navigate to a details page, passing the expense ID or object
            await Shell.Current.GoToAsync($"ExpenseDetailsPage?expenseId={expense.Id}");
        }
    }

    // --- Add/Edit Expense ViewModel ---
    public class AddExpenseViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private Expense _currentExpense;
        private ObservableCollection<Category> _categories;
        private Category _selectedCategory;
        private string _pageTitle = "Add Expense";
        private string _photoButtonText = "Add Photo";

        public Expense CurrentExpense
        {
            get => _currentExpense;
            set => SetProperty(ref _currentExpense, value);
        }

        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        public Category SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value) && CurrentExpense != null)
                {
                    CurrentExpense.CategoryId = value?.Id ?? 0; // Update expense's category ID
                }
            }
        }

        public string PageTitle
        {
            get => _pageTitle;
            set => SetProperty(ref _pageTitle, value);
        }

        public string PhotoButtonText
        {
            get => _photoButtonText;
            set => SetProperty(ref _photoButtonText, value);
        }


        public ICommand SaveExpenseCommand { get; }
        public ICommand AddPhotoCommand { get; }
        public ICommand LoadCategoriesCommand { get; }

        public AddExpenseViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            CurrentExpense = new Expense { Date = DateTime.Today, StartTime = DateTime.Now.TimeOfDay, EndTime = DateTime.Now.TimeOfDay }; // Default values
            Categories = new ObservableCollection<Category>();

            SaveExpenseCommand = new Command(async () => await SaveExpense());
            AddPhotoCommand = new Command(async () => await OnAddPhoto());
            LoadCategoriesCommand = new Command(async () => await LoadCategories());

            Task.Run(async () => await LoadCategories()); // Load categories on initialization
        }

        public async Task LoadCategories()
        {
            await _databaseService.InitializeAsync();
            var categoryList = await _databaseService.GetCategoriesAsync();
            Categories.Clear();
            foreach (var cat in categoryList)
            {
                Categories.Add(cat);
            }
            // If editing, set the selected category
            // if (CurrentExpense?.CategoryId > 0) { ... find and set SelectedCategory ... }
        }


        private async Task SaveExpense()
        {
            if (CurrentExpense == null || SelectedCategory == null || CurrentExpense.Amount <= 0 || string.IsNullOrWhiteSpace(CurrentExpense.Description))
            {
                // Show validation error to user
                await Shell.Current.DisplayAlert("Error", "Please fill all required fields (Description, Amount, Category).", "OK");
                return;
            }

            CurrentExpense.CategoryId = SelectedCategory.Id; // Ensure ID is set
            await _databaseService.InitializeAsync();
            await _databaseService.SaveExpenseAsync(CurrentExpense);

            // Navigate back to the expense list
            await Shell.Current.GoToAsync(".."); // Go back one level
        }

        private async Task OnAddPhoto()
        {
            // --- Photo Logic ---

            try
            {
                var photo = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = "Select Expense Photo"
                });

                if (photo != null)
                {
                    // Save the photo to local app data storage
                    string localFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string newFileName = $"{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}"; // Unique name
                    string localFilePath = Path.Combine(localFolder, newFileName);

                    using (var sourceStream = await photo.OpenReadAsync())
                    using (var localStream = File.OpenWrite(localFilePath))
                    {
                        await sourceStream.CopyToAsync(localStream);
                    }

                    // Store the *path* to the photo in the Expense object
                    CurrentExpense.PhotoPath = localFilePath;
                    PhotoButtonText = "Change Photo"; // Update button text
                    OnPropertyChanged(nameof(CurrentExpense)); // Notify UI if displaying the path/image
                    await Shell.Current.DisplayAlert("Success", $"Photo saved at: {localFilePath}", "OK");

                }
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                // MediaPicker is not supported on the device
                await Shell.Current.DisplayAlert("Error", "Photo picking not supported on this device.", "OK");
            }
            catch (PermissionException pEx)
            {
                // Permissions not granted
                await Shell.Current.DisplayAlert("Error", "Permission denied for accessing photos.", "OK");
                // Prompt the user to grant permissions here.
            }
            catch (Exception ex)
            {
                // Other error
                await Shell.Current.DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        // Method to load an existing expense for editing (called during navigation)
        public async Task LoadExpenseForEdit(int expenseId)
        {
            await _databaseService.InitializeAsync();
            var expense = await _databaseService.GetExpenseAsync(expenseId);
            if (expense != null)
            {
                CurrentExpense = expense;
                PageTitle = "Edit Expense"; // Change title
                // Load categories if not already loaded (or ensure they are)
                if (Categories.Count == 0) await LoadCategories();
                // Set the selected category in the picker
                SelectedCategory = Categories.FirstOrDefault(c => c.Id == expense.CategoryId);
                PhotoButtonText = string.IsNullOrEmpty(expense.PhotoPath) ? "Add Photo" : "Change Photo";
            }
        }
    }

}
