# LogSystem WebApp - JavaScript Module Documentation

This directory contains the modular ES6 JavaScript architecture for the LogSystem WebApp.

## Directory Structure

```
js/
├── api/              # API client and communication
│   └── client.js     # Fetch wrappers, API endpoints
├── components/       # Reusable UI components
│   ├── alert.js      # Alert/toast notifications
│   ├── modal.js      # Modal dialogs and confirmations
│   ├── navigation.js # Navigation enhancements
│   └── tooltip.js    # Tooltips and popovers
├── utils/            # Helper utilities
│   ├── dom.js        # DOM manipulation
│   ├── datetime.js   # Date/time formatting
│   └── validation.js # Form validation
├── pages/            # Page-specific modules
│   └── (to be populated)
├── main.js           # Main entry point
└── site.js           # Legacy global functions (backward compat)
```

## Usage

### Importing Modules

Use ES6 import syntax:

```javascript
import { showSuccess, showError } from './components/alert.js';
import { confirmDialog } from './components/modal.js';
import { logCollectionsApi } from './api/client.js';
import { validateForm } from './utils/validation.js';
```

### Main Entry Point

The `main.js` file initializes all core functionality:

```javascript
// Automatically runs on DOM ready
// - Initializes navigation (skip link, active states, mobile menu)
// - Initializes alerts (auto-dismiss)
// - Sets up accessibility features
```

Include in your page:
```html
<script type="module" src="~/js/main.js"></script>
```

## Component Reference

### Alert Manager

**File:** `components/alert.js`

**Purpose:** Display success, error, warning, and info messages

**Features:**
- Auto-dismiss for success/info (3-5 seconds)
- Manual dismiss for error/warning
- Screen reader announcements
- Icon indicators

**Usage:**
```javascript
import { alertManager } from './components/alert.js';

// Show success (auto-dismisses)
alertManager.showSuccess('Operation completed successfully');

// Show error (manual dismiss)
alertManager.showError('Failed to save data');

// Show warning
alertManager.showWarning('This action cannot be undone');

// Show info
alertManager.showInfo('Processing in background');
```

**Shorthand Functions:**
```javascript
import { showSuccess, showError, showWarning, showInfo } from './components/alert.js';

showSuccess('Saved!');
showError('Error occurred');
```

---

### Modal Manager

**File:** `components/modal.js`

**Purpose:** Create confirmation dialogs and modals

**Features:**
- Promise-based API (async/await)
- Focus management and trapping
- Keyboard support (Escape to close)
- Screen reader announcements
- Customizable buttons and styling

**Usage:**
```javascript
import { confirmDialog, confirmDelete, alertDialog } from './components/modal.js';

// Confirmation dialog
const confirmed = await confirmDialog({
    title: 'Confirm Action',
    message: 'Are you sure you want to proceed?',
    confirmText: 'Yes, Continue',
    cancelText: 'Cancel',
    confirmClass: 'btn-primary'
});

if (confirmed) {
    // User clicked confirm
    await performAction();
}

// Delete confirmation (pre-configured for danger)
const deleted = await confirmDelete({
    title: 'Delete Collection',
    itemName: 'Log Collection ABC',
    onConfirm: async () => {
        await api.deleteCollection(id);
    }
});

// Simple alert
await alertDialog({
    title: 'Notice',
    message: 'Operation completed',
    okText: 'OK'
});
```

---

### Tooltip & Popover Managers

**File:** `components/tooltip.js`

**Purpose:** Manage Bootstrap tooltips and popovers with accessibility

**Features:**
- Auto-initialization from HTML attributes
- Keyboard support (Escape to dismiss)
- Focus management
- XSS protection

**Usage:**

**HTML (declarative):**
```html
<!-- Tooltip -->
<button data-bs-toggle="tooltip" title="Edit this collection">Edit</button>

<!-- Popover -->
<button data-bs-toggle="popover"
        data-bs-title="Help"
        data-bs-content="This is a detailed explanation...">
    Help
</button>
```

