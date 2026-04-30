// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

/* ============================================================================
   LogSystem WebApp - Global JavaScript
   ============================================================================ */

/* -------------------------------------------------------------------------
   Navigation Active State Detection - Step 3
   Automatically highlights the current page in the navigation menu
   ------------------------------------------------------------------------- */

document.addEventListener('DOMContentLoaded', function() {
    setActiveNavLink();
    initializeAlerts();
});

/**
 * Sets the active class on the navigation link matching the current page
 * Handles both exact matches and partial matches (for sub-pages)
 */
function setActiveNavLink() {
    const currentPath = window.location.pathname.toLowerCase();
    const navLinks = document.querySelectorAll('.nav-link');

    // Remove active class from all links first
    navLinks.forEach(link => link.classList.remove('active'));

    // Find the best matching link
    let bestMatch = null;
    let bestMatchLength = 0;

    navLinks.forEach(link => {
        const href = link.getAttribute('href');
        if (!href) return;

        const linkPath = href.toLowerCase();

        // Exact match - highest priority
        if (currentPath === linkPath || currentPath === linkPath + '/') {
            if (linkPath.length > bestMatchLength) {
                bestMatch = link;
                bestMatchLength = linkPath.length;
            }
        }
        // Partial match - for sub-pages (e.g., /LogCollections/Manage matches /LogCollections)
        else if (currentPath.startsWith(linkPath) && linkPath !== '/') {
            if (linkPath.length > bestMatchLength) {
                bestMatch = link;
                bestMatchLength = linkPath.length;
            }
        }
    });

    // Special case: Home page should only match exact "/"
    const homePath = '/';
    if (currentPath === homePath || currentPath === homePath) {
        const homeLink = document.getElementById('nav-home');
        if (homeLink) {
            bestMatch = homeLink;
        }
    }

    // Apply active class to best match
    if (bestMatch) {
        bestMatch.classList.add('active');
        bestMatch.setAttribute('aria-current', 'page');
    }
}

/* -------------------------------------------------------------------------
   Alert System - Step 6: Auto-dismiss Functionality
   Manages alert messages with auto-dismiss and enhanced UX
   ------------------------------------------------------------------------- */

/**
 * Alert configuration
 */
const ALERT_CONFIG = {
    success: { autoDismiss: true, duration: 3000 },
    info: { autoDismiss: true, duration: 5000 },
    warning: { autoDismiss: false, duration: 0 },
    danger: { autoDismiss: false, duration: 0 },
    error: { autoDismiss: false, duration: 0 }
};

/**
 * Initialize alert auto-dismiss functionality
 * Runs on page load and sets up auto-dismiss for success/info alerts
 */
function initializeAlerts() {
    const alerts = document.querySelectorAll('.alert');

    alerts.forEach(alert => {
        // Add icon wrapper if not present
        if (!alert.querySelector('.alert-icon')) {
            enhanceAlertWithIcon(alert);
        }

        // Set up auto-dismiss
        setupAlertAutoDismiss(alert);

        // Set up close button
        const closeButton = alert.querySelector('.btn-close');
        if (closeButton) {
            closeButton.addEventListener('click', () => dismissAlert(alert));
        }
    });
}

/**
 * Enhance alert with icon wrapper
 * @param {HTMLElement} alert - Alert element
 */
function enhanceAlertWithIcon(alert) {
    // Get alert content
    const content = alert.innerHTML;

    // Wrap content with icon and content divs
    alert.innerHTML = `
        <div class="alert-icon" role="img" aria-label="Alert icon"></div>
        <div class="alert-content">${content}</div>
    `;
}

/**
 * Set up auto-dismiss for an alert
 * @param {HTMLElement} alert - Alert element
 */
