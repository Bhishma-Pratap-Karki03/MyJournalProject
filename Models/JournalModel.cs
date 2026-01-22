using System.ComponentModel.DataAnnotations;

namespace MyJournalProject.Models;

public class JournalModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required")]
    public string Content { get; set; } = string.Empty;

    [Required(ErrorMessage = "Primary mood is required")]
    public string PrimaryMood { get; set; } = string.Empty;

    public string? SecondaryMood1 { get; set; }
    public string? SecondaryMood2 { get; set; }

    [Required(ErrorMessage = "Category is required")]
    public string Category { get; set; } = "General";

    public List<string> Tags { get; set; } = new List<string>();

    // For new tag input
    public string NewTag { get; set; } = string.Empty;

    // For new mood input
    public string NewMood { get; set; } = string.Empty;

    public DateTime EntryDate { get; set; } = DateTime.Today;
}