using System.ComponentModel.DataAnnotations;

namespace MyJournalProject.Models;

// Model for user registration data
// Contains validation attributes for form validation in the UI
public class RegisterModel
{
    // Full name of the user (required field)
    [Required(ErrorMessage = "Full Name is required")]
    [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters")]
    public string FullName { get; set; } = string.Empty;

    // User's email address (used as username for login)
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    // User's password (must meet minimum security requirements)
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    // Password confirmation field (must match Password field)
    [Required(ErrorMessage = "Please confirm your password")]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}