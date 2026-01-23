using MyJournalProject.Common;
using MyJournalProject.Entities;
using MyJournalProject.Models;

namespace MyJournalProject.Services;

public interface IAuthService
{
    // Registers a new user in the system
    // Parameters:
    // model: Registration data containing user information
    // Returns: ServiceResult containing the created User entity if successful
    Task<ServiceResult<User>> RegisterAsync(RegisterModel model);

    // Authenticates an existing user with email and password
    // Parameters:
    // email: User's email address
    // password: User's password for authentication
    // Returns: ServiceResult containing the authenticated User entity if successful
    Task<ServiceResult<User>> LoginAsync(string email, string password);

    // Gets the file path of the application database
    // Returns: String containing the full path to the database file
    string GetDatabasePath();
}