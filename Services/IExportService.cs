using MyJournalProject.Common;
using MyJournalProject.Models;

namespace MyJournalProject.Services;

public interface IExportService
{
    Task<ServiceResult<ExportPreviewData>> GetExportPreviewAsync(int userId, ExportRequest request);
    Task<ServiceResult<byte[]>> GeneratePdfAsync(int userId, ExportRequest request);
    Task<ServiceResult<List<string>>> GetAvailableCategoriesAsync(int userId);
    Task<ServiceResult<List<string>>> GetAvailableTagsAsync(int userId);
}