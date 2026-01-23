using Microsoft.JSInterop;
using MyJournalProject.Common;

namespace MyJournalProject.Services;

// Service for managing application themes (light/dark mode)
// Handles theme storage, retrieval, and application to the UI
public class ThemeService : IThemeService
{
    // JavaScript runtime for interacting with localStorage
    private readonly IJSRuntime _jsRuntime;

    // Key for storing theme preference in localStorage
    private const string ThemeKey = "app-theme";

    // Available theme options
    private const string LightTheme = "light";
    private const string DarkTheme = "dark";

    // Current theme in memory (defaults to light)
    private string _currentTheme = LightTheme;

    // Constructor - injects JavaScript runtime dependency
    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    // Gets the current theme from localStorage or system preference
    public async Task<ServiceResult<string>> GetCurrentThemeAsync()
    {
        try
        {
            // Try to get theme from localStorage first (user's saved preference)
            var savedTheme = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", ThemeKey);

            // If theme is saved in localStorage, use it
            if (!string.IsNullOrEmpty(savedTheme))
            {
                _currentTheme = savedTheme;
                return ServiceResult<string>.SuccessResult(_currentTheme);
            }

            // If no saved theme, check system preference using our JS helper
            // The JS function "checkSystemPreference" detects if user prefers dark mode
            var prefersDark = await _jsRuntime.InvokeAsync<bool>("checkSystemPreference");
            _currentTheme = prefersDark ? DarkTheme : LightTheme;

            // Save the detected theme to localStorage for future use
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ThemeKey, _currentTheme);

            // Return the detected theme
            return ServiceResult<string>.SuccessResult(_currentTheme);
        }
        catch (Exception ex)
        {
            // If error occurs, log it and default to light theme
            Console.WriteLine($"Error getting theme: {ex.Message}");
            return ServiceResult<string>.SuccessResult(LightTheme); // Default to light theme
        }
    }

    // Sets a new theme (light or dark)
    public async Task<ServiceResult<bool>> SetThemeAsync(string theme)
    {
        try
        {
            // Validate theme input - must be either "light" or "dark"
            if (theme != LightTheme && theme != DarkTheme)
            {
                return ServiceResult<bool>.FailureResult("Invalid theme. Must be 'light' or 'dark'.");
            }

            // Update current theme in memory
            _currentTheme = theme;

            // Save theme preference to localStorage for persistence
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ThemeKey, theme);

            // Apply the theme to the HTML document (updates CSS classes)
            await ApplyThemeToDocument();

            // Return success result
            return ServiceResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            // Log error and return failure result
            Console.WriteLine($"Error setting theme: {ex.Message}");
            return ServiceResult<bool>.FailureResult($"Error setting theme: {ex.Message}");
        }
    }

    // Toggles between light and dark themes
    public async Task<ServiceResult<bool>> ToggleThemeAsync()
    {
        try
        {
            // Get current theme
            var currentThemeResult = await GetCurrentThemeAsync();

            // If getting current theme fails, default to light theme
            if (!currentThemeResult.Success)
            {
                currentThemeResult = ServiceResult<string>.SuccessResult(LightTheme);
            }

            // Determine new theme (switch from light to dark or vice versa)
            var newTheme = currentThemeResult.Data == LightTheme ? DarkTheme : LightTheme;

            // Set and apply the new theme
            return await SetThemeAsync(newTheme);
        }
        catch (Exception ex)
        {
            // Log error and return failure result
            Console.WriteLine($"Error toggling theme: {ex.Message}");
            return ServiceResult<bool>.FailureResult($"Error toggling theme: {ex.Message}");
        }
    }

    // Initializes the theme on application startup
    // Called when the app loads to apply the saved theme
    public async Task<ServiceResult<bool>> InitializeThemeAsync()
    {
        try
        {
            // Get the current/saved theme
            var themeResult = await GetCurrentThemeAsync();

            // If theme retrieval is successful, apply it to the document
            if (themeResult.Success)
            {
                await ApplyThemeToDocument();
                return ServiceResult<bool>.SuccessResult(true);
            }

            // If theme retrieval failed, return false (but still success result)
            return ServiceResult<bool>.SuccessResult(false);
        }
        catch (Exception ex)
        {
            // Log error and return failure result
            Console.WriteLine($"Error initializing theme: {ex.Message}");
            return ServiceResult<bool>.FailureResult($"Error initializing theme: {ex.Message}");
        }
    }

    // Private helper method to apply theme to the HTML document
    // Calls JavaScript function "applyTheme" to update CSS classes
    private async Task ApplyThemeToDocument()
    {
        try
        {
            // Call JavaScript function that adds/removes CSS classes for theme
            await _jsRuntime.InvokeVoidAsync("applyTheme", _currentTheme);
        }
        catch (Exception ex)
        {
            // Log error but don't throw - theme might still work partially
            Console.WriteLine($"Error applying theme to document: {ex.Message}");
        }
    }
}