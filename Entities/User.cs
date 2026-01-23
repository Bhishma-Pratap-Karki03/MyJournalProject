using System.ComponentModel.DataAnnotations;

namespace MyJournalProject.Entities;

// Entity class representing a User in the database
// Maps to the Users table with properties as columns
public class User
{
    // Primary key - unique identifier for the user
    public int Id { get; set; }

    // User's full name (required field with maximum length constraint)
    [Required]
    [MaxLength(200)] 
    public string FullName { get; set; } = string.Empty;

    // User's email address
    [Required]
    [EmailAddress] // Validation for proper email format
    [MaxLength(200)] // Database column size constraint
    public string Email { get; set; } = string.Empty;

    // Hashed password 
    [Required]
    public string Password { get; set; } = string.Empty;

    // Timestamp of when the user account was created
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}