using MyJournalProject.Common;

namespace MyJournalProject.Services;


public interface IThemeService
{
    // Gets the current theme (light/dark) from storage or system preference
    // Returns: ServiceResult containing the theme name ("light" or "dark")
    Task<ServiceResult<string>> GetCurrentThemeAsync();

    // Sets a new theme for the application
    // Parameters: 
    // theme: The theme to set ("light" or "dark")
    // Returns: ServiceResult indicating success or failure
    Task<ServiceResult<bool>> SetThemeAsync(string theme);

    // Toggles between light and dark themes
    // If current theme is "light", switches to "dark", and vice versa
    // Returns: ServiceResult indicating success or failure
    Task<ServiceResult<bool>> ToggleThemeAsync();

    // Initializes the theme on application startup
    // This should be called when the app loads to apply the saved theme
    // Returns: ServiceResult indicating success or failure
    Task<ServiceResult<bool>> InitializeThemeAsync();
}