/* ============================================================================
   Validation Utilities
   LogSystem WebApp - Form validation helper functions
   ============================================================================ */

import { announceToScreenReader, focusElement } from './dom.js';

/**
 * Validate a single form field
 * @param {HTMLInputElement} field - Field to validate
 * @returns {boolean} - True if valid
 */
export function validateField(field) {
    let isValid = true;
    let errorMessage = '';

    // Check if field is empty and required
    if (field.hasAttribute('aria-required') && field.value.trim() === '') {
        isValid = false;
        errorMessage = 'This field is required.';
    }

    // Additional validation for number fields
    if (field.type === 'number' && field.value) {
        const value = parseInt(field.value);
        const min = parseInt(field.getAttribute('min'));
        const max = parseInt(field.getAttribute('max'));

        if (isNaN(value)) {
            isValid = false;
            errorMessage = 'Please enter a valid number.';
        } else if (min !== null && value < min) {
            isValid = false;
            errorMessage = `Value must be at least ${min}.`;
        } else if (max !== null && value > max) {
            isValid = false;
            errorMessage = `Value must not exceed ${max}.`;
        }
    }

    // Email validation
    if (field.type === 'email' && field.value) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(field.value)) {
            isValid = false;
            errorMessage = 'Please enter a valid email address.';
        }
    }

    // URL validation
    if (field.type === 'url' && field.value) {
        try {
            new URL(field.value);
        } catch {
            isValid = false;
            errorMessage = 'Please enter a valid URL.';
        }
    }

    // Update field state
    if (!isValid) {
        setFieldError(field, errorMessage);
    } else {
        clearFieldError(field);
    }

    return isValid;
}

/**
 * Set error state on a field
 * @param {HTMLInputElement} field - Field to mark as invalid
 * @param {string} message - Error message
 */
export function setFieldError(field, message) {
    const errorElement = document.getElementById(`${field.id}-error`);

    field.classList.remove('is-valid');
    field.classList.add('is-invalid');
    field.setAttribute('aria-invalid', 'true');

    if (errorElement) {
        errorElement.textContent = message;
        errorElement.style.display = 'block';
        errorElement.setAttribute('role', 'alert');
    }

    // Announce to screen readers
    announceToScreenReader(`Validation error: ${message}`, 'assertive');
}

/**
 * Clear error state from a field
 * @param {HTMLInputElement} field - Field to clear error from
 */
export function clearFieldError(field) {
    const errorElement = document.getElementById(`${field.id}-error`);

    field.classList.remove('is-invalid', 'is-valid');
    field.setAttribute('aria-invalid', 'false');

    if (errorElement) {
        errorElement.textContent = '';
        errorElement.style.display = 'none';
        errorElement.removeAttribute('role');
    }
}

/**
 * Validate all fields in a form
 * @param {HTMLFormElement} form - Form to validate
 * @returns {boolean} - True if all fields are valid
 */
export function validateForm(form) {
    const fields = form.querySelectorAll('.form-control[aria-required="true"], .form-select[aria-required="true"]');
    let isValid = true;
    let firstInvalidField = null;

    fields.forEach(field => {
        if (!validateField(field)) {
            isValid = false;
            if (!firstInvalidField) {
                firstInvalidField = field;
            }
        }
    });

    // Focus first invalid field
    if (!isValid && firstInvalidField) {
        focusElement(firstInvalidField);
    }

    return isValid;
}

/**
 * Clear all validation errors in a form
 * @param {HTMLFormElement} form - Form to clear errors from
 */
export function clearFormErrors(form) {
    const errorElements = form.querySelectorAll('.invalid-feedback');
    errorElements.forEach(el => {
        el.textContent = '';
        el.style.display = 'none';
        el.removeAttribute('role');
    });

    const inputElements = form.querySelectorAll('.form-control, .form-select');
    inputElements.forEach(el => {
        el.classList.remove('is-invalid', 'is-valid');
        el.setAttribute('aria-invalid', 'false');
    });
}

/**
 * Display validation errors from API response
 * @param {Object} errorData - Error data from API
 * @param {HTMLFormElement} form - Form element
 */
export function displayValidationErrors(errorData, form) {
    let firstErrorField = null;

    if (errorData.errors) {
        // Standard validation problem details format
        for (const [field, messages] of Object.entries(errorData.errors)) {
            const fieldName = field.charAt(0).toLowerCase() + field.slice(1);
            const errorElement = document.getElementById(`${fieldName}-error`);
            const inputElement = document.getElementById(fieldName);

            if (errorElement && inputElement) {
                const errorMessage = messages.join(', ');
                setFieldError(inputElement, errorMessage);

                // Track first error field for focus
                if (!firstErrorField) {
                    firstErrorField = inputElement;
                }
            }
        }

        // Focus first error field for accessibility
        if (firstErrorField) {
            focusElement(firstErrorField);
        }
    }
}

/**
 * Setup live validation for a form
 * @param {HTMLFormElement} form - Form to setup validation for
 */
export function setupLiveValidation(form) {
    const inputs = form.querySelectorAll('.form-control[aria-required="true"], .form-select[aria-required="true"]');

    inputs.forEach(input => {
        // Validate on blur for better UX
        input.addEventListener('blur', function() {
            validateField(this);
        });

        // Clear errors on input
        input.addEventListener('input', function() {
            if (this.classList.contains('is-invalid')) {
                clearFieldError(this);
            }
        });
    });
}
