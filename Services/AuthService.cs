using MyJournalProject.Common;
using MyJournalProject.Data;
using MyJournalProject.Entities;
using MyJournalProject.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace MyJournalProject.Services;

public class AuthService : IAuthService
{
    private readonly IServiceProvider _serviceProvider;
    private AppDbContext? _context;

    public AuthService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        Console.WriteLine("AuthService initialized");
    }

    private async Task<AppDbContext> GetDbContextAsync()
    {
        if (_context == null)
        {
            _context = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

            // Ensure database is created
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

    public async Task<ServiceResult<User>> RegisterAsync(RegisterModel model)
    {
        try
        {
            Console.WriteLine($"Registering user: {model.Email}");

            var context = await GetDbContextAsync();

            // Check if email already exists
            var existing = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (existing != null)
            {
                Console.WriteLine($"Email already exists: {model.Email}");
                return ServiceResult<User>.FailureResult("This email is already registered");
            }

            // Hash the password using SHA256
            string hashedPassword = HashPassword(model.Password);
            Console.WriteLine($"Password hashed for: {model.Email}");

            // Create user with hashed password and timestamp
            var user = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                Password = hashedPassword,
                CreatedAt = DateTime.UtcNow
            };

            // Save to database
            context.Users.Add(user);
            await context.SaveChangesAsync();

            Console.WriteLine($"User registered successfully: {model.Email}, ID: {user.Id}");
            Console.WriteLine($"Database location: {context.Database.GetDbConnection().DataSource}");

            return ServiceResult<User>.SuccessResult(user);
        }
        catch (DbUpdateException dbEx)
        {
            Console.WriteLine($"Database error during registration: {dbEx.Message}");
            if (dbEx.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {dbEx.InnerException.Message}");

                // Check for SQLite unique constraint
                if (dbEx.InnerException.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase))
                {
                    return ServiceResult<User>.FailureResult("This email is already registered");
                }
            }
            return ServiceResult<User>.FailureResult($"Registration error: {dbEx.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Registration failed: {ex}");
            return ServiceResult<User>.FailureResult($"Registration failed: {ex.Message}");
        }
    }

    public async Task<ServiceResult<User>> LoginAsync(string email, string password)
    {
        try
        {
            Console.WriteLine($"Attempting login for email: {email}");

            var context = await GetDbContextAsync();

            // Find user by email
            var user = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                Console.WriteLine($"User not found: {email}");
                return ServiceResult<User>.FailureResult("Invalid email or password");
            }

            Console.WriteLine($"User found: {user.FullName}, ID: {user.Id}");

            // Hash the provided password and compare with stored hash
            string hashedInputPassword = HashPassword(password);

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

    private string HashPassword(string password)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(password);
            byte[] hashBytes = sha256.ComputeHash(bytes);

            StringBuilder builder = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
    }

    // Helper method to get database info
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