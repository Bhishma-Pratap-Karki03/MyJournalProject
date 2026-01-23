using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyJournalProject.Data;
using MyJournalProject.Services;
using Microsoft.Maui.Storage;

namespace MyJournalProject;

// This class is the entry point for configuring the MAUI application
// It sets up services, database, and application initialization
public static class MauiProgram
{
    // Main method to create and configure the MAUI application
    public static MauiApp CreateMauiApp()
    {
        // Create a new MAUI app builder
        var builder = MauiApp.CreateBuilder();

        // Configure the main app and fonts
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                // Add custom font for the application
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Add Blazor WebView support for hybrid MAUI/Blazor app
        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        // Enable developer tools and debug logging in debug mode
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // Configure SQLite database connection and setup
        ConfigureDatabase(builder.Services);

        // Register application services with dependency injection
        // Note: ImageUploadService is intentionally not registered
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IJournalService, JournalService>();

        // Register dashboard service
        builder.Services.AddScoped<IDashboardService, DashboardService>();

        // Register export service
        builder.Services.AddScoped<IExportService, ExportService>();

        // Register theme service for managing application themes
        builder.Services.AddScoped<IThemeService, ThemeService>();

        // Build the application with all configured services
        var app = builder.Build();

        // Initialize database on application startup
        InitializeDatabase(app.Services);

        // Return the fully configured application
        return app;
    }

    // Configures the SQLite database connection and registers DbContext
    private static void ConfigureDatabase(IServiceCollection services)
    {
        string localFolderPath;

        // Handle different platform-specific paths for database storage
        // Windows uses LocalApplicationData folder
        if (DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            localFolderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MyJournalProject");
        }
        else
        {
            // Other platforms (iOS, Android, macOS) use app data directory
            localFolderPath = FileSystem.AppDataDirectory;
        }

        // Ensure the directory exists before creating database
        if (!Directory.Exists(localFolderPath))
        {
            Directory.CreateDirectory(localFolderPath);
        }

        // Create full database file path
        var databasePath = Path.Combine(localFolderPath, "myjournal.db");
        Console.WriteLine($"Database will be created at: {databasePath}");

        // Register AppDbContext with SQLite configuration
        services.AddDbContext<AppDbContext>(options =>
        {
            // Configure SQLite connection string
            options.UseSqlite($"Data Source={databasePath};Cache=Shared");

#if DEBUG
            // Enable detailed logging and error information in debug mode
            options.EnableSensitiveDataLogging()
                   .EnableDetailedErrors()
                   .LogTo(Console.WriteLine, LogLevel.Information);
#endif
        }, ServiceLifetime.Scoped, ServiceLifetime.Scoped);
    }

    // Initializes the database on application startup
    private static void InitializeDatabase(IServiceProvider serviceProvider)
    {
        try
        {
            // Create a scope for database initialization
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Console.WriteLine("DATABASE INITIALIZATION");

            // Get and display database path for debugging
            var dbPath = dbContext.Database.GetDbConnection().DataSource;
            Console.WriteLine($"Database location: {dbPath}");

            // Check if database file already exists
            bool dbFileExists = File.Exists(dbPath);
            Console.WriteLine($"Database file exists: {dbFileExists}");

            // Ensure database is created with all necessary tables
            Console.WriteLine("Ensuring database is created with all tables...");
            dbContext.Database.EnsureCreated();
            Console.WriteLine("Database ensured created.");

            // Verify that all required tables exist by attempting to query them
            Console.WriteLine("Verifying tables exist...");

            try
            {
                // Check Users table existence
                var userCount = dbContext.Users.Count();
                Console.WriteLine($"Users table exists with {userCount} records");
            }
            catch (Exception ex)
            {
                // If Users table doesn't exist, recreate the entire database
                Console.WriteLine($"Users table error: {ex.Message}");
                RecreateDatabase(dbContext, dbPath);
                return;
            }

            try
            {
                // Check Journals table existence
                var journalCount = dbContext.Journals.Count();
                Console.WriteLine($"Journals table exists with {journalCount} records");
            }
            catch (Exception ex)
            {
                // If Journals table doesn't exist, recreate the entire database
                Console.WriteLine($"Journals table error: {ex.Message}");
                Console.WriteLine("Journals table missing, recreating database...");
                RecreateDatabase(dbContext, dbPath);
            }

            Console.WriteLine("DATABASE INITIALIZATION COMPLETE");
        }
        catch (Exception ex)
        {
            // Log any errors during database initialization
            Console.WriteLine($"Error initializing database: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    // Recreates the database from scratch (used when tables are missing or corrupted)
    private static void RecreateDatabase(AppDbContext dbContext, string dbPath)
    {
        try
        {
            Console.WriteLine("Recreating database...");

            // Close any existing database connection
            dbContext.Database.CloseConnection();

            // Delete the existing database file if it exists
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
                Console.WriteLine("Old database file deleted");
            }

            // Create a fresh database with all tables
            dbContext.Database.EnsureCreated();
            Console.WriteLine("New database created with all tables");

            // Verify the new database structure by listing all tables
            var connection = dbContext.Database.GetDbConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            // SQL query to get all table names from SQLite
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
            // Log any errors during database recreation
            Console.WriteLine($"Error recreating database: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}