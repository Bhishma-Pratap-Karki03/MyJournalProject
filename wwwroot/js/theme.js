// Theme management JavaScript
window.applyTheme = function (theme) {
    try {
        const html = document.documentElement;

        html.removeAttribute('data-theme');
        html.classList.remove('light-theme', 'dark-theme');

        // Apply new theme
        if (theme === 'dark') {
            html.setAttribute('data-theme', 'dark');
            html.classList.add('dark-theme');
        } else {
            html.setAttribute('data-theme', 'light');
            html.classList.add('light-theme');
        }

        // Also set data attribute on body for specific component targeting
        document.body.setAttribute('data-theme', theme);

        // Dispatch custom event for theme change
        const event = new CustomEvent('themechanged', { detail: { theme } });
        document.dispatchEvent(event);

        console.log(`Theme applied: ${theme}`);
        return true;
    } catch (error) {
        console.error('Error applying theme:', error);
        return false;
    }
};

// Check system preference for dark/light mode
window.checkSystemPreference = function () {
    try {
        // Check if user's system prefers dark mode
        return window.matchMedia('(prefers-color-scheme: dark)').matches;
    } catch (error) {
        console.error('Error checking system preference:', error);
        return false;
    }
};

// Initialize theme on page load
window.initializeTheme = async function () {
    try {
        // Check localStorage first for saved theme preference
        const savedTheme = localStorage.getItem('app-theme');

        if (savedTheme) {
            // Apply saved theme if exists
            await window.applyTheme(savedTheme);
            return savedTheme;
        }

        // Check system preference if no saved theme
        const prefersDark = window.checkSystemPreference();
        const theme = prefersDark ? 'dark' : 'light';

        // Save to localStorage for future use
        localStorage.setItem('app-theme', theme);

        // Apply the theme
        await window.applyTheme(theme);
        return theme;
    } catch (error) {
        console.error('Error initializing theme:', error);
        return 'light'; // Default to light theme on error
    }
};

// Listen for system theme changes
if (window.matchMedia) {
    // Create media query listener for dark mode preference
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)');

    // Add event listener for when system theme changes
    prefersDark.addEventListener('change', (e) => {
        // Only update if user hasn't explicitly set a preference in localStorage
        if (!localStorage.getItem('app-theme')) {
            const theme = e.matches ? 'dark' : 'light';
            window.applyTheme(theme);
            localStorage.setItem('app-theme', theme);
        }
    });
}