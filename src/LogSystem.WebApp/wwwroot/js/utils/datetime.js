/* ============================================================================
   DateTime Utilities
   LogSystem WebApp - Date and time helper functions
   ============================================================================ */

/**
 * Format ISO datetime to dd/MM/yyyy HH:mm
 * @param {string} isoString - ISO datetime string
 * @returns {string} - Formatted datetime string
 */
export function formatDateTime(isoString) {
    if (!isoString) return '';

    const date = new Date(isoString);
    if (isNaN(date.getTime())) return isoString;

    const day = String(date.getUTCDate()).padStart(2, '0');
    const month = String(date.getUTCMonth() + 1).padStart(2, '0');
    const year = date.getUTCFullYear();
    const hours = String(date.getUTCHours()).padStart(2, '0');
    const minutes = String(date.getUTCMinutes()).padStart(2, '0');

    return `${day}/${month}/${year} ${hours}:${minutes}`;
}

/**
 * Convert dd/MM/yyyy HH:mm to ISO format (UTC)
 * @param {string} dateTimeStr - Datetime string in dd/MM/yyyy HH:mm format
 * @returns {string|null} - ISO datetime string or null if invalid
 */
export function dateTimeToISO(dateTimeStr) {
    if (!dateTimeStr || dateTimeStr.trim() === '') return null;

    const match = dateTimeStr.match(/^(\d{2})\/(\d{2})\/(\d{4})\s(\d{2}):(\d{2})$/);
    if (!match) return null;

    const [, day, month, year, hour, minute] = match;

    // Create ISO format: yyyy-MM-ddTHH:mm:ss
    return `${year}-${month}-${day}T${hour}:${minute}:00`;
}

/**
 * Apply date/time input mask for dd/MM/yyyy HH:mm format
 * @param {HTMLInputElement} input - Input element to apply mask to
 */
export function applyDateTimeMask(input) {
    input.addEventListener('input', function(e) {
        let value = e.target.value.replace(/\D/g, ''); // Remove non-digits
        let formatted = '';

        // Format as dd/MM/yyyy HH:mm
        if (value.length > 0) {
            formatted = value.substring(0, 2); // dd
        }
        if (value.length >= 3) {
            formatted += '/' + value.substring(2, 4); // MM
        }
        if (value.length >= 5) {
            formatted += '/' + value.substring(4, 8); // yyyy
        }
        if (value.length >= 9) {
            formatted += ' ' + value.substring(8, 10); // HH
        }
        if (value.length >= 11) {
            formatted += ':' + value.substring(10, 12); // mm
        }

        e.target.value = formatted;
    });

    input.addEventListener('keydown', function(e) {
        // Allow: backspace, delete, tab, escape, enter
        if ([46, 8, 9, 27, 13].indexOf(e.keyCode) !== -1 ||
            // Allow: Ctrl+A, Ctrl+C, Ctrl+V, Ctrl+X
            (e.keyCode === 65 && e.ctrlKey === true) ||
            (e.keyCode === 67 && e.ctrlKey === true) ||
            (e.keyCode === 86 && e.ctrlKey === true) ||
            (e.keyCode === 88 && e.ctrlKey === true) ||
            // Allow: home, end, left, right
            (e.keyCode >= 35 && e.keyCode <= 39)) {
            return;
        }
        // Ensure that it is a number and stop the keypress
        if ((e.shiftKey || (e.keyCode < 48 || e.keyCode > 57)) && (e.keyCode < 96 || e.keyCode > 105)) {
            e.preventDefault();
        }
        // Limit to 16 characters (dd/MM/yyyy HH:mm)
        if (e.target.value.replace(/\D/g, '').length >= 12) {
            e.preventDefault();
        }
    });
}

/**
 * Validate datetime string format
 * @param {string} dateTimeStr - Datetime string to validate
 * @returns {boolean} - True if valid format
 */
export function isValidDateTime(dateTimeStr) {
    if (!dateTimeStr) return false;
    const match = dateTimeStr.match(/^(\d{2})\/(\d{2})\/(\d{4})\s(\d{2}):(\d{2})$/);
    if (!match) return false;

    const [, day, month, year, hour, minute] = match;
    const dayNum = parseInt(day);
    const monthNum = parseInt(month);
    const hourNum = parseInt(hour);
    const minuteNum = parseInt(minute);

    // Basic validation
    if (dayNum < 1 || dayNum > 31) return false;
    if (monthNum < 1 || monthNum > 12) return false;
    if (hourNum < 0 || hourNum > 23) return false;
    if (minuteNum < 0 || minuteNum > 59) return false;

    return true;
}
