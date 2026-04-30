# Step 6: Quick Reference Guide

## Alert System Usage

### Auto-Dismiss Behavior

| Alert Type | Auto-Dismiss | Duration | Use Case |
|------------|--------------|----------|----------|
| Success    | ✅ Yes       | 3 seconds | Successful operations (save, delete, update) |
| Info       | ✅ Yes       | 5 seconds | Informational messages, tips |
| Warning    | ❌ No        | Manual   | Important warnings requiring attention |
| Error      | ❌ No        | Manual   | Errors requiring user action |

### JavaScript Functions

```javascript
// Show alerts programmatically
showSuccess("Record saved successfully!");
showError("Failed to save record. Please try again.");
showWarning("This action cannot be undone.");
showInfo("Tip: Use Ctrl+S to save quickly.");

// Generic alert function
showAlert("Your message", "success|danger|warning|info");

// Dismiss an alert manually
dismissAlert(alertElement);
```

### HTML Structure

```html
<!-- Success Alert (auto-dismisses in 3s) -->
<div class="alert alert-success alert-dismissible" role="alert">
    <div class="alert-icon" role="img" aria-label="Success icon"></div>
    <div class="alert-content">Operation completed successfully!</div>
    <button type="button" class="btn-close" aria-label="Close alert">
        <span aria-hidden="true">&times;</span>
    </button>
</div>

<!-- Error Alert (manual dismiss) -->
<div class="alert alert-danger alert-dismissible" role="alert">
    <div class="alert-icon" role="img" aria-label="Error icon"></div>
    <div class="alert-content">An error occurred. Please try again.</div>
    <button type="button" class="btn-close" aria-label="Close alert">
        <span aria-hidden="true">&times;</span>
    </button>
</div>
```

---

## Card Component Patterns

### Standard Card

```html
<div class="card">
    <div class="card-header">
        <h2 class="h5 mb-0">Card Title</h2>
    </div>
    <div class="card-body">
        <!-- Content with 1.5rem padding -->
        <p>Card content goes here.</p>
    </div>
    <div class="card-footer">
        <button class="btn btn-primary">Action</button>
    </div>
</div>
```

### Card with Actions in Header

```html
<div class="card">
    <div class="card-header d-flex justify-content-between align-items-center">
        <h2 class="h5 mb-0">Results</h2>
        <span class="badge bg-secondary">10 records</span>
    </div>
    <div class="card-body">
        <!-- Content -->
    </div>
</div>
```

### Compact Card (smaller padding)

```html
<div class="card">
    <div class="card-body card-body-sm">
        <!-- Content with 1rem padding -->
    </div>
</div>
```

---

## Page Layout Patterns

### Pattern 1: Simple Page Header

```html
<div class="page-title-wrapper">
    <h1 class="page-title">Page Title</h1>
    <p class="page-subtitle">Brief description of the page</p>
</div>
```

### Pattern 2: Page Header with Actions

```html
<div class="page-header-with-actions mb-4">
    <div class="page-header-content">
        <h1 class="page-title">Page Title</h1>
        <p class="page-subtitle">Description</p>
    </div>
    <div class="page-header-actions">
        <a href="#" class="btn btn-outline-secondary">Secondary</a>
        <a href="#" class="btn btn-primary">Primary Action</a>
    </div>
</div>
```

### Pattern 3: Breadcrumb Navigation

```html
<nav class="breadcrumb-nav" aria-label="breadcrumb">
    <ol class="breadcrumb">
        <li class="breadcrumb-item"><a href="/">Home</a></li>
        <li class="breadcrumb-item"><a href="/LogCollections">Log Collections</a></li>
        <li class="breadcrumb-item active" aria-current="page">Manage</li>
    </ol>
</nav>
```

### Pattern 4: Section Organization

```html
<div class="section">
    <div class="section-header">
        <h2 class="section-title">Section Title</h2>
        <p class="section-subtitle">Optional description</p>
    </div>
    <!-- Section content -->
</div>
```

---

## CSS Custom Properties (Variables)

### Card-Related Variables

```css
--card-padding: 1.5rem;           /* Standard card body padding */
--card-padding-sm: 1rem;          /* Compact card padding */
--card-border-radius: 0.5rem;     /* 8px border radius */
```

### Alert-Related Variables