function setupAlertAutoDismiss(alert) {
    // Determine alert type
    let alertType = null;
    for (const type of Object.keys(ALERT_CONFIG)) {
        if (alert.classList.contains(`alert-${type}`)) {
            alertType = type;
            break;
        }
    }

    if (!alertType) return;

    const config = ALERT_CONFIG[alertType];

    if (config.autoDismiss && alert.style.display !== 'none') {
        setTimeout(() => {
            dismissAlert(alert);
        }, config.duration);
    }
}

/**
 * Dismiss an alert with animation
 * @param {HTMLElement} alert - Alert element to dismiss
 */
function dismissAlert(alert) {
    // Add dismissing animation class
    alert.classList.add('alert-dismissing');

    // After animation completes, hide the alert
    setTimeout(() => {
        alert.style.display = 'none';
        alert.classList.remove('alert-dismissing');

        // Announce to screen readers
        announceToScreenReader('Alert dismissed');
    }, 300); // Match animation duration
}

/**
 * Show an alert message programmatically
 * @param {string} message - Alert message
 * @param {string} type - Alert type (success, danger, warning, info)
 * @param {string} containerId - ID of container element
 */
function showAlert(message, type = 'info', containerId = 'alert-container') {
    let container = document.getElementById(containerId);

    // If no specific container, look for existing alert elements
    if (!container) {
        const existingAlert = document.getElementById(`${type}-message`);
        if (existingAlert) {
            existingAlert.innerHTML = `
                <div class="alert-icon" role="img" aria-label="Alert icon"></div>
                <div class="alert-content">${escapeHtml(message)}</div>
            `;
            existingAlert.style.display = 'flex';
            setupAlertAutoDismiss(existingAlert);
            return;
        }

        // Create container at top of main content
        container = document.createElement('div');
        container.id = 'alert-container';
        const main = document.querySelector('main');
        if (main) {
            main.insertBefore(container, main.firstChild);
        }
    }

    const alertId = `alert-${Date.now()}`;
    const alertHtml = `
        <div id="${alertId}" class="alert alert-${type} alert-dismissible" role="alert" aria-live="${type === 'danger' || type === 'error' ? 'assertive' : 'polite'}">
            <div class="alert-icon" role="img" aria-label="Alert icon"></div>
            <div class="alert-content">${escapeHtml(message)}</div>
            <button type="button" class="btn-close" aria-label="Close alert" onclick="dismissAlert(document.getElementById('${alertId}'))">
                <span aria-hidden="true">&times;</span>
            </button>
        </div>
    `;

    container.insertAdjacentHTML('afterbegin', alertHtml);
    const alert = document.getElementById(alertId);
    setupAlertAutoDismiss(alert);
}

/**
 * Helper function to show success message
 * @param {string} message - Success message
 */
function showSuccess(message) {
    showAlert(message, 'success');
}

/**
 * Helper function to show error message
 * @param {string} message - Error message
 */
function showError(message) {
    showAlert(message, 'danger');
}

/**
 * Helper function to show warning message
 * @param {string} message - Warning message
 */
function showWarning(message) {
    showAlert(message, 'warning');
}

/**
 * Helper function to show info message
 * @param {string} message - Info message
 */
function showInfo(message) {
    showAlert(message, 'info');
}

/**
 * Announce message to screen readers
 * @param {string} message - Message to announce
 */
function announceToScreenReader(message) {
    const announcement = document.createElement('div');
    announcement.setAttribute('role', 'status');
    announcement.setAttribute('aria-live', 'polite');
    announcement.className = 'visually-hidden';
    announcement.textContent = message;

    document.body.appendChild(announcement);

    setTimeout(() => {
        document.body.removeChild(announcement);
    }, 1000);
}

/* -------------------------------------------------------------------------
   Utility Functions
   Common helper functions used across the application
   ------------------------------------------------------------------------- */

/**
 * Escapes HTML special characters to prevent XSS
 * @param {string} text - Text to escape
 * @returns {string} - Escaped HTML-safe text
 */
function escapeHtml(text) {
    if (text === null || text === undefined) return '';
    const div = document.createElement('div');
    div.textContent = String(text);
    return div.innerHTML;
}
