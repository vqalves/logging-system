/* ============================================================================
   Tooltip Component
   LogSystem WebApp - Bootstrap tooltip wrapper with accessibility
   ============================================================================ */

/**
 * Tooltip Manager for initializing and managing Bootstrap tooltips
 */
class TooltipManager {
    constructor() {
        this.tooltips = new Map();
    }

    /**
     * Initialize all tooltips on the page
     * Looks for elements with data-bs-toggle="tooltip"
     */
    initializeAll() {
        const tooltipElements = document.querySelectorAll('[data-bs-toggle="tooltip"]');

        tooltipElements.forEach(element => {
            this.initialize(element);
        });
    }

    /**
     * Initialize a single tooltip
     * @param {HTMLElement} element - Element to add tooltip to
     * @param {Object} options - Bootstrap tooltip options
     * @returns {bootstrap.Tooltip} - Tooltip instance
     */
    initialize(element, options = {}) {
        // Check if already initialized
        if (this.tooltips.has(element)) {
            return this.tooltips.get(element);
        }

        // Default options for accessibility
        const defaultOptions = {
            trigger: 'hover focus', // Both hover and focus for accessibility
            placement: 'top',
            html: false, // Prevent XSS
            delay: { show: 300, hide: 100 },
            ...options
        };

        // Create tooltip
        const tooltip = new bootstrap.Tooltip(element, defaultOptions);

        // Store reference
        this.tooltips.set(element, tooltip);

        // Add keyboard support
        this.addKeyboardSupport(element, tooltip);

        return tooltip;
    }

    /**
     * Add keyboard support for tooltips (Escape to close)
     * @param {HTMLElement} element - Tooltip element
     * @param {bootstrap.Tooltip} tooltip - Tooltip instance
     */
    addKeyboardSupport(element, tooltip) {
        element.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                tooltip.hide();
            }
        });
    }

    /**
     * Dispose of a tooltip
     * @param {HTMLElement} element - Element with tooltip
     */
    dispose(element) {
        if (this.tooltips.has(element)) {
            const tooltip = this.tooltips.get(element);
            tooltip.dispose();
            this.tooltips.delete(element);
        }
    }

    /**
     * Dispose of all tooltips
     */
    disposeAll() {
        this.tooltips.forEach((tooltip, element) => {
            tooltip.dispose();
        });
        this.tooltips.clear();
    }

    /**
     * Add a tooltip to an element programmatically
     * @param {HTMLElement} element - Element to add tooltip to
     * @param {string} title - Tooltip text
     * @param {Object} options - Additional options
     * @returns {bootstrap.Tooltip} - Tooltip instance
     */
    addTooltip(element, title, options = {}) {
        element.setAttribute('data-bs-toggle', 'tooltip');
        element.setAttribute('title', title);

        return this.initialize(element, options);
    }
}

/**
 * Popover Manager for initializing and managing Bootstrap popovers
 */
class PopoverManager {
    constructor() {
        this.popovers = new Map();
    }

    /**
     * Initialize all popovers on the page
     * Looks for elements with data-bs-toggle="popover"
     */
    initializeAll() {
        const popoverElements = document.querySelectorAll('[data-bs-toggle="popover"]');

        popoverElements.forEach(element => {
            this.initialize(element);
        });
    }

    /**
     * Initialize a single popover
     * @param {HTMLElement} element - Element to add popover to
     * @param {Object} options - Bootstrap popover options
     * @returns {bootstrap.Popover} - Popover instance
     */
    initialize(element, options = {}) {
        // Check if already initialized
        if (this.popovers.has(element)) {
            return this.popovers.get(element);
        }

        // Default options for accessibility
        const defaultOptions = {
            trigger: 'click', // Click for better accessibility
            placement: 'auto',
            html: true, // Allow HTML content
            sanitize: true, // Sanitize HTML to prevent XSS
            ...options
        };

        // Create popover
        const popover = new bootstrap.Popover(element, defaultOptions);

        // Store reference
        this.popovers.set(element, popover);

        // Add keyboard support
        this.addKeyboardSupport(element, popover);

        // Close popover when clicking outside
        this.addClickOutsideHandler(element, popover);

        return popover;
    }

    /**
     * Add keyboard support for popovers (Escape to close)
     * @param {HTMLElement} element - Popover element
     * @param {bootstrap.Popover} popover - Popover instance
     */
    addKeyboardSupport(element, popover) {
        // Close on Escape
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                popover.hide();
            }
        });

        // Focus management
        element.addEventListener('shown.bs.popover', () => {
            // Find the popover element
            const popoverElement = document.querySelector(`[id^="popover-"]`);
            if (popoverElement) {
                // Make popover focusable
                popoverElement.setAttribute('tabindex', '-1');

                // Focus close button if exists
                const closeButton = popoverElement.querySelector('.btn-close');
                if (closeButton) {
                    closeButton.focus();
                }
            }
        });
    }

    /**
     * Add click outside handler to close popover
     * @param {HTMLElement} element - Popover trigger element
     * @param {bootstrap.Popover} popover - Popover instance
     */
    addClickOutsideHandler(element, popover) {
        document.addEventListener('click', (e) => {
            // Check if click is outside the element and the popover
            const popoverElement = document.querySelector(`[id^="popover-"]`);
            if (popoverElement &&
                !element.contains(e.target) &&
                !popoverElement.contains(e.target)) {
                popover.hide();
            }
        });
    }

    /**
     * Dispose of a popover
     * @param {HTMLElement} element - Element with popover
     */
    dispose(element) {
        if (this.popovers.has(element)) {
            const popover = this.popovers.get(element);
            popover.dispose();
            this.popovers.delete(element);
        }
    }

    /**
     * Dispose of all popovers
     */
    disposeAll() {
        this.popovers.forEach((popover, element) => {
            popover.dispose();
        });
        this.popovers.clear();
    }

    /**
     * Add a popover to an element programmatically
     * @param {HTMLElement} element - Element to add popover to
     * @param {string} title - Popover title
     * @param {string} content - Popover content
     * @param {Object} options - Additional options
     * @returns {bootstrap.Popover} - Popover instance
     */
    addPopover(element, title, content, options = {}) {
        element.setAttribute('data-bs-toggle', 'popover');
        element.setAttribute('data-bs-title', title);
        element.setAttribute('data-bs-content', content);

        return this.initialize(element, options);
    }
}

/**
 * Initialize help icon tooltips
 * Adds tooltips to help icons (?) in forms
 */
export function initializeHelpTooltips() {
    const helpIcons = document.querySelectorAll('.form-help-icon');

    helpIcons.forEach(icon => {
        if (!icon.hasAttribute('data-bs-toggle')) {
            icon.setAttribute('data-bs-toggle', 'tooltip');
            icon.setAttribute('data-bs-placement', 'right');
        }
    });

    tooltipManager.initializeAll();
}

// Export singleton instances
export const tooltipManager = new TooltipManager();
export const popoverManager = new PopoverManager();
