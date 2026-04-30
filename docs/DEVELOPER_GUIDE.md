# LogSystem Developer Guide

**Version:** 1.0
**Last Updated:** April 30, 2026

---

## Table of Contents

1. [Getting Started](#getting-started)
2. [Project Structure](#project-structure)
3. [Adding New Pages](#adding-new-pages)
4. [Form Patterns](#form-patterns)
5. [API Integration](#api-integration)
6. [Component Usage](#component-usage)
7. [Styling Guidelines](#styling-guidelines)
8. [JavaScript Patterns](#javascript-patterns)
9. [Common Tasks](#common-tasks)
10. [Troubleshooting](#troubleshooting)

---

## Getting Started

### Prerequisites

- .NET 6.0 or later
- Visual Studio 2022 / VS Code / Rider
- Node.js (for any future build tooling)
- Basic knowledge of ASP.NET Razor Pages
- Familiarity with Bootstrap 5

### Development Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd log-system
   ```

2. **Build the project**
   ```bash
   dotnet build
   ```

3. **Run the application**
   ```bash
   dotnet run --project src/LogSystem.WebApp
   ```

4. **Access the application**
   ```
   https://localhost:5001
   ```

### Development Tools

**Recommended Browser Extensions:**
- axe DevTools (accessibility testing)
- Lighthouse (performance/accessibility audits)
- React Developer Tools (if using React components)

**VS Code Extensions:**
- C# Dev Kit
- Razor Language Support
- ESLint
- Prettier
- CSS Variable Autocomplete

---

## Project Structure

```
LogSystem.WebApp/
├── Pages/                          # Razor Pages
│   ├── Shared/                     # Shared layouts and components
│   │   ├── _Components/            # Reusable Razor components
│   │   │   ├── _Alert.cshtml
│   │   │   ├── _DataTable.cshtml
│   │   │   ├── _FormGroup.cshtml
│   │   │   ├── _PageHeader.cshtml
│   │   │   ├── _ActionButtons.cshtml
│   │   │   ├── _LoadingSpinner.cshtml
│   │   │   └── _EmptyState.cshtml
│   │   ├── _Layout.cshtml          # Main layout
│   │   └── _ValidationScriptsPartial.cshtml
│   ├── Index.cshtml                # Home page
│   ├── LogCollections.cshtml       # Collections list
│   ├── LogCollections/
│   │   └── Manage.cshtml           # Create/edit collection
│   ├── LogAttributes.cshtml        # Attributes list
│   ├── LogAttributes/
│   │   ├── Manage.cshtml           # Create attribute
│   │   └── Edit.cshtml             # Edit attribute
│   └── SearchLogs.cshtml           # Search interface
├── wwwroot/                        # Static files
│   ├── css/                        # Stylesheets
│   │   ├── variables.css           # Design tokens
│   │   ├── utilities.css           # Utility classes
│   │   ├── components.css          # Component styles
│   │   ├── layouts.css             # Layout patterns
│   │   ├── tables.css              # Table styling
│   │   ├── forms.css               # Form styling
│   │   ├── accessibility.css       # A11y enhancements
│   │   └── site.css                # Main import file
│   ├── js/                         # JavaScript modules
│   │   ├── api/                    # API client modules
│   │   │   └── client.js
│   │   ├── components/             # JS components
│   │   │   ├── alert.js
│   │   │   ├── modal.js
│   │   │   ├── navigation.js
│   │   │   └── tooltip.js
│   │   ├── utils/                  # Utility functions
│   │   │   ├── datetime.js
│   │   │   ├── dom.js
│   │   │   └── validation.js
│   │   ├── pages/                  # Page-specific scripts
│   │   ├── main.js                 # Main entry point
│   │   └── site.js                 # Legacy scripts
│   └── lib/                        # Third-party libraries
│       ├── bootstrap/
│       └── jquery/
├── Endpoints/                      # API endpoints
│   └── *.cs
└── docs/                           # Documentation
    ├── DESIGN_SYSTEM.md
    ├── COMPONENTS.md
    ├── DEVELOPER_GUIDE.md
    └── TESTING_GUIDE.md
```

---

## Adding New Pages

### Step 1: Create Page Files

Create two files in the appropriate directory:

**Example:** `/Pages/MyFeature.cshtml`
```razor
@page
@model LogSystem.WebApp.Pages.MyFeatureModel
@{
    ViewData["Title"] = "My Feature";
}

<!-- Page content here -->
```

**Example:** `/Pages/MyFeature.cshtml.cs`
```csharp
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LogSystem.WebApp.Pages
{
    public class MyFeatureModel : PageModel
    {
        public void OnGet()
        {
            // Page initialization
        }
    }
}
```

### Step 2: Use Page Template

Follow this standard page structure:

```razor
@page
@model LogSystem.WebApp.Pages.MyFeatureModel
@{
    ViewData["Title"] = "My Feature";
}

<!-- Breadcrumb Navigation (optional) -->
<nav class="breadcrumb-nav" aria-label="breadcrumb">
    <ol class="breadcrumb">
        <li class="breadcrumb-item"><a href="/">Home</a></li>
        <li class="breadcrumb-item active" aria-current="page">My Feature</li>
    </ol>
</nav>

<!-- Page Header -->
<div class="page-header-with-actions mb-4">
    <div class="page-header-content">
        <h1 class="page-title">My Feature</h1>
        <p class="page-subtitle">Description of this feature</p>
    </div>
    <div class="page-header-actions">
        <a href="/create" class="btn btn-primary">Primary Action</a>
    </div>
</div>

<!-- Alert Messages -->
<div id="error-message" class="alert alert-danger alert-dismissible" role="alert" style="display: none;">
    <div class="alert-icon" role="img" aria-label="Error icon"></div>
    <div class="alert-content" id="error-message-text"></div>
    <button type="button" class="btn-close" aria-label="Close alert" onclick="document.getElementById('error-message').style.display='none'">
        <span aria-hidden="true">&times;</span>
    </button>
</div>

<!-- Main Content -->
<div class="section">
    <div class="section-header">
        <h2 class="section-title">Section Title</h2>
        <p class="section-subtitle">Section description</p>
    </div>

    <!-- Content here -->
</div>

<!-- Page Scripts -->
@section Scripts {
    <script>
        // Page-specific JavaScript
    </script>
}
```

### Step 3: Add Navigation Link

Update `/Pages/Shared/_Layout.cshtml`:

```html
<li class="nav-item">
    <a class="nav-link" asp-page="/MyFeature" id="nav-myfeature" aria-label="My Feature">My Feature</a>
</li>
```

### Step 4: Update Active Navigation State

Add JavaScript to highlight active navigation item (in site.js or page script):

```javascript
document.addEventListener('DOMContentLoaded', function() {
    // Set active navigation state
    const currentPath = window.location.pathname;
    if (currentPath.includes('/MyFeature')) {
        document.getElementById('nav-myfeature')?.classList.add('active');
    }
});
```

---

## Form Patterns

### Basic Form Structure

```razor
<form method="post" class="needs-validation" novalidate onsubmit="return handleSubmit(event)">
    <div class="section">
        <h2 class="section-title">Form Section</h2>

        <div class="row">
            <div class="col-md-6 mb-3">
                <label for="fieldName" class="form-label">
                    Field Name
                    <span class="text-danger" aria-label="required">*</span>
                </label>
                <input
                    type="text"
                    id="fieldName"
                    name="fieldName"
                    class="form-control"
                    required
                    aria-describedby="fieldName-help">
                <small id="fieldName-help" class="form-text text-muted">
                    Help text for this field
                </small>
                <div class="invalid-feedback">
                    This field is required
                </div>
            </div>
        </div>
    </div>

    <div class="form-actions">
        <button type="submit" class="btn btn-primary" id="submit-btn">
            <span class="btn-text">Save</span>
            <span class="spinner-border spinner-border-sm ms-2 d-none" role="status">
                <span class="visually-hidden">Saving...</span>
            </span>
        </button>
        <a href="/cancel" class="btn btn-secondary">Cancel</a>
    </div>
</form>

@section Scripts {
    <script>
        async function handleSubmit(event) {
            event.preventDefault();

            const form = event.target;
            if (!form.checkValidity()) {
                form.classList.add('was-validated');
                return false;
            }

            const submitBtn = document.getElementById('submit-btn');
            const btnText = submitBtn.querySelector('.btn-text');
            const spinner = submitBtn.querySelector('.spinner-border');

            // Show loading state
            submitBtn.disabled = true;
            btnText.textContent = 'Saving...';
            spinner.classList.remove('d-none');

            try {
                const formData = new FormData(form);
                const data = Object.fromEntries(formData);

                const response = await fetch('/api/endpoint', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(data)
                });

                if (!response.ok) {
                    throw new Error('Save failed');
                }

                // Success
                window.location.href = '/success-page';

            } catch (error) {
                // Show error
                showError('Failed to save: ' + error.message);

                // Reset button
                submitBtn.disabled = false;
                btnText.textContent = 'Save';
                spinner.classList.add('d-none');
            }

            return false;
        }

        function showError(message) {
            const errorDiv = document.getElementById('error-message');
            document.getElementById('error-message-text').textContent = message;
            errorDiv.style.display = 'block';
        }
    </script>
}
```

### Using FormGroup Component

```razor
@{
    ViewData["FieldId"] = "collectionName";
    ViewData["FieldLabel"] = "Collection Name";
    ViewData["FieldType"] = "text";
    ViewData["FieldRequired"] = true;
    ViewData["FieldHelpText"] = "Unique identifier for this collection";
}
@await Html.PartialAsync("_Components/_FormGroup")
```

### Validation Patterns

**Client-Side Validation:**
```javascript
function validateForm(formData) {
    const errors = [];

    if (!formData.name || formData.name.trim().length === 0) {
        errors.push('Name is required');
    }

    if (formData.name.length > 100) {
        errors.push('Name must be less than 100 characters');
    }

    if (!/^[a-zA-Z0-9-_]+$/.test(formData.name)) {
        errors.push('Name can only contain letters, numbers, hyphens, and underscores');
    }

    return errors;
}
```

**Server-Side Validation:**
```csharp
public class MyFormModel : PageModel
{
    [BindProperty]
    [Required]
    [StringLength(100)]
    [RegularExpression(@"^[a-zA-Z0-9-_]+$")]
    public string Name { get; set; }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Process form
        return RedirectToPage("/Success");
    }
}
```

---

## API Integration

### Fetch API Pattern

**GET Request:**
```javascript
async function loadData() {
    try {
        const response = await fetch('/api/collections');

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        const data = await response.json();
        return data;

    } catch (error) {
        console.error('Failed to load data:', error);
        showError('Failed to load data: ' + error.message);
        throw error;
    }
}
```

**POST Request:**
```javascript
async function createItem(itemData) {
    try {
        const response = await fetch('/api/collections', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(itemData)
        });

        if (!response.ok) {
            const errorData = await response.json().catch(() => null);
            throw new Error(errorData?.message || `HTTP ${response.status}`);
        }

        const result = await response.json();
        showSuccess('Item created successfully');
        return result;

    } catch (error) {
        console.error('Failed to create item:', error);
        showError('Failed to create item: ' + error.message);
        throw error;
    }
}
```

**DELETE Request:**
```javascript
async function deleteItem(id) {
    if (!confirm('Are you sure you want to delete this item?')) {
        return;
    }

    try {
        const response = await fetch(`/api/collections/${id}`, {
            method: 'DELETE'
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }

        showSuccess('Item deleted successfully');
        location.reload();

    } catch (error) {
        console.error('Failed to delete item:', error);
        showError('Failed to delete item: ' + error.message);
    }
}
```

### Error Handling

**Standard Error Response:**
```json
{
    "message": "Human-readable error message",
    "code": "ERROR_CODE",
    "details": {}
}
```

**Error Display:**
```javascript
function showError(message) {
    const errorDiv = document.getElementById('error-message');
    const errorText = document.getElementById('error-message-text');
    errorText.textContent = message;
    errorDiv.style.display = 'block';

    // Auto-hide after 10 seconds for non-critical errors
    setTimeout(() => {
        errorDiv.style.display = 'none';
    }, 10000);
}

function showSuccess(message) {
    const successDiv = document.getElementById('success-message');
    const successText = document.getElementById('success-message-text');
    successText.textContent = message;
    successDiv.style.display = 'block';

    // Auto-hide after 5 seconds
    setTimeout(() => {
        successDiv.style.display = 'none';
    }, 5000);
}
```

---

## Component Usage

### When to Create a Component

Create a reusable component when:
1. Pattern is used on 3+ pages
2. Complex markup that needs consistency
3. Interactive behavior that's repeated
4. Accessibility requirements are complex

### Component Checklist

- [ ] Component file in `/Pages/Shared/_Components/`
- [ ] Documentation comment at top of file
- [ ] All properties documented
- [ ] Default values provided
- [ ] ARIA labels and roles
- [ ] Keyboard accessibility tested
- [ ] Responsive design verified
- [ ] Added to COMPONENTS.md documentation

### Example Component Creation

```razor
@*
    Reusable Badge Component

    Usage:
    @{
        ViewData["BadgeText"] = "Active";
        ViewData["BadgeType"] = "success";
    }
    @await Html.PartialAsync("_Components/_Badge")
*@

@{
    var text = ViewData["BadgeText"]?.ToString() ?? "";
    var type = ViewData["BadgeType"]?.ToString() ?? "secondary";
}

<span class="badge bg-@type">@text</span>
```

---

## Styling Guidelines

### CSS Variable Usage

**Always use CSS variables:**
```css
/* Good */
.my-component {
    color: var(--color-text-primary);
    margin-bottom: var(--spacing-4);
    border-radius: var(--border-radius-base);
}

/* Avoid */
.my-component {
    color: #212529;
    margin-bottom: 1rem;
    border-radius: 4px;
}
```

### Utility Classes

**Use Bootstrap utilities when possible:**
```html
<!-- Good -->
<div class="d-flex justify-content-between align-items-center mb-3">
    <h2 class="h4 mb-0">Title</h2>
    <button class="btn btn-primary">Action</button>
</div>

<!-- Avoid custom CSS for common patterns -->
<div class="custom-flex-header">...</div>
```

### Component-Specific Styles

**Add to appropriate CSS file:**
```css
/* components.css - for new component styles */
.my-new-component {
    padding: var(--spacing-4);
    background: var(--color-bg-secondary);
    border-radius: var(--border-radius-lg);
}

.my-new-component__title {
    font-size: var(--font-size-lg);
    font-weight: var(--font-weight-semibold);
    margin-bottom: var(--spacing-2);
}
```

### Naming Conventions

**BEM methodology for custom components:**
```css
/* Block */
.card-stats { }

/* Element */
.card-stats__value { }
.card-stats__label { }

/* Modifier */
.card-stats--highlighted { }
```

---

## JavaScript Patterns

### Module Structure

**Create page-specific modules:**
```javascript
// /wwwroot/js/pages/myPage.js

import { showError, showSuccess } from '../components/alert.js';
import { formatDateTime } from '../utils/datetime.js';

export async function initializePage() {
    await loadData();
    setupEventListeners();
}

async function loadData() {
    // Implementation
}

function setupEventListeners() {
    // Implementation
}

// Initialize on DOMContentLoaded
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializePage);
} else {
    initializePage();
}
```

### Utility Functions

**Always escape HTML:**
```javascript
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Usage
const row = document.createElement('tr');
row.innerHTML = `
    <td>${escapeHtml(item.name)}</td>
    <td>${escapeHtml(item.description)}</td>
`;
```

**Format dates consistently:**
```javascript
function formatDateTime(isoString) {
    const date = new Date(isoString);
    return date.toLocaleString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });
}
```

---

## Common Tasks

### Adding a New API Endpoint

1. **Create endpoint class in `/Endpoints/`**
2. **Follow existing patterns** (see `/Endpoints/CreateOrUpdateLogCollectionEndpoint.cs`)
3. **Add request/response classes if needed**
4. **Register in Program.cs** (if required)
5. **Document API contract**

### Adding a Table Column

1. **Update column headers in `.cshtml`**
2. **Update column widths array**
3. **Update JavaScript row rendering**
4. **Test responsive behavior**

### Adding Form Validation

1. **Add HTML5 validation attributes**
2. **Add client-side validation logic**
3. **Add server-side validation (PageModel)**
4. **Display validation errors**
5. **Test all error paths**

---

## Troubleshooting

### Common Issues

**Issue:** CSS changes not appearing
- **Solution:** Clear browser cache, verify CSS file import order in site.css

**Issue:** Component not rendering
- **Solution:** Check ViewData property names match exactly, verify partial path

**Issue:** JavaScript errors in console
- **Solution:** Check for syntax errors, verify all dependencies loaded, check async/await usage

**Issue:** Form validation not working
- **Solution:** Verify form has `novalidate` attribute, check JavaScript validation function

**Issue:** API calls failing
- **Solution:** Check network tab, verify endpoint URL, check request format, review server logs

### Debugging Tips

1. **Use browser DevTools:** Console, Network, Elements tabs
2. **Check server logs:** Look for exceptions and error messages
3. **Verify API responses:** Use Network tab to inspect request/response
4. **Test incrementally:** Add functionality piece by piece
5. **Use console.log:** Log variables and execution flow

---

## Best Practices Summary

1. **Follow existing patterns** - Look at similar pages for examples
2. **Use components** - Don't duplicate markup
3. **Validate inputs** - Both client and server side
4. **Handle errors** - Always catch and display errors to users
5. **Test accessibility** - Keyboard navigation, screen readers, contrast
6. **Document changes** - Update relevant documentation
7. **Test responsive** - Check mobile, tablet, desktop viewports
8. **Use CSS variables** - Never hardcode design values
9. **Keep JavaScript modular** - Small, focused functions
10. **Review before commit** - Check formatting, remove console.logs, test functionality

---

## Resources

- [ASP.NET Core Razor Pages Documentation](https://docs.microsoft.com/en-us/aspnet/core/razor-pages/)
- [Bootstrap 5 Documentation](https://getbootstrap.com/docs/5.0/)
- [MDN Web Docs](https://developer.mozilla.org/)
- [LogSystem Design System](DESIGN_SYSTEM.md)
- [LogSystem Component Library](COMPONENTS.md)
- [LogSystem Testing Guide](TESTING_GUIDE.md)
