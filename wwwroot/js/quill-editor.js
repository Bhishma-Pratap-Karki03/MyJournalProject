let quillInstance = null;
let editorInitialized = false;
let dotNetHelper = null;

// Initialize Quill editor
window.initializeQuillEditor = (dotNetHelperRef, elementId, placeholder, initialContent) => {
    try {
        console.log('Initializing Quill editor on:', elementId);
        dotNetHelper = dotNetHelperRef;

        const container = document.getElementById(elementId);
        if (!container) {
            console.error('Element not found:', elementId);
            return false;
        }

        // Clear any existing content
        container.innerHTML = '';

        // Create editor container
        const editorContainer = document.createElement('div');
        editorContainer.id = elementId + '-editor';
        editorContainer.className = 'quill-editor-container';
        container.appendChild(editorContainer);

        // Check if Quill is loaded
        if (typeof Quill === 'undefined') {
            console.error('Quill.js is not loaded!');
            return false;
        }

        // Initialize Quill with basic options
        quillInstance = new Quill('#' + elementId + '-editor', {
            theme: 'snow',
            placeholder: placeholder || 'Start writing your thoughts here...',
            modules: {
                toolbar: [
                    [{ 'header': [1, 2, 3, false] }],
                    ['bold', 'italic', 'underline'],
                    [{ 'list': 'ordered' }, { 'list': 'bullet' }],
                    ['link'],
                    ['clean']
                ]
            }
        });

        // Set initial content if provided
        if (initialContent && initialContent.trim() !== '' && initialContent !== '<p><br></p>') {
            quillInstance.root.innerHTML = initialContent;
        }

        // Store last content for change detection
        let lastContent = quillInstance.root.innerHTML;

        // Function to update stats
        const updateStats = () => {
            try {
                const text = quillInstance.getText().trim();
                const words = text === '' ? 0 : text.split(/\s+/).length;
                const characters = text.length;
                const currentContent = quillInstance.root.innerHTML;

                // Only dispatch event if content actually changed
                if (currentContent !== lastContent) {
                    lastContent = currentContent;

                    // Call .NET method directly
                    if (dotNetHelper) {
                        dotNetHelper.invokeMethodAsync('UpdateEditorStats', words, characters, currentContent)
                            .catch(err => console.warn('Failed to invoke .NET method:', err));
                    }
                }
            } catch (error) {
                console.error('Error in updateStats:', error);
            }
        };

        // Debounced version
        let debounceTimer;
        const debouncedUpdateStats = () => {
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(updateStats, 300);
        };

        // Set up event listeners
        quillInstance.on('text-change', debouncedUpdateStats);

        // Also update on paste
        editorContainer.addEventListener('paste', debouncedUpdateStats);

        // Mark as initialized
        editorInitialized = true;

        console.log('Quill editor initialized successfully');
        return true;
    } catch (error) {
        console.error('Error initializing Quill editor:', error);
        console.error(error.stack);
        return false;
    }
};

// Get Quill editor content as HTML
window.getQuillContent = () => {
    if (quillInstance && quillInstance.root) {
        const content = quillInstance.root.innerHTML;
        return content === '<p><br></p>' ? '' : content;
    }
    return '';
};

// Set Quill editor content
window.setQuillContent = (content) => {
    if (quillInstance) {
        try {
            quillInstance.root.innerHTML = content || '';
            return true;
        } catch (error) {
            console.error('Error setting content:', error);
            return false;
        }
    }
    return false;
};

// Clear Quill editor content
window.clearQuillEditor = () => {
    if (quillInstance) {
        quillInstance.root.innerHTML = '';
        return true;
    }
    return false;
};

// Get word and character count
window.getEditorStats = () => {
    if (quillInstance) {
        const text = quillInstance.getText().trim();
        const words = text === '' ? 0 : text.split(/\s+/).length;
        const characters = text.length;
        return { words: words, characters: characters };
    }
    return { words: 0, characters: 0 };
};

// Check if content is empty
window.isQuillContentEmpty = () => {
    if (quillInstance) {
        const html = quillInstance.root.innerHTML;
        return html === '<p><br></p>' || html === '' || html === '<p></p>';
    }
    return true;
};

// Destroy editor instance
window.destroyQuillEditor = () => {
    try {
        if (quillInstance) {
            quillInstance = null;
            editorInitialized = false;
            dotNetHelper = null;
        }
        return true;
    } catch (error) {
        console.error('Error destroying editor:', error);
        return false;
    }
};

// Helper to count words from HTML
window.countWordsInHtml = (html) => {
    if (!html) return 0;

    // Remove HTML tags
    const plainText = html.replace(/<[^>]*>/g, ' ');
    // Replace multiple spaces with single space
    const cleanText = plainText.replace(/\s+/g, ' ').trim();

    if (!cleanText) return 0;
    return cleanText.split(' ').length;
};

// Helper to count characters from HTML
window.countCharsInHtml = (html) => {
    if (!html) return 0;

    // Remove HTML tags
    const plainText = html.replace(/<[^>]*>/g, '');
    return plainText.length;
};