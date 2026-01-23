using MyJournalProject.Common;
using MyJournalProject.Models;

namespace MyJournalProject.Services;

public interface IExportService
{
    // Generates preview data for export without creating the actual file
    // Useful for showing users what will be included in the export
    // Parameters:
    // userId: ID of the user requesting the export
    // request: Export configuration (date range, filters, etc.)
    // Returns: ServiceResult containing ExportPreviewData with summary information
    Task<ServiceResult<ExportPreviewData>> GetExportPreviewAsync(int userId, ExportRequest request);

    // Generates a PDF document containing the user's journal entries
    // Parameters:
    // userId: ID of the user requesting the PDF
    // request: Export configuration (date range, filters, format options)
    // Returns: ServiceResult containing the PDF file as a byte array
    Task<ServiceResult<byte[]>> GeneratePdfAsync(int userId, ExportRequest request);

    // Gets all unique categories available for a user's journal entries
    // Useful for providing filter options in the export UI
    // Parameters:
    // userId: ID of the user whose categories to retrieve
    // Returns: ServiceResult containing a list of unique category names
    Task<ServiceResult<List<string>>> GetAvailableCategoriesAsync(int userId);

    // Gets all unique tags available for a user's journal entries
    // Useful for providing filter options in the export UI
    // Parameters:
    // userId: ID of the user whose tags to retrieve
    // Returns: ServiceResult containing a list of unique tag names
    Task<ServiceResult<List<string>>> GetAvailableTagsAsync(int userId);
}