**JavaScript (programmatic):**
```javascript
import { tooltipManager, popoverManager } from './components/tooltip.js';

// Initialize all tooltips on page
tooltipManager.initializeAll();

// Add tooltip to element
tooltipManager.addTooltip(element, 'Tooltip text here');

// Initialize all popovers on page
popoverManager.initializeAll();

// Add popover to element
popoverManager.addPopover(element, 'Title', 'Content HTML', {
    placement: 'right'
});
```

---

### Navigation

**File:** `components/navigation.js`

**Purpose:** Enhance navigation with accessibility features

**Features:**
- Skip to main content link
- Active page indicator
- Mobile menu enhancements
- Keyboard navigation
- Breadcrumb ARIA

**Usage:**
```javascript
import { initializeNavigation } from './components/navigation.js';

// Initialize all navigation enhancements
// (automatically called by main.js)
initializeNavigation();
```

Individual functions:
```javascript
import {
    setActiveNavLink,
    initializeSkipLink,
    enhanceMobileNavigation,
    initializeBreadcrumbs
} from './components/navigation.js';

setActiveNavLink();            // Highlight current page
initializeSkipLink();          // Add skip link
enhanceMobileNavigation();     // Enhance mobile menu
initializeBreadcrumbs();       // Add breadcrumb ARIA
```

---

### API Client

**File:** `api/client.js`

**Purpose:** Centralized API communication with error handling

**Features:**
- Type-safe responses
- Custom error handling
- HTTP method wrappers (GET, POST, PUT, DELETE)
- Specialized API clients

**Usage:**
```javascript
import { logCollectionsApi, logAttributesApi } from './api/client.js';

// Get all collections
try {
    const collections = await logCollectionsApi.getAll();
    console.log(collections);
} catch (error) {
    console.error('API Error:', error.status, error.message);
}

// Save collection
const formData = {
    id: 0,
    name: 'New Collection',
    tableName: 'new_logs',
    // ...
};

const saved = await logCollectionsApi.save(formData);

// Delete collection
await logCollectionsApi.deleteById(collectionId);

// Get metrics
const metrics = await logCollectionsApi.getMetrics();

// Search logs
const results = await logCollectionsApi.searchLogs(collectionId, {
    filters: [...],
    limit: 100,
    lastId: null
});

// Attributes
await logAttributesApi.create(attributeData);
await logAttributesApi.deleteById(attributeId);
```

**Error Handling:**
```javascript
import { ApiError } from './api/client.js';

try {
    const data = await logCollectionsApi.getAll();
} catch (error) {
    if (error instanceof ApiError) {
        console.log('Status:', error.status);
        console.log('Message:', error.message);
        console.log('Data:', error.data);
    }
}
```

---

### DOM Utilities

**File:** `utils/dom.js`

**Purpose:** Common DOM manipulation helpers

**Functions:**

#### escapeHtml(text)
Escape HTML to prevent XSS:
```javascript
import { escapeHtml } from './utils/dom.js';

const safe = escapeHtml(userInput);
element.innerHTML = safe;
```

#### announceToScreenReader(message, priority)
Announce to screen readers:
```javascript
import { announceToScreenReader } from './utils/dom.js';

announceToScreenReader('Form submitted successfully', 'polite');
announceToScreenReader('Error occurred', 'assertive');
```

#### focusElement(element, smooth)
Focus and scroll to element:
```javascript
import { focusElement } from './utils/dom.js';

focusElement(firstErrorField, true); // smooth scroll
```

#### trapFocus(container)
Trap focus within container (for modals):
```javascript
import { trapFocus } from './utils/dom.js';

const cleanup = trapFocus(modalElement);
// Later: cleanup(); // Remove focus trap
```

#### debounce(func, wait)
Debounce function calls:
```javascript
import { debounce } from './utils/dom.js';

const searchDebounced = debounce((query) => {
    performSearch(query);
}, 300);

inputElement.addEventListener('input', (e) => {
    searchDebounced(e.target.value);
});
```

