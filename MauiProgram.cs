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

        // Register Services WITHOUT ImageUploadService
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IJournalService, JournalService>();
        // In ConfigureServices method of MauiProgram.cs
        builder.Services.AddScoped<IDashboardService, DashboardService>();
        builder.Services.AddScoped<IExportService, ExportService>();

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

            Console.WriteLine("=== DATABASE INITIALIZATION ===");

            // Get database path
            var dbPath = dbContext.Database.GetDbConnection().DataSource;
            Console.WriteLine($"Database location: {dbPath}");

            // Check if database file exists
            bool dbFileExists = File.Exists(dbPath);
            Console.WriteLine($"Database file exists: {dbFileExists}");

            // Always ensure database is created/updated
            Console.WriteLine("Ensuring database is created with all tables...");
            dbContext.Database.EnsureCreated();

            Console.WriteLine("Database ensured created.");

            // Verify all tables exist by trying to query them
            Console.WriteLine("Verifying tables exist...");

            try
            {
                // Check Users table
                var userCount = dbContext.Users.Count();
                Console.WriteLine($"✓ Users table exists with {userCount} records");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Users table error: {ex.Message}");
                // If Users table doesn't exist, recreate database
                RecreateDatabase(dbContext, dbPath);
                return;
            }

            try
            {
                // Check Journals table
                var journalCount = dbContext.Journals.Count();
                Console.WriteLine($"✓ Journals table exists with {journalCount} records");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Journals table error: {ex.Message}");
                Console.WriteLine("Journals table missing, recreating database...");
                RecreateDatabase(dbContext, dbPath);
            }

            Console.WriteLine("=== DATABASE INITIALIZATION COMPLETE ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing database: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private static void RecreateDatabase(AppDbContext dbContext, string dbPath)
    {
        try
        {
            Console.WriteLine("Recreating database...");

            // Close existing connection
            dbContext.Database.CloseConnection();

            // Delete database file if it exists
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
                Console.WriteLine("Old database file deleted");
            }

            // Create fresh database
            dbContext.Database.EnsureCreated();
            Console.WriteLine("New database created with all tables");

            // Verify tables
            var connection = dbContext.Database.GetDbConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";
            using var reader = command.ExecuteReader();

            Console.WriteLine("Tables in new database:");
            while (reader.Read())
            {
                Console.WriteLine($"- {reader.GetString(0)}");
            }

            connection.Close();

            Console.WriteLine("Database recreation successful!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error recreating database: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}