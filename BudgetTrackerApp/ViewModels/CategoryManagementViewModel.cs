using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using BudgetTrackerApp.Models;
using BudgetTrackerApp.Services;
using Microsoft.Maui.Controls; // For Command, Shell etc.

namespace BudgetTrackerApp.ViewModels
{
    public class CategoryManagementViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private ObservableCollection<Category> _categories;
        private string _newCategoryName;
        private string _statusMessage;

        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        public string NewCategoryName
        {
            get => _newCategoryName;
            set => SetProperty(ref _newCategoryName, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand LoadCategoriesCommand { get; }
        public ICommand AddCategoryCommand { get; }
        public ICommand DeleteCategoryCommand { get; }

        public CategoryManagementViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            Categories = new ObservableCollection<Category>();

            LoadCategoriesCommand = new Command(async () => await LoadCategories());
            AddCategoryCommand = new Command(async () => await AddCategory(), CanAddCategory);
            DeleteCategoryCommand = new Command<Category>(async (category) => await DeleteCategory(category));

            // Ensure commands react to property changes
            this.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(NewCategoryName))
                {
                    (AddCategoryCommand as Command)?.ChangeCanExecute();
                }
            };
        }

        public async Task LoadCategories()
        {
            StatusMessage = string.Empty;
            await _databaseService.InitializeAsync();
            var categoryList = await _databaseService.GetCategoriesAsync();
            Categories.Clear();
            foreach (var cat in categoryList)
            {
                Categories.Add(cat);
            }
            OnPropertyChanged(nameof(Categories)); // Notify UI
        }

        private bool CanAddCategory()
        {
            return !string.IsNullOrWhiteSpace(NewCategoryName);
        }

        private async Task AddCategory()
        {
            if (!CanAddCategory()) return;

            await _databaseService.InitializeAsync();

            // Check if category already exists (case-insensitive check recommended)
            var existingCategory = await _databaseService.GetCategoryByNameAsync(NewCategoryName.Trim());
            if (existingCategory != null)
            {
                StatusMessage = $"Category '{NewCategoryName.Trim()}' already exists.";
                return;
            }


            var newCategory = new Category { Name = NewCategoryName.Trim() };
            int result = await _databaseService.SaveCategoryAsync(newCategory);

            if (result > 0)
            {
                Categories.Add(newCategory); // Add to observable collection
                NewCategoryName = string.Empty; // Clear input field
                StatusMessage = "Category added successfully.";
            }
            else
            {
                StatusMessage = "Failed to add category.";
            }
        }

        private async Task DeleteCategory(Category category)
        {
            if (category == null) return;

            // Confirmation dialog
            bool confirm = await Shell.Current.DisplayAlert("Confirm Delete", $"Are you sure you want to delete the category '{category.Name}'? This cannot be undone.", "Yes", "No");
            if (!confirm) return;



            await _databaseService.InitializeAsync();
            int result = await _databaseService.DeleteCategoryAsync(category);

            if (result > 0)
            {
                Categories.Remove(category); // Remove from observable collection
                StatusMessage = "Category deleted.";
            }
            else
            {
                StatusMessage = "Failed to delete category.";
            }
        }
    }
}
