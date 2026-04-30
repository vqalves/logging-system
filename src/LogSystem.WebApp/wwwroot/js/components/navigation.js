/* ============================================================================
   Navigation Component
   LogSystem WebApp - Navigation active state and enhancements
   ============================================================================ */

/**
 * Sets the active class on the navigation link matching the current page
 * Handles both exact matches and partial matches (for sub-pages)
 */
export function setActiveNavLink() {
    const currentPath = window.location.pathname.toLowerCase();
    const navLinks = document.querySelectorAll('.nav-link');

    // Remove active class from all links first
    navLinks.forEach(link => {
        link.classList.remove('active');
        link.removeAttribute('aria-current');
    });

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
    if (currentPath === homePath) {
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

/**
 * Initialize skip link for keyboard navigation
 * Adds a "Skip to main content" link at the top of the page
 */
export function initializeSkipLink() {
    // Check if skip link already exists
    if (document.getElementById('skip-link')) return;

    // Create skip link
    const skipLink = document.createElement('a');
    skipLink.id = 'skip-link';
    skipLink.href = '#main-content';
    skipLink.className = 'skip-link';
    skipLink.textContent = 'Skip to main content';

    // Add to top of body
    document.body.insertBefore(skipLink, document.body.firstChild);

    // Add ID to main element if not present
    const main = document.querySelector('main');
    if (main && !main.id) {
        main.id = 'main-content';
        main.setAttribute('tabindex', '-1'); // Make focusable but not in tab order
    }

    // Handle click
    skipLink.addEventListener('click', (e) => {
        e.preventDefault();
        if (main) {
            main.focus();
            main.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    });
}

/**
 * Enhance mobile navigation menu accessibility
 */
export function enhanceMobileNavigation() {
    const navToggler = document.querySelector('.navbar-toggler');
    const navCollapse = document.querySelector('.navbar-collapse');

    if (!navToggler || !navCollapse) return;

    // Ensure proper ARIA attributes
    const collapseId = navCollapse.id || 'navbarSupportedContent';
    navCollapse.id = collapseId;
    navToggler.setAttribute('aria-controls', collapseId);

    // Update aria-expanded based on collapse state
    navCollapse.addEventListener('shown.bs.collapse', () => {
        navToggler.setAttribute('aria-expanded', 'true');
    });

    navCollapse.addEventListener('hidden.bs.collapse', () => {
        navToggler.setAttribute('aria-expanded', 'false');
    });

    // Close menu when Escape is pressed
    navCollapse.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && navCollapse.classList.contains('show')) {
            navToggler.click();
            navToggler.focus();
        }
    });

    // Close menu when clicking outside
    document.addEventListener('click', (e) => {
        if (navCollapse.classList.contains('show') &&
            !navCollapse.contains(e.target) &&
            !navToggler.contains(e.target)) {
            navToggler.click();
        }
    });
}

/**
 * Initialize breadcrumb navigation
 */
export function initializeBreadcrumbs() {
    const breadcrumbs = document.querySelectorAll('.breadcrumb-nav');

    breadcrumbs.forEach(breadcrumb => {
        // Ensure proper ARIA label
        if (!breadcrumb.getAttribute('aria-label')) {
            breadcrumb.setAttribute('aria-label', 'Breadcrumb navigation');
        }

        // Ensure active item has aria-current
        const activeItem = breadcrumb.querySelector('.breadcrumb-item.active');
        if (activeItem && !activeItem.getAttribute('aria-current')) {
            activeItem.setAttribute('aria-current', 'page');
        }
    });
}

/**
 * Initialize all navigation enhancements
 */
export function initializeNavigation() {
    setActiveNavLink();
    initializeSkipLink();
    enhanceMobileNavigation();
    initializeBreadcrumbs();
}
