using MyJournalProject.Common;
using MyJournalProject.Data;
using MyJournalProject.Entities;
using MyJournalProject.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text.RegularExpressions;

using iText = iTextSharp.text;
using iTextPdf = iTextSharp.text.pdf;

namespace MyJournalProject.Services;

// Service for exporting journal entries (currently PDF)
// Handles filtering, formatting, and PDF generation for user's journal data
public class ExportService : IExportService
{
    // Database context for accessing journal data
    private readonly AppDbContext _context;

    // Journal service for additional journal operations
    private readonly IJournalService _journalService;

    // Constructor - injects database context and journal service dependencies
    public ExportService(AppDbContext context, IJournalService journalService)
    {
        _context = context;
        _journalService = journalService;
    }

    // Helper method to retrieve journals based on export filters
    public async Task<ServiceResult<List<Journal>>> GetJournalsForExportAsync(int userId, ExportFilterModel filters)
    {
        try
        {
            // Start with base query for the user's journals
            var query = _context.Journals
                .Where(j => j.UserId == userId)
                .AsQueryable();

            // Apply date range filter
            if (filters.FromDate.HasValue)
            {
                query = query.Where(j => j.EntryDate >= filters.FromDate.Value.Date);
            }

            if (filters.ToDate.HasValue)
            {
                query = query.Where(j => j.EntryDate <= filters.ToDate.Value.Date);
            }

            // Apply primary mood filter
            if (!string.IsNullOrEmpty(filters.PrimaryMood))
            {
                query = query.Where(j => j.PrimaryMood == filters.PrimaryMood);
            }

            // Apply category filter
            if (!string.IsNullOrEmpty(filters.Category))
            {
                query = query.Where(j => j.Category == filters.Category);
            }

            // Apply tags filter - check if journal contains any of the selected tags
            if (filters.Tags != null && filters.Tags.Any())
            {
                foreach (var tag in filters.Tags)
                {
                    query = query.Where(j => j.Tags.Contains(tag));
                }
            }

            // Execute query and return results ordered by date (newest first)
            var journals = await query
                .OrderByDescending(j => j.EntryDate)
                .ToListAsync();

            return ServiceResult<List<Journal>>.SuccessResult(journals);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting journals for export: {ex.Message}");
            return ServiceResult<List<Journal>>.FailureResult($"Error getting journals for export: {ex.Message}");
        }
    }

