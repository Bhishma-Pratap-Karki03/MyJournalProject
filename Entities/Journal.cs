using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace MyJournalProject.Entities;

public class Journal
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [ForeignKey("User")]
    public int UserId { get; set; }

    [Required]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    // Mood tracking
    [Required]
    [StringLength(50)]
    public string PrimaryMood { get; set; } = string.Empty;

    [StringLength(50)]
    public string? SecondaryMood1 { get; set; }

    [StringLength(50)]
    public string? SecondaryMood2 { get; set; }

    // Categorization
    [Required]
    [StringLength(100)]
    public string Category { get; set; } = "General";

    // Tags stored as JSON array
    public string TagsJson { get; set; } = "[]";

    [NotMapped]
    public List<string> Tags
    {
        get => JsonSerializer.Deserialize<List<string>>(TagsJson ?? "[]") ?? new List<string>();
        set => TagsJson = JsonSerializer.Serialize(value);
    }

    // Stats
    public int WordCount { get; set; } = 0;
    public int CharacterCount { get; set; } = 0;

    // Dates
    [Required]
    public DateTime EntryDate { get; set; } = DateTime.Today;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User? User { get; set; }
}