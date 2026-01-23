using MyJournalProject.Common;
using MyJournalProject.Entities;
using MyJournalProject.Models;

namespace MyJournalProject.Services;

public interface IJournalService
{
    // Creates a new journal entry for a user
    // Parameters:
    // model: Journal data from the UI/form
    // userId: ID of the user creating the journal
    // Returns: ServiceResult containing the created Journal entity
    Task<ServiceResult<Journal>> CreateJournalAsync(JournalModel model, int userId);

    // Updates an existing journal entry
    // Parameters:
    // journalId: ID of the journal to update
    // model: Updated journal data
    // userId: ID of the user making the update
    // Returns: ServiceResult containing the updated Journal entity
    Task<ServiceResult<Journal>> UpdateJournalAsync(int journalId, JournalModel model, int userId);

    // Gets a specific journal entry by ID
    // Parameters:
    // journalId: ID of the journal to retrieve
    // userId: ID of the user requesting the journal
    // Returns: ServiceResult containing the Journal entity
    Task<ServiceResult<Journal>> GetJournalAsync(int journalId, int userId);

    // Gets a journal entry by specific date
    // Parameters:
    // date: Date to search for journal entry
    // userId: ID of the user requesting the journal
    // Returns: ServiceResult containing the Journal entity
    Task<ServiceResult<Journal>> GetJournalByDateAsync(DateTime date, int userId);

    // Gets all journal entries for a specific user
    // Parameters:
    // userId: ID of the user whose journals to retrieve
    // Returns: ServiceResult containing a list of Journal entities
    Task<ServiceResult<List<Journal>>> GetUserJournalsAsync(int userId);

    // Deletes a journal entry
    // Parameters:
    // journalId: ID of the journal to delete
    // userId: ID of the user requesting deletion (for authorization)
    // Returns: ServiceResult indicating success or failure
    Task<ServiceResult<bool>> DeleteJournalAsync(int journalId, int userId);

    // Checks if a journal exists for a specific date
    // Parameters:
    // date: Date to check
    // userId: ID of the user to check for
    // Returns: ServiceResult indicating whether a journal exists for that date
    Task<ServiceResult<bool>> CheckJournalExistsAsync(DateTime date, int userId);

    // Gets all unique categories used by a user
    // Parameters:
    // userId: ID of the user whose categories to retrieve
    // Returns: ServiceResult containing a list of unique category names
    Task<ServiceResult<List<string>>> GetUserCategoriesAsync(int userId);

    // Gets all unique tags used by a user
    // Parameters:
    // userId: ID of the user whose tags to retrieve
    // Returns: ServiceResult containing a list of unique tag names
    Task<ServiceResult<List<string>>> GetUserTagsAsync(int userId);

    // Gets all unique moods used by a user (primary and secondary)
    // Parameters:
    // userId: ID of the user whose moods to retrieve
    // Returns: ServiceResult containing a list of unique mood names
    Task<ServiceResult<List<string>>> GetUserMoodsAsync(int userId);
}