    // Generates preview data for export without creating the actual PDF file
    public async Task<ServiceResult<ExportPreviewData>> GetExportPreviewAsync(int userId, ExportRequest request)
    {
        try
        {
            Console.WriteLine($"Getting export preview for user {userId}");
            Console.WriteLine($"Filters: FromDate={request.Filters.FromDate}, ToDate={request.Filters.ToDate}, PrimaryMood={request.Filters.PrimaryMood}, Category={request.Filters.Category}");
            Console.WriteLine($"Tags count: {request.Filters.Tags?.Count ?? 0}");

            // Get journals based on filters
            var journalsResult = await GetJournalsForExportAsync(userId, request.Filters);

            if (!journalsResult.Success)
            {
                return ServiceResult<ExportPreviewData>.FailureResult(journalsResult.ErrorMessage);
            }

            var journals = journalsResult.Data;
            Console.WriteLine($"Found {journals.Count} journals for preview");

            // Create preview data object
            var preview = new ExportPreviewData
            {
                EntryCount = journals.Count,
                TotalWordCount = journals.Sum(j => j.WordCount)
            };

            // Set date range for display
            if (journals.Any())
            {
                // Use actual dates from filtered journals
                var minDate = journals.Min(j => j.EntryDate);
                var maxDate = journals.Max(j => j.EntryDate);
                preview.DateRange = $"{minDate:MM/dd/yyyy} - {maxDate:MM/dd/yyyy}";
            }
            else
            {
                // No journals found, use filter dates if provided
                if (request.Filters.FromDate.HasValue && request.Filters.ToDate.HasValue)
                {
                    preview.DateRange = $"{request.Filters.FromDate.Value:MM/dd/yyyy} - {request.Filters.ToDate.Value:MM/dd/yyyy}";
                }
                else if (request.Filters.FromDate.HasValue)
                {
                    preview.DateRange = $"From {request.Filters.FromDate.Value:MM/dd/yyyy}";
                }
                else if (request.Filters.ToDate.HasValue)
                {
                    preview.DateRange = $"To {request.Filters.ToDate.Value:MM/dd/yyyy}";
                }
                else
                {
                    preview.DateRange = "All dates";
                }
            }

            // Build list of applied filters for display
            var filters = new List<string>();

            if (!string.IsNullOrEmpty(request.Filters.PrimaryMood))
                filters.Add($"Primary Mood: {request.Filters.PrimaryMood}");

            if (!string.IsNullOrEmpty(request.Filters.Category))
                filters.Add($"Category: {request.Filters.Category}");

            if (request.Filters.Tags != null && request.Filters.Tags.Any())
                filters.Add($"Tags: {string.Join(", ", request.Filters.Tags.Select(t => $"#{t}"))}");

            if (filters.Count == 0)
                filters.Add("No filters applied");

            preview.AppliedFilters = filters;

            Console.WriteLine($"Preview data - Entries: {preview.EntryCount}, Word Count: {preview.TotalWordCount}");
            return ServiceResult<ExportPreviewData>.SuccessResult(preview);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating preview: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return ServiceResult<ExportPreviewData>.FailureResult($"Error generating preview: {ex.Message}");
        }
    }

    // Generates a PDF document containing filtered journal entries
    public async Task<ServiceResult<byte[]>> GeneratePdfAsync(int userId, ExportRequest request)
    {
        try
        {
            Console.WriteLine($"Generating PDF for user {userId}");

            // Get journals based on filters
            var journalsResult = await GetJournalsForExportAsync(userId, request.Filters);

            if (!journalsResult.Success)
            {
                return ServiceResult<byte[]>.FailureResult(journalsResult.ErrorMessage);
            }

            var journals = journalsResult.Data;

            // Check if any journals were found
            if (!journals.Any())
            {
                return ServiceResult<byte[]>.FailureResult("No journals found for the selected filters");
            }

            // Create PDF document in memory stream
            using var memoryStream = new MemoryStream();
            var document = new iText.Document(iText.PageSize.A4, 50, 50, 50, 50); // A4 page with margins
            var writer = iTextPdf.PdfWriter.GetInstance(document, memoryStream);

            document.Open();

            // Add title to PDF
            var titleFont = iText.FontFactory.GetFont(iText.FontFactory.HELVETICA_BOLD, 24, iText.BaseColor.BLACK);
            var title = new iText.Paragraph("My Journal Entries", titleFont)
            {
                Alignment = iText.Element.ALIGN_CENTER
            };
            title.SpacingAfter = 20f;
            document.Add(title);

            // Add export date/time information
            var infoFont = iText.FontFactory.GetFont(iText.FontFactory.HELVETICA, 10, iText.BaseColor.DARK_GRAY);
            var info = new iText.Paragraph($"Exported on: {DateTime.Now:MMMM dd, yyyy hh:mm tt}", infoFont)
            {
                Alignment = iText.Element.ALIGN_RIGHT
            };
            info.SpacingAfter = 10f;
            document.Add(info);

            // Add summary statistics
            var summaryFont = iText.FontFactory.GetFont(iText.FontFactory.HELVETICA, 11, iText.BaseColor.BLACK);
            var summary = new iText.Paragraph($"Total Entries: {journals.Count} | Total Words: {journals.Sum(j => j.WordCount):N0}", summaryFont)
            {
                Alignment = iText.Element.ALIGN_CENTER
            };
            summary.SpacingAfter = 15f;
            document.Add(summary);

            // Add filters summary section if any filters were applied
            if (request.Filters.FromDate.HasValue || request.Filters.ToDate.HasValue ||
                !string.IsNullOrEmpty(request.Filters.PrimaryMood) ||
                !string.IsNullOrEmpty(request.Filters.Category) ||
                (request.Filters.Tags != null && request.Filters.Tags.Any()))
            {
                var filterTitle = new iText.Paragraph("Applied Filters:",
                    iText.FontFactory.GetFont(iText.FontFactory.HELVETICA_BOLD, 12));
                filterTitle.SpacingBefore = 10f;
                filterTitle.SpacingAfter = 5f;
                document.Add(filterTitle);

                var filterText = new List<string>();

                if (request.Filters.FromDate.HasValue && request.Filters.ToDate.HasValue)
                    filterText.Add($"Date Range: {request.Filters.FromDate.Value:MM/dd/yyyy} to {request.Filters.ToDate.Value:MM/dd/yyyy}");
                else if (request.Filters.FromDate.HasValue)
                    filterText.Add($"From: {request.Filters.FromDate.Value:MM/dd/yyyy}");
                else if (request.Filters.ToDate.HasValue)
                    filterText.Add($"To: {request.Filters.ToDate.Value:MM/dd/yyyy}");

                if (!string.IsNullOrEmpty(request.Filters.PrimaryMood))
                    filterText.Add($"Primary Mood: {request.Filters.PrimaryMood}");

                if (!string.IsNullOrEmpty(request.Filters.Category))
                    filterText.Add($"Category: {request.Filters.Category}");

                if (request.Filters.Tags != null && request.Filters.Tags.Any())
                    filterText.Add($"Tags: {string.Join(", ", request.Filters.Tags.Select(t => $"#{t}"))}");

                var filterParagraph = new iText.Paragraph(string.Join(" | ", filterText),
                    iText.FontFactory.GetFont(iText.FontFactory.HELVETICA, 10, iText.BaseColor.GRAY));
                filterParagraph.SpacingAfter = 20f;
                document.Add(filterParagraph);
            }

            // Add content options info (what metadata is included)
            var optionsText = new List<string>();

            if (request.Options.IncludeMoodDetails)
                optionsText.Add("Mood Details");

            if (request.Options.IncludeTags)
                optionsText.Add("Tags");

            if (request.Options.IncludeWordCount)
                optionsText.Add("Word Count");

            if (request.Options.IncludeTimestamps)
                optionsText.Add("Timestamps");

            if (request.Options.IncludeEntryDate)
                optionsText.Add("Entry Date");

            if (request.Options.IncludeCategory)
                optionsText.Add("Category");

            if (optionsText.Any())
            {
                var optionsTitle = new iText.Paragraph("Included Content:",
                    iText.FontFactory.GetFont(iText.FontFactory.HELVETICA_BOLD, 12));
                optionsTitle.SpacingBefore = 5f;
                optionsTitle.SpacingAfter = 5f;
                document.Add(optionsTitle);

                var optionsParagraph = new iText.Paragraph(string.Join(", ", optionsText),
                    iText.FontFactory.GetFont(iText.FontFactory.HELVETICA, 10, iText.BaseColor.GRAY));
                optionsParagraph.SpacingAfter = 20f;
                document.Add(optionsParagraph);
            }

            // Add each journal entry to the PDF
            for (int i = 0; i < journals.Count; i++)
            {
                var journal = journals[i];

                // Add page break for each entry after the first
                if (i > 0)
                {
                    document.NewPage();
                }

                // Add entry header with dark background
                var headerTable = new iTextPdf.PdfPTable(1)
                {
                    WidthPercentage = 100,
                    SpacingBefore = 10f,
                    SpacingAfter = 10f
                };

                var headerCell = new iTextPdf.PdfPCell(new iText.Phrase($"ENTRY {i + 1} OF {journals.Count}",
                    iText.FontFactory.GetFont(iText.FontFactory.HELVETICA_BOLD, 10, iText.BaseColor.WHITE)))
                {
                    BackgroundColor = new iText.BaseColor(63, 67, 78), // Dark gray color
                    Border = iText.Rectangle.NO_BORDER,
                    Padding = 8f,
                    HorizontalAlignment = iText.Element.ALIGN_CENTER
                };
                headerTable.AddCell(headerCell);
                document.Add(headerTable);

                // Entry title
                var entryHeader = new iText.Paragraph(journal.Title,
                    iText.FontFactory.GetFont(iText.FontFactory.HELVETICA_BOLD, 18, iText.BaseColor.BLACK));
                entryHeader.SpacingAfter = 8f;
                entryHeader.Alignment = iText.Element.ALIGN_CENTER;
                document.Add(entryHeader);

                // Entry metadata table with two columns
                var metadataTable = new iTextPdf.PdfPTable(2)
                {
                    WidthPercentage = 100,
                    SpacingBefore = 10f,
                    SpacingAfter = 15f
                };

                // Set column widths: 30% for labels, 70% for values
                float[] columnWidths = { 30f, 70f };
                metadataTable.SetWidths(columnWidths);

                // Entry Date (always included as it's essential)
                AddMetadataRow(metadataTable, "Entry Date:", journal.EntryDate.ToString("MMMM dd, yyyy"));

                // Category (if included in options)
                if (request.Options.IncludeCategory)
                {
                    AddMetadataRow(metadataTable, "Category:", journal.Category);
                }

                // Mood Details (if included in options)
                if (request.Options.IncludeMoodDetails)
                {
                    var moodText = journal.PrimaryMood;
                    var secondaryMoods = new List<string>();
                    if (!string.IsNullOrEmpty(journal.SecondaryMood1))
                        secondaryMoods.Add(journal.SecondaryMood1);
                    if (!string.IsNullOrEmpty(journal.SecondaryMood2))
                        secondaryMoods.Add(journal.SecondaryMood2);

                    if (secondaryMoods.Any())
                    {
                        moodText += $" ({string.Join(", ", secondaryMoods)})";
                    }
                    AddMetadataRow(metadataTable, "Mood:", moodText);
                }

                // Tags (if included in options and journal has tags)
                if (request.Options.IncludeTags && journal.Tags.Any())
                {
                    AddMetadataRow(metadataTable, "Tags:", string.Join(", ", journal.Tags.Select(t => $"#{t}")));
                }

                // Word Count (if included in options)
                if (request.Options.IncludeWordCount)
                {
                    AddMetadataRow(metadataTable, "Word Count:", $"{journal.WordCount:N0} words");
                }

                // Timestamps (if included in options)
                if (request.Options.IncludeTimestamps)
                {
                    AddMetadataRow(metadataTable, "Created:", journal.CreatedAt.ToLocalTime().ToString("MMMM dd, yyyy hh:mm tt"));
                    AddMetadataRow(metadataTable, "Last Updated:", journal.UpdatedAt.ToLocalTime().ToString("MMMM dd, yyyy hh:mm tt"));
                }

                document.Add(metadataTable);

                // Add visual separator
                document.Add(new iText.Chunk(new iText.pdf.draw.LineSeparator(1f, 100f, iText.BaseColor.LIGHT_GRAY, iText.Element.ALIGN_CENTER, -1)));
                document.Add(new iText.Paragraph(" "));

                // Journal content section
                var contentTitle = new iText.Paragraph("Journal Content:",
                    iText.FontFactory.GetFont(iText.FontFactory.HELVETICA_BOLD, 14, iText.BaseColor.DARK_GRAY));
                contentTitle.SpacingBefore = 10f;
                contentTitle.SpacingAfter = 10f;
                document.Add(contentTitle);

                var contentParagraph = new iText.Paragraph();
                contentParagraph.SpacingBefore = 5f;
                contentParagraph.SpacingAfter = 20f;

                // Strip HTML tags and get plain text content
                var plainText = StripHtmlTags(journal.Content);

                // Add the content with proper font
                var contentFont = iText.FontFactory.GetFont(iText.FontFactory.HELVETICA, 12, iText.BaseColor.BLACK);
                var contentChunk = new iText.Phrase(plainText, contentFont);
                contentParagraph.Add(contentChunk);

                document.Add(contentParagraph);
            }

            // Add footer with page numbers to all pages
            writer.PageEvent = new PdfFooter();

            // Close document - this completes PDF generation
            document.Close();

            Console.WriteLine($"PDF generated successfully with {journals.Count} entries");
            return ServiceResult<byte[]>.SuccessResult(memoryStream.ToArray());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating PDF: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return ServiceResult<byte[]>.FailureResult($"Error generating PDF: {ex.Message}");
        }
    }

    // Helper method to add a row to the metadata table
    private void AddMetadataRow(iTextPdf.PdfPTable table, string label, string value)
    {
        var labelCell = new iTextPdf.PdfPCell(new iText.Phrase(label,
            iText.FontFactory.GetFont(iText.FontFactory.HELVETICA_BOLD, 10, new iText.BaseColor(35, 68, 97)))) // #234461 color
        {
            Border = iText.Rectangle.NO_BORDER,
            Padding = 4f,
            HorizontalAlignment = iText.Element.ALIGN_LEFT
        };

        var valueCell = new iTextPdf.PdfPCell(new iText.Phrase(value,
            iText.FontFactory.GetFont(iText.FontFactory.HELVETICA, 10, iText.BaseColor.BLACK)))
        {
            Border = iText.Rectangle.NO_BORDER,
            Padding = 4f,
            HorizontalAlignment = iText.Element.ALIGN_LEFT
        };

        table.AddCell(labelCell);
        table.AddCell(valueCell);
    }

    // Helper method to strip HTML tags from journal content
    private string StripHtmlTags(string html)
    {
        if (string.IsNullOrEmpty(html))
            return "No content available.";

        try
        {
            // First decode HTML entities (&lt;, &gt;, etc.)
            var decoded = System.Net.WebUtility.HtmlDecode(html);

            // Remove HTML tags but preserve some basic formatting
            var plainText = Regex.Replace(decoded, "<.*?>", " ");

            // Replace multiple spaces with single space
            plainText = Regex.Replace(plainText, @"\s+", " ");

            // Trim and ensure it ends with proper punctuation
            plainText = plainText.Trim();
            if (!string.IsNullOrEmpty(plainText) &&
                !plainText.EndsWith(".") &&
                !plainText.EndsWith("!") &&
                !plainText.EndsWith("?"))
            {
                plainText += ".";
            }

            return plainText;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stripping HTML tags: {ex.Message}");
            return "Content format error.";
        }
    }

    // Gets all unique categories available for a user
    public async Task<ServiceResult<List<string>>> GetAvailableCategoriesAsync(int userId)
    {
        try
        {
            // Use journal service to get categories
            var result = await _journalService.GetUserCategoriesAsync(userId);
            if (result.Success)
            {
                Console.WriteLine($"Loaded {result.Data.Count} categories for user {userId}");
                return result;
            }
            return ServiceResult<List<string>>.FailureResult(result.ErrorMessage ?? "Failed to load categories");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting categories: {ex.Message}");
            return ServiceResult<List<string>>.FailureResult($"Error getting categories: {ex.Message}");
        }
    }

    // Gets all unique tags available for a user
    public async Task<ServiceResult<List<string>>> GetAvailableTagsAsync(int userId)
    {
        try
        {
            // Use journal service to get tags
            var result = await _journalService.GetUserTagsAsync(userId);
            if (result.Success)
            {
                Console.WriteLine($"Loaded {result.Data.Count} tags for user {userId}");
                return result;
            }
            return ServiceResult<List<string>>.FailureResult(result.ErrorMessage ?? "Failed to load tags");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting tags: {ex.Message}");
            return ServiceResult<List<string>>.FailureResult($"Error getting tags: {ex.Message}");
        }
    }
}

// Custom PDF footer class that adds page numbers to each page
public class PdfFooter : iTextPdf.PdfPageEventHelper
{
    private iText.Font footerFont;

    public PdfFooter()
    {
        footerFont = iText.FontFactory.GetFont(iText.FontFactory.HELVETICA, 8, iText.BaseColor.GRAY);
    }

    // Called at the end of each page to add footer
    public override void OnEndPage(iTextPdf.PdfWriter writer, iText.Document document)
    {
        var footer = new iText.Paragraph($"Page {writer.PageNumber}", footerFont)
        {
            Alignment = iText.Element.ALIGN_CENTER
        };

        // Create a table for the footer to ensure proper positioning
        var footerTable = new iTextPdf.PdfPTable(1)
        {
            TotalWidth = document.PageSize.Width - document.LeftMargin - document.RightMargin
        };

        var cell = new iTextPdf.PdfPCell(footer)
        {
            Border = iText.Rectangle.NO_BORDER,
            PaddingTop = 10f
        };

        footerTable.AddCell(cell);

        // Write the footer at the bottom margin
        footerTable.WriteSelectedRows(0, -1, document.LeftMargin, document.BottomMargin, writer.DirectContent);
    }
}