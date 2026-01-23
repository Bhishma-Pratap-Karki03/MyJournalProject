using System.ComponentModel.DataAnnotations;

namespace MyJournalProject.Models;

// Model for user login data
// Contains validation attributes for form validation in the UI
public class LoginModel
{
    // User's email address (used as username for login)
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    // User's password
    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)] // Specifies this is a password field for UI rendering
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = false;
}