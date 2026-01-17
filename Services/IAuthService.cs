using MyJournalProject.Common;
using MyJournalProject.Entities;
using MyJournalProject.Models;

namespace MyJournalProject.Services;

public interface IAuthService
{
    Task<ServiceResult<User>> RegisterAsync(RegisterModel model);
    Task<ServiceResult<User>> LoginAsync(string email, string password);
    string GetDatabasePath(); 
}