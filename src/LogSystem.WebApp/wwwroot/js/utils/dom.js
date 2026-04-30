/* ============================================================================
   DOM Utilities
   LogSystem WebApp - DOM manipulation helper functions
   ============================================================================ */

/**
 * Escapes HTML special characters to prevent XSS
 * @param {string} text - Text to escape
 * @returns {string} - Escaped HTML-safe text
 */
export function escapeHtml(text) {
    if (text === null || text === undefined) return '';
    const div = document.createElement('div');
    div.textContent = String(text);
    return div.innerHTML;
}

/**
 * Announce message to screen readers using aria-live region
 * @param {string} message - Message to announce
 * @param {string} priority - 'polite' or 'assertive' (default: 'polite')
 */
export function announceToScreenReader(message, priority = 'polite') {
    const announcement = document.createElement('div');
    announcement.setAttribute('role', 'status');
    announcement.setAttribute('aria-live', priority);
    announcement.className = 'visually-hidden';
    announcement.textContent = message;

    document.body.appendChild(announcement);

    setTimeout(() => {
        document.body.removeChild(announcement);
    }, 1000);
}

/**
 * Focus an element and scroll it into view
 * @param {HTMLElement} element - Element to focus
 * @param {boolean} smooth - Use smooth scrolling (default: true)
 */
export function focusElement(element, smooth = true) {
    if (!element) return;

    element.focus();
    element.scrollIntoView({
        behavior: smooth ? 'smooth' : 'auto',
        block: 'center'
    });
}

/**
 * Find the first focusable element within a container
 * @param {HTMLElement} container - Container element
 * @returns {HTMLElement|null} - First focusable element or null
 */
export function getFirstFocusableElement(container) {
    const focusableSelectors = 'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])';
    const focusableElements = container.querySelectorAll(focusableSelectors);
    return focusableElements.length > 0 ? focusableElements[0] : null;
}

/**
 * Trap focus within a container (for modals and dialogs)
 * @param {HTMLElement} container - Container to trap focus within
 * @returns {Function} - Cleanup function to remove event listener
 */
export function trapFocus(container) {
    const focusableSelectors = 'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])';
    const focusableElements = container.querySelectorAll(focusableSelectors);
    const firstElement = focusableElements[0];
    const lastElement = focusableElements[focusableElements.length - 1];

    const handleTabKey = (e) => {
        if (e.key !== 'Tab') return;

        if (e.shiftKey) {
            // Shift + Tab
            if (document.activeElement === firstElement) {
                lastElement.focus();
                e.preventDefault();
            }
        } else {
            // Tab
            if (document.activeElement === lastElement) {
                firstElement.focus();
                e.preventDefault();
            }
        }
    };

    container.addEventListener('keydown', handleTabKey);

    // Return cleanup function
    return () => {
        container.removeEventListener('keydown', handleTabKey);
    };
}

/**
 * Debounce a function call
 * @param {Function} func - Function to debounce
 * @param {number} wait - Wait time in milliseconds
 * @returns {Function} - Debounced function
 */
export function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

/**
 * Throttle a function call
 * @param {Function} func - Function to throttle
 * @param {number} limit - Time limit in milliseconds
 * @returns {Function} - Throttled function
 */
export function throttle(func, limit) {
    let inThrottle;
    return function executedFunction(...args) {
        if (!inThrottle) {
            func(...args);
            inThrottle = true;
            setTimeout(() => inThrottle = false, limit);
        }
    };
}
