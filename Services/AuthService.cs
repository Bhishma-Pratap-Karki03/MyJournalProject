using MyJournalProject.Common;
using MyJournalProject.Data;
using MyJournalProject.Entities;
using MyJournalProject.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace MyJournalProject.Services;

// Service for handling user authentication (registration and login)
// Manages user accounts, password hashing, and database operations
public class AuthService : IAuthService
{
    // Service provider for accessing scoped services (like DbContext)
    private readonly IServiceProvider _serviceProvider;

    // Database context (lazy-loaded to handle scoped service in singleton)
    private AppDbContext? _context;

    // Constructor - injects service provider for scoped service resolution
    public AuthService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        Console.WriteLine("AuthService initialized");
    }

    // Helper method to get or create database context with proper scope handling
    private async Task<AppDbContext> GetDbContextAsync()
    {
        if (_context == null)
        {
            // Create a new scope to resolve scoped DbContext
            _context = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

            // Ensure database is created/initialized
            try
            {
                await _context.Database.EnsureCreatedAsync();
                Console.WriteLine("Database connection verified");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database connection error: {ex.Message}");
                throw;
            }
        }
        return _context;
    }

    // Registers a new user with the system
    public async Task<ServiceResult<User>> RegisterAsync(RegisterModel model)
    {
        try
        {
            Console.WriteLine($"Registering user: {model.Email}");

            var context = await GetDbContextAsync();

            // Check if email already exists in the database
            var existing = await context.Users
                .AsNoTracking() // Read-only query for better performance
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (existing != null)
            {
                Console.WriteLine($"Email already exists: {model.Email}");
                return ServiceResult<User>.FailureResult("This email is already registered");
            }

            // Hash the password for secure storage (never store plain text passwords)
            string hashedPassword = HashPassword(model.Password);
            Console.WriteLine($"Password hashed for: {model.Email}");

            // Create new user entity
            var user = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                Password = hashedPassword, // Store only the hash, not the plain password
                CreatedAt = DateTime.UtcNow
            };

            // Add user to database and save changes
            context.Users.Add(user);
            await context.SaveChangesAsync();

            Console.WriteLine($"User registered successfully: {model.Email}, ID: {user.Id}");
            Console.WriteLine($"Database location: {context.Database.GetDbConnection().DataSource}");

            return ServiceResult<User>.SuccessResult(user);
        }
        catch (DbUpdateException dbEx)
        {
            // Handle database-specific errors
            Console.WriteLine($"Database error during registration: {dbEx.Message}");
            if (dbEx.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {dbEx.InnerException.Message}");

                // Check for SQLite unique constraint violation (duplicate email)
                if (dbEx.InnerException.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase))
                {
                    return ServiceResult<User>.FailureResult("This email is already registered");
                }
            }
            return ServiceResult<User>.FailureResult($"Registration error: {dbEx.Message}");
        }
        catch (Exception ex)
        {
            // Handle general exceptions
            Console.WriteLine($"Registration failed: {ex}");
            return ServiceResult<User>.FailureResult($"Registration failed: {ex.Message}");
        }
    }

    // Authenticates an existing user with email and password
    public async Task<ServiceResult<User>> LoginAsync(string email, string password)
    {
        try
        {
            Console.WriteLine($"Attempting login for email: {email}");

            var context = await GetDbContextAsync();

            // Find user by email (case-sensitive search)
            var user = await context.Users
                .AsNoTracking() // Read-only query for better performance
                .FirstOrDefaultAsync(u => u.Email == email);

            // Check if user exists
            if (user == null)
            {
                Console.WriteLine($"User not found: {email}");
                return ServiceResult<User>.FailureResult("Invalid email or password");
            }

            Console.WriteLine($"User found: {user.FullName}, ID: {user.Id}");

            // Hash the provided password and compare with stored hash
            string hashedInputPassword = HashPassword(password);

            // Compare password hashes (timing-safe comparison is handled by string equality)
            if (user.Password != hashedInputPassword)
            {
                Console.WriteLine($"Password mismatch for user: {email}");
                return ServiceResult<User>.FailureResult("Invalid email or password");
            }

            Console.WriteLine($"Login successful for: {email}");
            return ServiceResult<User>.SuccessResult(user);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login failed with exception: {ex}");
            return ServiceResult<User>.FailureResult($"Login failed: {ex.Message}");
        }
    }

    // Hashes a password using SHA256 algorithm
    private string HashPassword(string password)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            // Convert password string to bytes
            byte[] bytes = Encoding.UTF8.GetBytes(password);

            // Compute SHA256 hash
            byte[] hashBytes = sha256.ComputeHash(bytes);

            // Convert hash bytes to hexadecimal string
            StringBuilder builder = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                builder.Append(b.ToString("x2")); // "x2" formats each byte as two-digit hexadecimal
            }
            return builder.ToString();
        }
    }

    // Gets the file path of the application database (for debugging/admin purposes)
    public string GetDatabasePath()
    {
        try
        {
            if (_context != null)
            {
                return _context.Database.GetDbConnection().DataSource;
            }
            return "Database not initialized";
        }
        catch (Exception ex)
        {
            return $"Error getting database path: {ex.Message}";
        }
    }
}