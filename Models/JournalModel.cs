using System.ComponentModel.DataAnnotations;

namespace MyJournalProject.Models;

// Model for journal entry data (used for creating and updating journals)
// Contains validation attributes for form validation in the UI
public class JournalModel
{
    // Journal ID (used for updates, not required for new entries)
    public int Id { get; set; }

    // Journal title (required field with maximum length)
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = string.Empty;

    // Journal content/body (required field, rich text/HTML content)
    [Required(ErrorMessage = "Content is required")]
    public string Content { get; set; } = string.Empty;

    // Primary emotional mood (required field, main emotion for the day)
    [Required(ErrorMessage = "Primary mood is required")]
    public string PrimaryMood { get; set; } = string.Empty;

    // Optional secondary moods 
    public string? SecondaryMood1 { get; set; }
    public string? SecondaryMood2 { get; set; }

    // Category for journal entry (defaults to "General")
    [Required(ErrorMessage = "Category is required")]
    public string Category { get; set; } = "General";

    // List of tags for organizing and searching journals
    public List<string> Tags { get; set; } = new List<string>();

    // Temporary field for entering new tags (not persisted to database)
    // Used in UI for adding custom tags
    public string NewTag { get; set; } = string.Empty;

    // Temporary field for entering new moods (not persisted to database)
    // Used in UI for adding custom mood entries
    public string NewMood { get; set; } = string.Empty;

    // Date of the journal entry (defaults to current date)
    public DateTime EntryDate { get; set; } = DateTime.Today;
}