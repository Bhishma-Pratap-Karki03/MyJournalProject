// Quill Editor JavaScript Bridge - No Image Upload
window.QuillEditor = {
    editors: {},

    initialize: function (elementId, placeholder, content, dotNetObjectRef) {
        try {
            // Check if Quill is available
            if (typeof Quill === 'undefined') {
                console.error('Quill is not loaded. Make sure to include Quill.js in your HTML.');
                return false;
            }

            // Create the editor container
            const container = document.getElementById(elementId);
            if (!container) {
                console.error('Editor container not found:', elementId);
                return false;
            }

            // Clear container and create editor structure WITHOUT image button
            container.innerHTML = `
                <div id="${elementId}_toolbar" class="quill-toolbar">
                    <span class="ql-formats">
                        <select class="ql-header">
                            <option value="1">Heading 1</option>
                            <option value="2">Heading 2</option>
                            <option value="3">Heading 3</option>
                            <option selected value="">Normal</option>
                        </select>
                    </span>
                    <span class="ql-formats">
                        <button class="ql-bold"></button>
                        <button class="ql-italic"></button>
                        <button class="ql-underline"></button>
                        <button class="ql-strike"></button>
                    </span>
                    <span class="ql-formats">
                        <button class="ql-list" value="ordered"></button>
                        <button class="ql-list" value="bullet"></button>
                    </span>
                    <span class="ql-formats">
                        <button class="ql-link"></button>
                    </span>
                    <span class="ql-formats">
                        <select class="ql-color"></select>
                        <select class="ql-background"></select>
                    </span>
                    <span class="ql-formats">
                        <button class="ql-clean"></button>
                    </span>
                </div>
                <div id="${elementId}_editor" class="quill-editor-content"></div>
            `;

            // Configure Quill WITHOUT image module
            const quill = new Quill(`#${elementId}_editor`, {
                modules: {
                    toolbar: `#${elementId}_toolbar`,
                    clipboard: {
                        matchVisual: false
                    }
                },
                placeholder: placeholder || 'Start writing your thoughts here...',
                theme: 'snow',
                formats: [
                    'bold', 'italic', 'underline', 'strike',
                    'header', 'list', 'link',
                    'color', 'background'
                ]
            });

            // Set initial content if provided
            if (content) {
                quill.root.innerHTML = content;
            }

            // Store editor reference
            this.editors[elementId] = quill;

            // Setup event listeners
            const handleContentChange = () => {
                const html = quill.root.innerHTML;
                const text = this.getPlainText(html);
                const wordCount = this.countWords(text);
                const charCount = this.countCharacters(text);

                // Call Blazor method using DotNetObjectReference
                if (dotNetObjectRef) {
                    try {
                        dotNetObjectRef.invokeMethodAsync('HandleEditorContentChanged', html, text, wordCount, charCount);
                    } catch (e) {
                        console.log('Error calling Blazor method:', e);
                    }
                }
            };

            // Initial call to set stats
            handleContentChange();

            // Listen for changes
            quill.on('text-change', function (delta, oldDelta, source) {
                if (source === 'user' || source === 'api') {
                    handleContentChange();
                }
            });

            console.log(`Quill editor initialized: ${elementId}`);
            return true;
        } catch (error) {
            console.error('Error initializing Quill editor:', error);
            return false;
        }
    },

    getContent: function (elementId) {
        const editor = this.editors[elementId];
        if (!editor) {
            console.error('Editor not found:', elementId);
            return { html: '', text: '', wordCount: 0, charCount: 0 };
        }

        const html = editor.root.innerHTML;
        const text = this.getPlainText(html);
        const wordCount = this.countWords(text);
        const charCount = this.countCharacters(text);

        return {
            html: html,
            text: text,
            wordCount: wordCount,
            charCount: charCount
        };
    },

    setContent: function (elementId, content) {
        const editor = this.editors[elementId];
        if (editor && content !== undefined) {
            editor.root.innerHTML = content;
            return true;
        }
        return false;
    },

    clearContent: function (elementId) {
        return this.setContent(elementId, '');
    },

    getPlainText: function (html) {
        if (!html) return '';

        // Create a temporary div to parse HTML
        const tempDiv = document.createElement('div');
        tempDiv.innerHTML = html;

        // Get plain text and normalize whitespace
        let text = tempDiv.textContent || tempDiv.innerText || '';
        text = text.replace(/\s+/g, ' ').trim();

        return text;
    },

    countWords: function (text) {
        if (!text || text.trim() === '') return 0;
        return text.split(' ').filter(word => word.length > 0).length;
    },

    countCharacters: function (text) {
        if (!text) return 0;
        // Count all characters including spaces
        return text.length;
    },

    destroy: function (elementId) {
        const editor = this.editors[elementId];
        if (editor) {
            // Remove event listeners
            editor.off('text-change');
            delete this.editors[elementId];

            // Clear container
            const container = document.getElementById(elementId);
            if (container) {
                container.innerHTML = '';
            }
            return true;
        }
        return false;
    }
};

// Initialize all editors when DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    // Auto-initialize editors with data-quill attribute
    const quillElements = document.querySelectorAll('[data-quill]');
    quillElements.forEach(element => {
        const elementId = element.id;
        const placeholder = element.getAttribute('data-placeholder') || '';
        const content = element.getAttribute('data-content') || '';

        if (elementId) {
            window.QuillEditor.initialize(elementId, placeholder, content, null);
        }
    });
});
window.downloadFile = function (base64Data, fileName, contentType) {
    try {
        // Create a blob from the base64 data
        const byteCharacters = atob(base64Data);
        const byteNumbers = new Array(byteCharacters.length);

        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }

        const byteArray = new Uint8Array(byteNumbers);
        const blob = new Blob([byteArray], { type: contentType });

        // Create download link
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.style.display = 'none';
        a.href = url;
        a.download = fileName;

        document.body.appendChild(a);
        a.click();

        // Cleanup
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);

        return true;
    } catch (error) {
        console.error('Error downloading file:', error);
        return false;
    }
};