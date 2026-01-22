using MyJournalProject.Common;
using MyJournalProject.Entities;
using MyJournalProject.Models;

namespace MyJournalProject.Services;

public interface IJournalService
{
    Task<ServiceResult<Journal>> CreateJournalAsync(JournalModel model, int userId);
    Task<ServiceResult<Journal>> UpdateJournalAsync(int journalId, JournalModel model, int userId);
    Task<ServiceResult<Journal>> GetJournalAsync(int journalId, int userId);
    Task<ServiceResult<Journal>> GetJournalByDateAsync(DateTime date, int userId);
    Task<ServiceResult<List<Journal>>> GetUserJournalsAsync(int userId);
    Task<ServiceResult<bool>> DeleteJournalAsync(int journalId, int userId);
    Task<ServiceResult<bool>> CheckJournalExistsAsync(DateTime date, int userId);
    Task<ServiceResult<List<string>>> GetUserCategoriesAsync(int userId);
    Task<ServiceResult<List<string>>> GetUserTagsAsync(int userId);
    Task<ServiceResult<List<string>>> GetUserMoodsAsync(int userId);
}