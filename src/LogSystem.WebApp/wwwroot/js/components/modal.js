/* ============================================================================
   Modal Component
   LogSystem WebApp - Bootstrap modal wrapper with enhanced accessibility
   ============================================================================ */

import { trapFocus, getFirstFocusableElement, announceToScreenReader } from '../utils/dom.js';

/**
 * Modal Manager class for creating and managing Bootstrap modals
 */
class ModalManager {
    constructor() {
        this.activeModal = null;
        this.previousFocus = null;
        this.focusTrap = null;
    }

    /**
     * Create and show a confirmation dialog
     * @param {Object} options - Modal options
     * @param {string} options.title - Modal title
     * @param {string} options.message - Modal message
     * @param {string} options.confirmText - Confirm button text (default: "Confirm")
     * @param {string} options.cancelText - Cancel button text (default: "Cancel")
     * @param {string} options.confirmClass - Confirm button class (default: "btn-primary")
     * @param {Function} options.onConfirm - Callback when confirmed
     * @param {Function} options.onCancel - Callback when cancelled
     * @returns {Promise<boolean>} - Resolves to true if confirmed, false if cancelled
     */
    async confirm(options) {
        const {
            title = 'Confirm Action',
            message = 'Are you sure?',
            confirmText = 'Confirm',
            cancelText = 'Cancel',
            confirmClass = 'btn-primary',
            onConfirm = null,
            onCancel = null
        } = options;

        return new Promise((resolve) => {
            // Store current focus
            this.previousFocus = document.activeElement;

            // Create modal HTML
            const modalId = `modal-${Date.now()}`;
            const modalHtml = `
                <div class="modal fade" id="${modalId}" tabindex="-1" aria-labelledby="${modalId}-title" aria-describedby="${modalId}-message" aria-modal="true" role="dialog">
                    <div class="modal-dialog modal-dialog-centered">
                        <div class="modal-content">
                            <div class="modal-header">
                                <h5 class="modal-title" id="${modalId}-title">${title}</h5>
                                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                            </div>
                            <div class="modal-body" id="${modalId}-message">
                                ${message}
                            </div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal" data-action="cancel">${cancelText}</button>
                                <button type="button" class="btn ${confirmClass}" data-action="confirm">${confirmText}</button>
                            </div>
                        </div>
                    </div>
                </div>
            `;

            // Add to DOM
            document.body.insertAdjacentHTML('beforeend', modalHtml);
            const modalElement = document.getElementById(modalId);

            // Initialize Bootstrap modal
            const modal = new bootstrap.Modal(modalElement);

            // Setup focus trap when modal is shown
            modalElement.addEventListener('shown.bs.modal', () => {
                this.activeModal = modalElement;

                // Setup focus trap
                const modalDialog = modalElement.querySelector('.modal-dialog');
                this.focusTrap = trapFocus(modalDialog);

                // Focus first focusable element
                const firstFocusable = getFirstFocusableElement(modalDialog);
                if (firstFocusable) {
                    firstFocusable.focus();
                }

                // Announce to screen readers
                announceToScreenReader(`Dialog opened: ${title}`, 'polite');
            });

            // Handle button clicks
            const confirmBtn = modalElement.querySelector('[data-action="confirm"]');
            const cancelBtn = modalElement.querySelector('[data-action="cancel"]');
            const closeBtn = modalElement.querySelector('.btn-close');

            confirmBtn.addEventListener('click', () => {
                modal.hide();
                if (onConfirm) onConfirm();
                resolve(true);
            });

            cancelBtn.addEventListener('click', () => {
                modal.hide();
                if (onCancel) onCancel();
                resolve(false);
            });

            closeBtn.addEventListener('click', () => {
                modal.hide();
                if (onCancel) onCancel();
                resolve(false);
            });

            // Cleanup when modal is hidden
            modalElement.addEventListener('hidden.bs.modal', () => {
                // Remove focus trap
                if (this.focusTrap) {
                    this.focusTrap();
                    this.focusTrap = null;
                }

                // Restore previous focus
                if (this.previousFocus) {
                    this.previousFocus.focus();
                    this.previousFocus = null;
                }

                this.activeModal = null;

                // Remove modal from DOM
                modalElement.remove();

                // Announce to screen readers
                announceToScreenReader('Dialog closed', 'polite');
            });

            // Show modal
            modal.show();
        });
    }

    /**
     * Show a simple alert dialog
     * @param {Object} options - Alert options
     * @param {string} options.title - Alert title
     * @param {string} options.message - Alert message
     * @param {string} options.okText - OK button text (default: "OK")
     * @returns {Promise<void>}
     */
    async alert(options) {
        const {
            title = 'Alert',
            message = '',
            okText = 'OK'
        } = options;

        return this.confirm({
            title,
            message,
            confirmText: okText,
            confirmClass: 'btn-primary',
            onConfirm: null,
            onCancel: null
        });
    }

    /**
     * Show a destructive action confirmation dialog
     * @param {Object} options - Options object
     * @param {string} options.title - Dialog title
     * @param {string} options.message - Dialog message
     * @param {string} options.itemName - Name of item being deleted (optional)
     * @param {Function} options.onConfirm - Callback when confirmed
     * @returns {Promise<boolean>}
     */
    async confirmDelete(options) {
        const {
            title = 'Confirm Deletion',
            message,
            itemName = '',
            onConfirm
        } = options;

        const finalMessage = message || `Are you sure you want to delete ${itemName ? `"${itemName}"` : 'this item'}? This action cannot be undone.`;

        return this.confirm({
            title,
            message: finalMessage,
            confirmText: 'Delete',
            cancelText: 'Cancel',
            confirmClass: 'btn-danger',
            onConfirm
        });
    }

    /**
     * Close the active modal programmatically
     */
    closeActive() {
        if (this.activeModal) {
            const modal = bootstrap.Modal.getInstance(this.activeModal);
            if (modal) {
                modal.hide();
            }
        }
    }
}

// Export singleton instance
export const modalManager = new ModalManager();

// Export convenience functions
export function confirmDialog(options) {
    return modalManager.confirm(options);
}

export function alertDialog(options) {
    return modalManager.alert(options);
}

export function confirmDelete(options) {
    return modalManager.confirmDelete(options);
}
