using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyJournalProject.Data;
using MyJournalProject.Services;
using Microsoft.Maui.Storage;

namespace MyJournalProject;

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
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // Configure SQLite database
        ConfigureDatabase(builder.Services);

        // Register Services
        builder.Services.AddScoped<IAuthService, AuthService>();

        // Build the app
        var app = builder.Build();

        // Initialize database on app start
        InitializeDatabase(app.Services);

        return app;
    }

    private static void ConfigureDatabase(IServiceCollection services)
    {
        // Get the database path in Local folder
        string localFolderPath;

        // For Windows
        if (DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            localFolderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MyJournalProject");
        }
        else
        {
            // For other platforms, use app data directory
            localFolderPath = FileSystem.AppDataDirectory;
        }

        // Create directory if it doesn't exist
        if (!Directory.Exists(localFolderPath))
        {
            Directory.CreateDirectory(localFolderPath);
        }

        var databasePath = Path.Combine(localFolderPath, "myjournal.db");
        Console.WriteLine($"Database will be created at: {databasePath}");

        // Register DbContext
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlite($"Data Source={databasePath};Cache=Shared");

#if DEBUG
            options.EnableSensitiveDataLogging()
                   .EnableDetailedErrors()
                   .LogTo(Console.WriteLine, LogLevel.Information);
#endif
        }, ServiceLifetime.Scoped, ServiceLifetime.Scoped);
    }

    private static void InitializeDatabase(IServiceProvider serviceProvider)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Apply migrations or create database if it doesn't exist
            dbContext.Database.EnsureCreated();

            Console.WriteLine("Database initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing database: {ex.Message}");
        }
    }
}