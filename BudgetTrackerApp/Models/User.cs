using SQLite; // Namespace for SQLite-net-pcl attributes

namespace BudgetTrackerApp.Models
{
    // Represents a user account
    [Table("Users")] // Maps this class to the 'Users' table in SQLite
    public class User
    {
        [PrimaryKey, AutoIncrement] // Defines Id as the primary key, auto-generated
        public int Id { get; set; }

        [Unique] // Ensures usernames are unique
        public string Username { get; set; }

        public string PasswordHash { get; set; } // Store a hash
    }

    // Represents an expense/budget category
    [Table("Categories")]
    public class Category
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Unique]
        public string Name { get; set; }
    }

    // Represents a single expense entry
    [Table("Expenses")]
    public class Expense
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; } // Use TimeSpan for time of day
        public TimeSpan EndTime { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; } // Use decimal for currency

        [Indexed] // Indexing CategoryId can speed up queries filtering by category
        public int CategoryId { get; set; } // Foreign key to the Category table

        public string PhotoPath { get; set; } // Path to the stored photo file 

    }

    // Represents monthly budget goals 
    [Table("Goals")]
    public class Goal
    {
        [PrimaryKey, AutoIncrement]
        public int UserId { get; set; }
        public int Id { get; set; }

        public int Year { get; set; }
        public int Month { get; set; } // 1-12

        public decimal MinSpendingGoal { get; set; }
        public decimal MaxSpendingGoal { get; set; }

    }
}
