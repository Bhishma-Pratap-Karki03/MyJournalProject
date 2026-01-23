using MyJournalProject.Common;
using MyJournalProject.Data;
using MyJournalProject.Entities;
using MyJournalProject.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace MyJournalProject.Services;

// Service for handling all journal-related operations
// Manages CRUD operations, validation, and business logic for journal entries
public class JournalService : IJournalService
{
    // Database context for data access
    private readonly AppDbContext _context;

    // Constructor - injects database context dependency
    public JournalService(AppDbContext context)
    {
        _context = context;
        Console.WriteLine("JournalService initialized with direct DbContext injection");
    }

    // Creates a new journal entry for a user
    public async Task<ServiceResult<Journal>> CreateJournalAsync(JournalModel model, int userId)
    {
        try
        {
            // Log creation attempt for debugging
            Console.WriteLine($"=== CREATE JOURNAL ===");
            Console.WriteLine($"User ID: {userId}");
            Console.WriteLine($"Date: {model.EntryDate:yyyy-MM-dd}");
            Console.WriteLine($"Title: '{model.Title}'");
            Console.WriteLine($"Primary Mood: '{model.PrimaryMood}'");
            Console.WriteLine($"Category: '{model.Category}'");
            Console.WriteLine($"Content length: {model.Content?.Length ?? 0} chars");

            // VALIDATION: Check if primary mood is valid (must be Positive, Negative, or Neutral)
            if (!IsValidPrimaryMood(model.PrimaryMood))
            {
                return ServiceResult<Journal>.FailureResult("Primary mood must be Positive, Negative, or Neutral");
            }

            // Check if journal already exists for this date (only one journal per day per user)
            var existingJournal = await _context.Journals
                .FirstOrDefaultAsync(j => j.UserId == userId && j.EntryDate.Date == model.EntryDate.Date);

            if (existingJournal != null)
            {
                return ServiceResult<Journal>.FailureResult("A journal entry already exists for this date.");
            }

            // Clean HTML content to remove image tags and base64 data
            var cleanedContent = CleanHtmlContent(model.Content);

            // Calculate word and character counts from cleaned content
            int wordCount = CountWords(StripHtmlTags(cleanedContent));
            int charCount = cleanedContent.Length;

            Console.WriteLine($"Calculated - Words: {wordCount}, Chars: {charCount}");

            // Create new Journal entity from model
            var journal = new Journal
            {
                UserId = userId,
                Title = model.Title,
                Content = cleanedContent,
                PrimaryMood = model.PrimaryMood,
                SecondaryMood1 = model.SecondaryMood1,
                SecondaryMood2 = model.SecondaryMood2,
                Category = model.Category,
                Tags = model.Tags,
                WordCount = wordCount,
                CharacterCount = charCount,
                EntryDate = model.EntryDate.Date,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Add to database and save changes
            _context.Journals.Add(journal);
            await _context.SaveChangesAsync();

            Console.WriteLine($"Journal created successfully: ID {journal.Id}");
            return ServiceResult<Journal>.SuccessResult(journal);
        }
        catch (DbUpdateException dbEx)
        {
            // Handle database-specific errors (like unique constraint violations)
            Console.WriteLine($"Database error during journal creation: {dbEx.Message}");
            Console.WriteLine($"Inner exception: {dbEx.InnerException?.Message}");

            // Check for unique constraint violation (duplicate date entry)
            if (dbEx.InnerException?.Message?.Contains("UNIQUE constraint failed") == true ||
                dbEx.InnerException?.Message?.Contains("unique constraint") == true)
            {
                return ServiceResult<Journal>.FailureResult("A journal entry already exists for this date.");
            }

            return ServiceResult<Journal>.FailureResult($"Database error: {dbEx.Message}");
        }
        catch (Exception ex)
        {
            // Handle general exceptions
            Console.WriteLine($"Journal creation failed: {ex}");
            return ServiceResult<Journal>.FailureResult($"Journal creation failed: {ex.Message}");
        }
    }

    // Updates an existing journal entry
    public async Task<ServiceResult<Journal>> UpdateJournalAsync(int journalId, JournalModel model, int userId)
    {
        try
        {
            Console.WriteLine($"=== UPDATE JOURNAL ===");
            Console.WriteLine($"Journal ID: {journalId}");
            Console.WriteLine($"User ID: {userId}");

            // VALIDATION: Check if primary mood is valid
            if (!IsValidPrimaryMood(model.PrimaryMood))
            {
                return ServiceResult<Journal>.FailureResult("Primary mood must be Positive, Negative, or Neutral");
            }

            // Find the journal to update (must belong to the user)
            var journal = await _context.Journals
                .FirstOrDefaultAsync(j => j.Id == journalId && j.UserId == userId);

            if (journal == null)
            {
                Console.WriteLine($"Journal not found: ID={journalId}, User={userId}");
                return ServiceResult<Journal>.FailureResult("Journal not found or you don't have permission to edit it.");
            }

            // Check if another journal exists for the new date (if date is being changed)
            if (journal.EntryDate.Date != model.EntryDate.Date)
            {
                var existingJournal = await _context.Journals
                    .FirstOrDefaultAsync(j => j.UserId == userId &&
                                             j.Id != journalId &&
                                             j.EntryDate.Date == model.EntryDate.Date);

                if (existingJournal != null)
                {
                    return ServiceResult<Journal>.FailureResult("A journal entry already exists for the selected date.");
                }
            }

            // Clean HTML content
            var cleanedContent = CleanHtmlContent(model.Content);

            // Calculate word and character count
            int wordCount = CountWords(StripHtmlTags(cleanedContent));
            int charCount = cleanedContent.Length;

            // Update journal properties with new values
            journal.Title = model.Title;
            journal.Content = cleanedContent;
            journal.PrimaryMood = model.PrimaryMood;
            journal.SecondaryMood1 = model.SecondaryMood1;
            journal.SecondaryMood2 = model.SecondaryMood2;
            journal.Category = model.Category;
            journal.Tags = model.Tags;
            journal.WordCount = wordCount;
            journal.CharacterCount = charCount;
            journal.EntryDate = model.EntryDate.Date;
            journal.UpdatedAt = DateTime.UtcNow;

            // Save changes to database
            await _context.SaveChangesAsync();

            Console.WriteLine($"Journal updated successfully: ID {journal.Id}");
            return ServiceResult<Journal>.SuccessResult(journal);
        }
        catch (DbUpdateException dbEx)
        {
            Console.WriteLine($"Database error during journal update: {dbEx.Message}");
            return ServiceResult<Journal>.FailureResult($"Database error: {dbEx.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Journal update failed: {ex}");
            return ServiceResult<Journal>.FailureResult($"Journal update failed: {ex.Message}");
        }
    }

    // Gets a specific journal entry by ID
    public async Task<ServiceResult<Journal>> GetJournalAsync(int journalId, int userId)
    {
        try
        {
            // Find journal by ID and user ID (ensures user can only access their own journals)
            var journal = await _context.Journals
                .FirstOrDefaultAsync(j => j.Id == journalId && j.UserId == userId);

            if (journal == null)
            {
                return ServiceResult<Journal>.FailureResult("Journal not found.");
            }

            return ServiceResult<Journal>.SuccessResult(journal);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting journal: {ex}");
            return ServiceResult<Journal>.FailureResult($"Error getting journal: {ex.Message}");
        }
    }

    // Gets a journal entry by specific date
    public async Task<ServiceResult<Journal>> GetJournalByDateAsync(DateTime date, int userId)
    {
        try
        {
            // Find journal by date and user ID
            var journal = await _context.Journals
                .FirstOrDefaultAsync(j => j.UserId == userId && j.EntryDate.Date == date.Date);

            if (journal == null)
            {
                return ServiceResult<Journal>.FailureResult("No journal found for this date.");
            }

            return ServiceResult<Journal>.SuccessResult(journal);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting journal by date: {ex}");
            return ServiceResult<Journal>.FailureResult($"Error getting journal: {ex.Message}");
        }
    }

    // Gets all journal entries for a user, sorted by date (newest first)
    public async Task<ServiceResult<List<Journal>>> GetUserJournalsAsync(int userId)
    {
        try
        {
            var journals = await _context.Journals
                .Where(j => j.UserId == userId)
                .OrderByDescending(j => j.EntryDate) // Most recent first
                .ToListAsync();

            return ServiceResult<List<Journal>>.SuccessResult(journals);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting user journals: {ex}");
            return ServiceResult<List<Journal>>.FailureResult($"Error getting journals: {ex.Message}");
        }
    }

    // Deletes a journal entry
    public async Task<ServiceResult<bool>> DeleteJournalAsync(int journalId, int userId)
    {
        try
        {
            // Find journal to delete (must belong to the user)
            var journal = await _context.Journals
                .FirstOrDefaultAsync(j => j.Id == journalId && j.UserId == userId);

            if (journal == null)
            {
                return ServiceResult<bool>.FailureResult("Journal not found or you don't have permission to delete it.");
            }

            // Remove from database and save changes
            _context.Journals.Remove(journal);
            await _context.SaveChangesAsync();

            Console.WriteLine($"Journal deleted successfully: ID {journalId}");
            return ServiceResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting journal: {ex}");
            return ServiceResult<bool>.FailureResult($"Error deleting journal: {ex.Message}");
        }
    }

    // Checks if a journal exists for a specific date
    public async Task<ServiceResult<bool>> CheckJournalExistsAsync(DateTime date, int userId)
    {
        try
        {
            var exists = await _context.Journals
                .AnyAsync(j => j.UserId == userId && j.EntryDate.Date == date.Date);

            Console.WriteLine($"CheckJournalExists - Date: {date:yyyy-MM-dd}, User: {userId}, Exists: {exists}");
            return ServiceResult<bool>.SuccessResult(exists);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking journal existence: {ex}");
            return ServiceResult<bool>.FailureResult($"Error checking journal: {ex.Message}");
        }
    }

    // Gets all unique categories used by a user
    public async Task<ServiceResult<List<string>>> GetUserCategoriesAsync(int userId)
    {
        try
        {
            var categories = await _context.Journals
                .Where(j => j.UserId == userId)
                .Select(j => j.Category)
                .Distinct() // Get only unique categories
                .OrderBy(c => c) // Sort alphabetically
                .ToListAsync();

            return ServiceResult<List<string>>.SuccessResult(categories);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting user categories: {ex}");
            return ServiceResult<List<string>>.FailureResult($"Error getting categories: {ex.Message}");
        }
    }

    // Gets all unique tags used by a user
    public async Task<ServiceResult<List<string>>> GetUserTagsAsync(int userId)
    {
        try
        {
            // Get all tags JSON strings from user's journals
            var allTagsJson = await _context.Journals
                .Where(j => j.UserId == userId)
                .Select(j => j.TagsJson)
                .ToListAsync();

            var allTags = new List<string>();

            // Deserialize each JSON string and collect all tags
            foreach (var tagsJson in allTagsJson)
            {
                var tags = System.Text.Json.JsonSerializer.Deserialize<List<string>>(tagsJson ?? "[]");
                if (tags != null)
                {
                    allTags.AddRange(tags);
                }
            }

            // Get unique tags and sort them
            var uniqueTags = allTags
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            return ServiceResult<List<string>>.SuccessResult(uniqueTags);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting user tags: {ex}");
            return ServiceResult<List<string>>.FailureResult($"Error getting tags: {ex.Message}");
        }
    }

    // Gets all unique moods used by a user (primary, secondary1, and secondary2)
    public async Task<ServiceResult<List<string>>> GetUserMoodsAsync(int userId)
    {
        try
        {
            // Get distinct primary moods
            var primaryMoods = await _context.Journals
                .Where(j => j.UserId == userId)
                .Select(j => j.PrimaryMood)
                .Distinct()
                .ToListAsync();

            // Get distinct secondary mood 1 values (excluding nulls)
            var secondaryMoods1 = await _context.Journals
                .Where(j => j.UserId == userId && j.SecondaryMood1 != null)
                .Select(j => j.SecondaryMood1!)
                .Distinct()
                .ToListAsync();

            // Get distinct secondary mood 2 values (excluding nulls)
            var secondaryMoods2 = await _context.Journals
                .Where(j => j.UserId == userId && j.SecondaryMood2 != null)
                .Select(j => j.SecondaryMood2!)
                .Distinct()
                .ToListAsync();

            // Combine all moods, remove duplicates, and sort
            var allMoods = primaryMoods
                .Concat(secondaryMoods1)
                .Concat(secondaryMoods2)
                .Distinct()
                .OrderBy(m => m)
                .ToList();

            return ServiceResult<List<string>>.SuccessResult(allMoods);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting user moods: {ex}");
            return ServiceResult<List<string>>.FailureResult($"Error getting moods: {ex.Message}");
        }
    }


    private string CleanHtmlContent(string html)
    {
        if (string.IsNullOrEmpty(html))
            return html;

        var cleaned = Regex.Replace(html, @"<img[^>]*>", "", RegexOptions.IgnoreCase);

        cleaned = Regex.Replace(cleaned, @"data:image/[^;]+;base64,[^""']+", "", RegexOptions.IgnoreCase);

        return cleaned;
    }

    // Helper method to strip HTML tags for word counting
    private string StripHtmlTags(string html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        // Remove HTML tags, replacing them with spaces
        return Regex.Replace(html, "<.*?>", " ");
    }

    // Helper method to count words in text
    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        // Clean up extra whitespace and count words
        var cleanText = Regex.Replace(text, "\\s+", " ").Trim();
        return cleanText == "" ? 0 : cleanText.Split(' ').Length;
    }

    // Helper method to validate primary mood (must be one of three allowed values)
    private bool IsValidPrimaryMood(string mood)
    {
        return mood == "Positive" || mood == "Negative" || mood == "Neutral";
    }
}