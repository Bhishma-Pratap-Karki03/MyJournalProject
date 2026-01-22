namespace MyJournalProject.Models;

public class ExportRequest
{
    public ExportFilterModel Filters { get; set; } = new();
    public ExportOptionsModel Options { get; set; } = new();
}

public class ExportFilterModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? PrimaryMood { get; set; }
    public string? Category { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class ExportOptionsModel
{
    public bool IncludeMoodDetails { get; set; }
    public bool IncludeTags { get; set; }
    public bool IncludeWordCount { get; set; }
    public bool IncludeTimestamps { get; set; }
    public bool IncludeEntryDate { get; set; }
    public bool IncludeCategory { get; set; }
}

public class ExportPreviewData
{
    public int EntryCount { get; set; }
    public string DateRange { get; set; } = string.Empty;
    public List<string> AppliedFilters { get; set; } = new();
    public int TotalWordCount { get; set; }
}