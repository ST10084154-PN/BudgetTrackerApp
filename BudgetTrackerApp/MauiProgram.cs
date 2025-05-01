using Microsoft.Extensions.Logging;
using BudgetTrackerApp.Services;
using BudgetTrackerApp.ViewModels;
using BudgetTrackerApp.Views;


namespace BudgetTrackerApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            // Register Database Service as Singleton (one instance)
            builder.Services.AddSingleton<DatabaseService>();

            // Register ViewModels
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<ExpenseListViewModel>();
            builder.Services.AddTransient<AddExpenseViewModel>();
            builder.Services.AddTransient<CategoryManagementViewModel>(); // Added
            builder.Services.AddTransient<GoalsViewModel>();          // Added
            builder.Services.AddTransient<ReportsViewModel>();        // Added
                                                                      // ... register other ViewModels

            // Register Pages/Views
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<ExpensesPage>();
            builder.Services.AddTransient<AddExpensePage>();
            builder.Services.AddTransient<CategoryManagementPage>(); // Added
            builder.Services.AddTransient<GoalsPage>();          // Added
            builder.Services.AddTransient<ReportsPage>();        // Added
                                                                 // ... register other Pages

            return builder.Build();
        }
    }
}