#### throttle(func, limit)
Throttle function calls:
```javascript
import { throttle } from './utils/dom.js';

const scrollHandler = throttle(() => {
    updateScrollPosition();
}, 100);

window.addEventListener('scroll', scrollHandler);
```

---

### DateTime Utilities

**File:** `utils/datetime.js`

**Purpose:** Date and time formatting for the application

**Functions:**

#### formatDateTime(isoString)
Format ISO to dd/MM/yyyy HH:mm:
```javascript
import { formatDateTime } from './utils/datetime.js';

const formatted = formatDateTime('2026-04-30T14:30:00');
// Result: "30/04/2026 14:30"
```

#### dateTimeToISO(dateTimeStr)
Convert dd/MM/yyyy HH:mm to ISO:
```javascript
import { dateTimeToISO } from './utils/datetime.js';

const iso = dateTimeToISO('30/04/2026 14:30');
// Result: "2026-04-30T14:30:00"
```

#### applyDateTimeMask(input)
Apply input mask to datetime field:
```javascript
import { applyDateTimeMask } from './utils/datetime.js';

const input = document.getElementById('datetime-field');
applyDateTimeMask(input); // Formats as user types
```

#### isValidDateTime(dateTimeStr)
Validate datetime string:
```javascript
import { isValidDateTime } from './utils/datetime.js';

if (isValidDateTime(value)) {
    // Valid format
}
```

---

### Validation Utilities

**File:** `utils/validation.js`

**Purpose:** Form validation with accessibility

**Functions:**

#### validateField(field)
Validate single field:
```javascript
import { validateField } from './utils/validation.js';

const isValid = validateField(inputElement);
```

#### validateForm(form)
Validate all required fields:
```javascript
import { validateForm } from './utils/validation.js';

if (validateForm(formElement)) {
    // All fields valid, proceed
}
```

#### setFieldError(field, message)
Show error on field:
```javascript
import { setFieldError } from './utils/validation.js';

setFieldError(inputElement, 'This field is required');
```

#### clearFieldError(field)
Clear error from field:
```javascript
import { clearFieldError } from './utils/validation.js';

clearFieldError(inputElement);
```

#### displayValidationErrors(errorData, form)
Display API validation errors:
```javascript
import { displayValidationErrors } from './utils/validation.js';

try {
    await api.save(data);
} catch (error) {
    if (error.status === 400) {
        displayValidationErrors(error.data, formElement);
    }
}
```

#### setupLiveValidation(form)
Enable blur validation:
```javascript
import { setupLiveValidation } from './utils/validation.js';

setupLiveValidation(formElement);
// Now fields validate on blur
```

---

## Best Practices

### 1. Always Import at Top
```javascript
// Good
import { showSuccess } from './components/alert.js';
import { confirmDialog } from './components/modal.js';

function handleSave() {
    // Use imports
}

// Bad
function handleSave() {
    import { showSuccess } from './components/alert.js'; // Dynamic import
}
```

### 2. Use Async/Await for API Calls
```javascript
// Good
async function loadData() {
    try {
        const data = await logCollectionsApi.getAll();
        displayData(data);
    } catch (error) {
        showError(error.message);
    }
}

// Avoid
logCollectionsApi.getAll().then(data => {
    displayData(data);
}).catch(error => {
    showError(error.message);
});
```

### 3. Always Escape User Input
```javascript
import { escapeHtml } from './utils/dom.js';

// Good
element.innerHTML = escapeHtml(userInput);

// Dangerous
element.innerHTML = userInput; // XSS vulnerability
```

### 4. Use Confirmation for Destructive Actions
```javascript
import { confirmDelete } from './components/modal.js';

async function deleteItem(id, name) {
    const confirmed = await confirmDelete({
        itemName: name,
        onConfirm: async () => {
            await api.deleteById(id);
        }
    });

    if (confirmed) {
        showSuccess(`${name} deleted successfully`);
        reloadData();
    }
}
```