```css
--color-success: #198754;         /* Success green */
--color-danger: #dc3545;          /* Error red */
--color-warning: #ffc107;         /* Warning yellow */
--color-info: #0dcaf0;            /* Info cyan */
--transition-fast: 150ms;         /* Alert animations */
```

### Spacing Variables

```css
--spacing-4: 1rem;               /* 16px - standard spacing */
--spacing-6: 1.5rem;             /* 24px - component spacing */
--spacing-8: 2rem;               /* 32px - section spacing */
```

---

## Accessibility Checklist

### Alert Accessibility
- [x] `role="alert"` on alert containers
- [x] `aria-live="assertive"` for errors
- [x] `aria-live="polite"` for success/info
- [x] `aria-label` on close buttons
- [x] Keyboard accessible close button
- [x] Screen reader announcements
- [x] Sufficient color contrast (4.5:1 minimum)

### Card Accessibility
- [x] Semantic HTML structure
- [x] Proper heading hierarchy
- [x] Focus indicators on interactive elements
- [x] Touch targets ≥44×44px on mobile

### General
- [x] Logical tab order
- [x] ARIA labels where needed
- [x] Color contrast compliance
- [x] Keyboard navigation support

---

## File Locations

### CSS Files
- `/wwwroot/css/variables.css` - Design tokens and CSS custom properties
- `/wwwroot/css/components.css` - Card and alert component styles
- `/wwwroot/css/layouts.css` - Page layout patterns

### JavaScript Files
- `/wwwroot/js/site.js` - Global JavaScript including alert system

### Page Files
- `/Pages/Index.cshtml` - Home page with feature cards
- `/Pages/LogCollections.cshtml` - Collections list with alerts
- `/Pages/SearchLogs.cshtml` - Search page with filters and results
- `/Pages/LogAttributes.cshtml` - Attributes table page
- `/Pages/LogCollections/Manage.cshtml` - Form page with validation alerts

---

## Common Use Cases

### 1. Show Success After Save

```javascript
// In your save function
try {
    const response = await fetch('/api/endpoint', {
        method: 'POST',
        body: JSON.stringify(data)
    });

    if (response.ok) {
        showSuccess('Record saved successfully!');
        // Auto-dismisses after 3 seconds
    }
} catch (error) {
    showError('Failed to save: ' + error.message);
    // Requires manual dismiss
}
```

### 2. Show Warning Before Destructive Action

```javascript
async function deleteItem(id, name) {
    // Use native confirm for blocking confirmation
    if (!confirm(`Are you sure you want to delete "${name}"?`)) {
        return;
    }

    try {
        const response = await fetch(`/api/items/${id}`, { method: 'DELETE' });
        if (response.ok) {
            showSuccess(`"${name}" deleted successfully.`);
        }
    } catch (error) {
        showError('Failed to delete item: ' + error.message);
    }
}
```

### 3. Show Info Tip

```javascript
// Show helpful tip when user enters a page
document.addEventListener('DOMContentLoaded', function() {
    showInfo('Tip: Use the search box to filter results quickly.');
    // Auto-dismisses after 5 seconds
});
```

---

## Migration Notes

If you have existing alert code, update it to the new structure:

### Before (Old Structure)
```html
<div id="error-message" class="alert alert-danger" style="display: none;"></div>

<script>
function showError(message) {
    const errorDiv = document.getElementById('error-message');
    errorDiv.textContent = message;
    errorDiv.style.display = 'block';
}
</script>
```

### After (New Structure)
```html
<div id="error-message" class="alert alert-danger alert-dismissible" role="alert" style="display: none;">
    <div class="alert-icon" role="img" aria-label="Error icon"></div>
    <div class="alert-content" id="error-message-text"></div>
    <button type="button" class="btn-close" aria-label="Close alert" onclick="document.getElementById('error-message').style.display='none'">
        <span aria-hidden="true">&times;</span>
    </button>
</div>

<script>
function showError(message) {
    const errorDiv = document.getElementById('error-message');
    const errorText = document.getElementById('error-message-text');
    errorText.textContent = message;
    errorDiv.style.display = 'flex';  // Use flex, not block
}
</script>
```

---

## Browser Support

- Chrome/Edge 90+ ✅
- Firefox 88+ ✅
- Safari 14+ ✅
- Mobile browsers (iOS Safari, Chrome Android) ✅

All features use standard Web APIs with no polyfills required.

---

**Last Updated:** 2026-04-30
**Related:** DESIGN_IMPROVEMENTS.md (Step 6)
