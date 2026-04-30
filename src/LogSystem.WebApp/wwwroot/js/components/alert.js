/* ============================================================================
   Alert Component
   LogSystem WebApp - Alert message management
   ============================================================================ */

import { escapeHtml, announceToScreenReader } from '../utils/dom.js';

/**
 * Alert configuration
 */
const ALERT_CONFIG = {
    success: { autoDismiss: true, duration: 3000, priority: 'polite' },
    info: { autoDismiss: true, duration: 5000, priority: 'polite' },
    warning: { autoDismiss: false, duration: 0, priority: 'polite' },
    danger: { autoDismiss: false, duration: 0, priority: 'assertive' },
    error: { autoDismiss: false, duration: 0, priority: 'assertive' }
};

/**
 * Alert Manager class
 */
class AlertManager {
    constructor() {
        this.alerts = new Map();
        this.autoDismissTimers = new Map();
    }

    /**
     * Initialize alert auto-dismiss functionality on page load
     */
    initializeExistingAlerts() {
        const alerts = document.querySelectorAll('.alert');

        alerts.forEach(alert => {
            // Add icon wrapper if not present
            if (!alert.querySelector('.alert-icon')) {
                this.enhanceAlertWithIcon(alert);
            }

            // Set up auto-dismiss
            this.setupAlertAutoDismiss(alert);

            // Set up close button
            const closeButton = alert.querySelector('.btn-close');
            if (closeButton) {
                closeButton.addEventListener('click', () => this.dismissAlert(alert));
            }
        });
    }

    /**
     * Enhance alert with icon wrapper
     * @param {HTMLElement} alert - Alert element
     */
    enhanceAlertWithIcon(alert) {
        // Skip if already enhanced
        if (alert.querySelector('.alert-icon')) return;

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
    setupAlertAutoDismiss(alert) {
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
            const timer = setTimeout(() => {
                this.dismissAlert(alert);
            }, config.duration);

            // Store timer so it can be cancelled if needed
            this.autoDismissTimers.set(alert, timer);
        }
    }

    /**
     * Dismiss an alert with animation
     * @param {HTMLElement} alert - Alert element to dismiss
     */
    dismissAlert(alert) {
        // Cancel auto-dismiss timer if exists
        if (this.autoDismissTimers.has(alert)) {
            clearTimeout(this.autoDismissTimers.get(alert));
            this.autoDismissTimers.delete(alert);
        }

        // Add dismissing animation class
        alert.classList.add('alert-dismissing');

        // After animation completes, hide the alert
        setTimeout(() => {
            alert.style.display = 'none';
            alert.classList.remove('alert-dismissing');

            // Announce to screen readers
            announceToScreenReader('Alert dismissed', 'polite');
        }, 300); // Match animation duration
    }

    /**
     * Show an alert message programmatically
     * @param {string} message - Alert message
     * @param {string} type - Alert type (success, danger, warning, info)
     * @param {string} containerId - ID of container element (optional)
     * @returns {HTMLElement} - Created alert element
     */
    showAlert(message, type = 'info', containerId = null) {
        let container = containerId ? document.getElementById(containerId) : null;

        // If no specific container, look for existing alert elements
        if (!container) {
            const existingAlert = document.getElementById(`${type}-message`);
            if (existingAlert) {
                existingAlert.innerHTML = `
                    <div class="alert-icon" role="img" aria-label="Alert icon"></div>
                    <div class="alert-content">${escapeHtml(message)}</div>
                `;
                existingAlert.style.display = 'flex';
                this.setupAlertAutoDismiss(existingAlert);

                // Announce to screen readers
                const config = ALERT_CONFIG[type] || ALERT_CONFIG.info;
                announceToScreenReader(message, config.priority);

                return existingAlert;
            }

            // Create container at top of main content
            container = document.getElementById('alert-container');
            if (!container) {
                container = document.createElement('div');
                container.id = 'alert-container';
                container.setAttribute('role', 'region');
                container.setAttribute('aria-label', 'Notifications');

                const main = document.querySelector('main');
                if (main) {
                    main.insertBefore(container, main.firstChild);
                }
            }
        }

        const alertId = `alert-${Date.now()}`;
        const config = ALERT_CONFIG[type] || ALERT_CONFIG.info;

        const alertHtml = `
            <div id="${alertId}" class="alert alert-${type} alert-dismissible" role="alert" aria-live="${config.priority}">
                <div class="alert-icon" role="img" aria-label="Alert icon"></div>
                <div class="alert-content">${escapeHtml(message)}</div>
                <button type="button" class="btn-close" aria-label="Close alert" data-alert-id="${alertId}">
                    <span aria-hidden="true">&times;</span>
                </button>
            </div>
        `;

        container.insertAdjacentHTML('afterbegin', alertHtml);
        const alert = document.getElementById(alertId);

        // Setup close button
        const closeButton = alert.querySelector('.btn-close');
        if (closeButton) {
            closeButton.addEventListener('click', () => this.dismissAlert(alert));
        }

        this.setupAlertAutoDismiss(alert);

        // Announce to screen readers
        announceToScreenReader(message, config.priority);

        // Store reference
        this.alerts.set(alertId, alert);

        return alert;
    }

    /**
     * Helper function to show success message
     * @param {string} message - Success message
     * @param {string} containerId - Optional container ID
     * @returns {HTMLElement} - Created alert element
     */
    showSuccess(message, containerId = null) {
        return this.showAlert(message, 'success', containerId);
    }

    /**
     * Helper function to show error message
     * @param {string} message - Error message
     * @param {string} containerId - Optional container ID
     * @returns {HTMLElement} - Created alert element
     */
    showError(message, containerId = null) {
        return this.showAlert(message, 'danger', containerId);
    }

    /**
     * Helper function to show warning message
     * @param {string} message - Warning message
     * @param {string} containerId - Optional container ID
     * @returns {HTMLElement} - Created alert element
     */
    showWarning(message, containerId = null) {
        return this.showAlert(message, 'warning', containerId);
    }

    /**
     * Helper function to show info message
     * @param {string} message - Info message
     * @param {string} containerId - Optional container ID
     * @returns {HTMLElement} - Created alert element
     */
    showInfo(message, containerId = null) {
        return this.showAlert(message, 'info', containerId);
    }

    /**
     * Clear all alerts
     */
    clearAll() {
        this.alerts.forEach((alert, id) => {
            this.dismissAlert(alert);
        });
        this.alerts.clear();
    }
}

// Export singleton instance
export const alertManager = new AlertManager();

// Export helper functions for backward compatibility
export function showAlert(message, type = 'info', containerId = null) {
    return alertManager.showAlert(message, type, containerId);
}

export function showSuccess(message, containerId = null) {
    return alertManager.showSuccess(message, containerId);
}

export function showError(message, containerId = null) {
    return alertManager.showError(message, containerId);
}

export function showWarning(message, containerId = null) {
    return alertManager.showWarning(message, containerId);
}

export function showInfo(message, containerId = null) {
    return alertManager.showInfo(message, containerId);
}