### 5. Validate Forms Before Submission
```javascript
import { validateForm } from './utils/validation.js';
import { displayValidationErrors } from './utils/validation.js';

async function handleSubmit(e) {
    e.preventDefault();

    if (!validateForm(formElement)) {
        return; // Client-side validation failed
    }

    try {
        await api.save(formData);
        showSuccess('Saved successfully');
    } catch (error) {
        if (error.status === 400) {
            displayValidationErrors(error.data, formElement);
        } else {
            showError(error.message);
        }
    }
}
```

---

## Accessibility Guidelines

All components follow WCAG 2.1 Level AA standards:

1. **Keyboard Navigation**
   - All interactive elements are keyboard accessible
   - Tab order is logical
   - Focus indicators are visible (2px outline)

2. **Screen Readers**
   - ARIA labels on icon buttons
   - Live regions for dynamic content
   - Proper semantic HTML

3. **Color Contrast**
   - All text meets 4.5:1 ratio
   - UI elements meet 3:1 ratio
   - Don't rely on color alone

4. **Touch Targets**
   - Minimum 44x44px on mobile
   - Adequate spacing between elements

---

## Performance Tips

1. **Lazy Load Page-Specific Modules**
   ```javascript
   // Only load when needed
   if (currentPage === 'search') {
       const { initializeSearch } = await import('./pages/searchLogs.js');
       initializeSearch();
   }
   ```

2. **Debounce Expensive Operations**
   ```javascript
   import { debounce } from './utils/dom.js';

   const expensiveSearch = debounce(async (query) => {
       const results = await api.search(query);
       displayResults(results);
   }, 300);
   ```

3. **Use Event Delegation**
   ```javascript
   // Good - one listener for all buttons
   tableElement.addEventListener('click', (e) => {
       if (e.target.matches('.delete-btn')) {
           handleDelete(e.target.dataset.id);
       }
   });

   // Avoid - listener per button
   document.querySelectorAll('.delete-btn').forEach(btn => {
       btn.addEventListener('click', handleDelete);
   });
   ```

---

## Migration Guide

### From Inline Scripts to Modules

**Before (inline in .cshtml):**
```html
<script>
    function showError(message) {
        // ...
    }

    function deleteCollection(id) {
        fetch(`/api/collections/${id}`, { method: 'DELETE' })
            .then(response => {
                if (response.ok) {
                    showSuccess('Deleted');
                }
            });
    }
</script>
```

**After (external module):**
```javascript
// pages/logCollections.js
import { showSuccess, showError } from '../components/alert.js';
import { confirmDelete } from '../components/modal.js';
import { logCollectionsApi } from '../api/client.js';

export async function deleteCollection(id, name) {
    const confirmed = await confirmDelete({
        itemName: name,
        onConfirm: async () => {
            await logCollectionsApi.deleteById(id);
        }
    });

    if (confirmed) {
        showSuccess(`${name} deleted successfully`);
        await loadCollections();
    }
}

export async function loadCollections() {
    try {
        const collections = await logCollectionsApi.getAll();
        displayCollections(collections);
    } catch (error) {
        showError('Failed to load collections');
    }
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', loadCollections);
```

**In .cshtml:**
```html
<script type="module" src="~/js/pages/logCollections.js"></script>
```

---

## Troubleshooting

### Module Not Found
**Error:** `Failed to resolve module specifier`

**Solution:** Use relative paths starting with `./` or `../`:
```javascript
// Correct
import { showSuccess } from './components/alert.js';

// Incorrect
import { showSuccess } from 'components/alert.js';
```

### CORS Errors
**Error:** `Access to module blocked by CORS policy`

**Solution:** Serve files from same origin or configure CORS headers

### Circular Dependencies
**Error:** Module import cycle

**Solution:** Refactor to break the cycle or use dynamic imports

---

## Additional Resources

- [MDN: JavaScript Modules](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Guide/Modules)
- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
- [Bootstrap 5 Documentation](https://getbootstrap.com/docs/5.0/)
- [Fetch API Reference](https://developer.mozilla.org/en-US/docs/Web/API/Fetch_API)

---

**Last Updated:** 2026-04-30
**Version:** 1.0.0
