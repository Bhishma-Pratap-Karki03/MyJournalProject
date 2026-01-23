namespace MyJournalProject.Models;

// Main model representing an export request
// Contains both filters (what to export) and options (how to export)
public class ExportRequest
{
    // Filters for selecting which journals to include in the export
    public ExportFilterModel Filters { get; set; } = new();

    // Options for customizing the export format and content
    public ExportOptionsModel Options { get; set; } = new();
}

// Model for filtering journal entries during export
// Allows users to select specific date ranges, moods, categories, and tags
public class ExportFilterModel
{
    // Start date for the export range (optional - if null, no start date filter)
    public DateTime? FromDate { get; set; }

    // End date for the export range (optional - if null, no end date filter)
    public DateTime? ToDate { get; set; }

    // Filter by primary mood (e.g., "Positive", "Neutral", "Negative")
    public string? PrimaryMood { get; set; }

    // Filter by category (e.g., "Work", "Personal", "Travel")
    public string? Category { get; set; }

    // List of tags to filter by (journals must contain ALL selected tags)
    public List<string> Tags { get; set; } = new();
}

// Model for export formatting options
// Controls what metadata and information is included in the exported file
public class ExportOptionsModel
{
    // Include mood details (primary and secondary moods)
    public bool IncludeMoodDetails { get; set; }

    // Include tags associated with each journal entry
    public bool IncludeTags { get; set; }

    // Include word count for each journal entry
    public bool IncludeWordCount { get; set; }

    // Include creation and update timestamps
    public bool IncludeTimestamps { get; set; }

    // Include the entry date (always included by default, but this controls formatting)
    public bool IncludeEntryDate { get; set; }

    // Include the category of each journal entry
    public bool IncludeCategory { get; set; }
}

// Model for export preview data shown to users before generating the export
// Provides summary information about what will be included in the export
public class ExportPreviewData
{
    // Number of journal entries that match the selected filters
    public int EntryCount { get; set; }

    // Formatted date range string for display (e.g., "Jan 1, 2024 - Jan 31, 2024")
    public string DateRange { get; set; } = string.Empty;

    // List of applied filters for user confirmation
    public List<string> AppliedFilters { get; set; } = new();

    // Total word count across all selected journal entries
    public int TotalWordCount { get; set; }
}