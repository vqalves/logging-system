/* ============================================================================
   LogSystem WebApp - Main Application JavaScript (ES6 Module)
   ============================================================================

   This is the new modular entry point that replaces inline scripts.
   Import and initialize all necessary components here.

   ============================================================================ */

// Import components
import { alertManager } from './components/alert.js';
import { initializeNavigation } from './components/navigation.js';

/**
 * Initialize application on DOM ready
 */
document.addEventListener('DOMContentLoaded', function() {
    // Initialize navigation enhancements
    initializeNavigation();

    // Initialize existing alerts on the page
    alertManager.initializeExistingAlerts();

    // Log initialization (for debugging)
    console.log('LogSystem WebApp initialized');
});

// Export alert manager for use in other scripts
export { alertManager };
