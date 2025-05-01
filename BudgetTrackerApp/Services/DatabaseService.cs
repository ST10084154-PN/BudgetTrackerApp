using SQLite; // SQLite-net-pcl namespace
using BudgetTrackerApp.Models; // Access to model classes
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;

namespace BudgetTrackerApp.Services
{
    // Service class to handle all database interactions
    public class DatabaseService
    {
        private SQLiteAsyncConnection _database; // Asynchronous connection 

        // --- Initialization ---

        // Gets the full path to the database file
        private string GetDatabasePath()
        {
            // Define database file name
            const string databaseFilename = "BudgetTrackerSQLite.db3";
            // Get the folder path based on the platform (Android, iOS, etc.)
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // Combine path and filename
            return Path.Combine(basePath, databaseFilename);
        }

        // Initializes the database connection and creates tables if they don't exist
        public async Task InitializeAsync()
        {
            if (_database == null)
            {
                string dbPath = GetDatabasePath();
                _database = new SQLiteAsyncConnection(dbPath);

                await _database.CreateTableAsync<User>();
                await _database.CreateTableAsync<Category>();
                await _database.CreateTableAsync<Expense>();
                await _database.CreateTableAsync<Goal>();
            }
        }

        // --- User Operations ---

        public Task<User> GetUserAsync(string username)
        {
            return _database.Table<User>().FirstOrDefaultAsync(u => u.Username == username);
        }

        public Task<int> SaveUserAsync(User user)
        {
            // Note: Implement proper password hashing here before saving!
            // Example: user.PasswordHash = HashPassword(user.PasswordPlainText);
            return _database.InsertAsync(user);
        }

        // --- Category Operations ---

        public Task<List<Category>> GetCategoriesAsync()
        {
            return _database.Table<Category>().ToListAsync();
        }

        public Task<Category> GetCategoryAsync(int id)
        {
            return _database.Table<Category>().FirstOrDefaultAsync(c => c.Id == id);
        }

        public Task<Category> GetCategoryByNameAsync(string name)
        {
            return _database.Table<Category>().FirstOrDefaultAsync(c => c.Name == name);
        }

        public Task<int> SaveCategoryAsync(Category category)
        {
            if (category.Id != 0)
            {
                return _database.UpdateAsync(category); // Update existing
            }
            else
            {
                return _database.InsertAsync(category); // Insert new
            }
        }

        public Task<int> DeleteCategoryAsync(Category category)
        {
            return _database.DeleteAsync(category);
        }


        // --- Expense Operations ---

        public Task<List<Expense>> GetExpensesAsync()
        {
            // Gets all expenses, ordered by date descending
            return _database.Table<Expense>().OrderByDescending(e => e.Date).ToListAsync();
        }

        public Task<List<Expense>> GetExpensesByPeriodAsync(DateTime startDate, DateTime endDate)
        {
            // Ensure endDate includes the whole day
            DateTime endOfDay = endDate.Date.AddDays(1).AddTicks(-1);
            return _database.Table<Expense>()
                            .Where(e => e.Date >= startDate.Date && e.Date <= endOfDay)
                            .OrderByDescending(e => e.Date)
                            .ToListAsync();
        }


        public Task<Expense> GetExpenseAsync(int id)
        {
            return _database.Table<Expense>().FirstOrDefaultAsync(e => e.Id == id);
        }

        public Task<int> SaveExpenseAsync(Expense expense)
        {
            if (expense.Id != 0)
            {
                return _database.UpdateAsync(expense);
            }
            else
            {
                return _database.InsertAsync(expense);
            }
        }

        public Task<int> DeleteExpenseAsync(Expense expense)
        {
            // if (File.Exists(expense.PhotoPath)) { File.Delete(expense.PhotoPath); }
            return _database.DeleteAsync(expense);
        }

        // --- Goal Operations ---

        public Task<Goal> GetGoalAsync(int year, int month)
        {
            return _database.Table<Goal>().FirstOrDefaultAsync(g => g.Year == year && g.Month == month);
        }

        public Task<int> SaveGoalAsync(Goal goal)
        {
            if (goal.Id != 0)
            {
                return _database.UpdateAsync(goal);
            }
            else
            {
                // Check if a goal for this month/year already exists before inserting
                return _database.InsertAsync(goal);
            }
        }

        // --- Aggregate Operations ---

        // Example: Get total spending per category for a period
        public async Task<Dictionary<string, decimal>> GetSpendingByCategoryAsync(DateTime startDate, DateTime endDate)
        {
            DateTime endOfDay = endDate.Date.AddDays(1).AddTicks(-1);
            var expenses = await _database.Table<Expense>()
                                         .Where(e => e.Date >= startDate.Date && e.Date <= endOfDay)
                                         .ToListAsync();

            var categories = await GetCategoriesAsync();
            var categoryDict = categories.ToDictionary(c => c.Id, c => c.Name); // For quick lookup

            var spending = expenses
                .GroupBy(e => e.CategoryId)
                .Select(g => new
                {
                    CategoryId = g.Key,
                    TotalAmount = g.Sum(e => e.Amount)
                })
                .ToDictionary(
                    item => categoryDict.ContainsKey(item.CategoryId) ? categoryDict[item.CategoryId] : "Uncategorized", // Get category name
                    item => item.TotalAmount
                );

            // Ensure all categories are present, even with zero spending
            foreach (var catName in categoryDict.Values)
            {
                if (!spending.ContainsKey(catName))
                {
                    spending.Add(catName, 0m);
                }
            }

            return spending;
        }
    }
}
