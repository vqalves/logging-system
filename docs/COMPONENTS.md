# LogSystem Component Library

**Version:** 1.0
**Last Updated:** April 30, 2026
**Location:** `/Pages/Shared/_Components/`

---

## Table of Contents

1. [Overview](#overview)
2. [Alert Component](#alert-component)
3. [DataTable Component](#datatable-component)
4. [FormGroup Component](#formgroup-component)
5. [PageHeader Component](#pageheader-component)
6. [ActionButtons Component](#actionbuttons-component)
7. [LoadingSpinner Component](#loadingspinner-component)
8. [EmptyState Component](#emptystate-component)
9. [Usage Examples](#usage-examples)

---

## Overview

The LogSystem component library provides reusable Razor partial views for common UI patterns. All components follow accessibility best practices (WCAG AA) and maintain consistent styling through the design system.

### Component Location

All reusable components are located in:
```
/Pages/Shared/_Components/
├── _Alert.cshtml
├── _DataTable.cshtml
├── _FormGroup.cshtml
├── _PageHeader.cshtml
├── _ActionButtons.cshtml
├── _LoadingSpinner.cshtml
└── _EmptyState.cshtml
```

### How to Use Components

Components receive configuration via `ViewData` dictionary. Set the required properties before rendering the partial:

```razor
@{
    ViewData["ComponentProperty"] = "value";
}
@await Html.PartialAsync("_Components/_ComponentName")
```

---

## Alert Component

**File:** `_Components/_Alert.cshtml`

Displays success, error, warning, or informational messages with optional auto-dismiss.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `AlertId` | string | "alert-message" | Unique ID for the alert element |
| `AlertType` | string | "info" | Alert type: `success`, `danger`, `warning`, `info` |
| `AlertMessage` | string | "" | Message text to display |
| `AlertDismissible` | bool | true | Show close button |
| `AlertVisible` | bool | false | Initial visibility state |

### Example

```razor
@{
    ViewData["AlertId"] = "success-message";
    ViewData["AlertType"] = "success";
    ViewData["AlertMessage"] = "Collection created successfully!";
    ViewData["AlertDismissible"] = true;
    ViewData["AlertVisible"] = false;
}
@await Html.PartialAsync("_Components/_Alert")
```

### JavaScript Usage

Show/hide alerts dynamically:

```javascript
// Show alert
const alert = document.getElementById('success-message');
document.getElementById('success-message-text').textContent = 'Operation successful';
alert.style.display = 'block';

// Hide alert
alert.style.display = 'none';
```

### Features

- Auto-dismisses success messages after 5 seconds
- ARIA roles for screen reader support
- Icon indicators for each alert type
- Dismissible with close button
- Keyboard accessible (Esc to close)

### Accessibility

- `role="alert"` for screen reader announcements
- `aria-label` for icon descriptions
- Visible close button with keyboard support

---

## DataTable Component

**File:** `_Components/_DataTable.cshtml`

Creates a responsive data table with optional loading states, empty states, and consistent styling.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `TableId` | string | "data-table" | Unique ID for the table element |
| `TableClass` | string | "" | Additional CSS classes (e.g., "table-hover") |
| `Columns` | string[] | [] | Array of column header names |
| `ColumnWidths` | string[] | [] | Array of column width classes (optional) |
| `ShowLoading` | bool | true | Show skeleton loading row initially |
| `EmptyMessage` | string | "No items found" | Message for empty table |
| `HeaderNote` | string | null | Optional note displayed above headers |

### Example

```razor
@{
    ViewData["TableId"] = "collections-table";
    ViewData["TableClass"] = "table-hover";
    ViewData["Columns"] = new[] { "ID", "Name", "Table Name", "Duration", "Actions" };
    ViewData["ColumnWidths"] = new[] { "col-id", "col-expand", "col-expand", "col-numeric", "col-actions" };
    ViewData["ShowLoading"] = true;
    ViewData["HeaderNote"] = "Real-time Metrics (60s window)";
}
@await Html.PartialAsync("_Components/_DataTable")
```

### Column Width Classes

- `col-id` - Fixed ~80px for ID columns
- `col-numeric` - Right-aligned numeric data
- `col-actions` - Fixed ~120px for action buttons
- `col-actions-lg` - Fixed ~180px for wider action buttons
- `col-expand` - Flexible width (fills space)

### JavaScript Population

```javascript
async function loadData() {
    const tbody = document.querySelector('#collections-table tbody');

    // Remove loading/empty row
    tbody.innerHTML = '';

    // Add data rows
    data.forEach(item => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td>${escapeHtml(item.id)}</td>
            <td>${escapeHtml(item.name)}</td>
            <td><button class="btn btn-sm btn-primary">Edit</button></td>
        `;
        tbody.appendChild(row);
    });
}
```

### Features

- Responsive horizontal scroll on mobile
- Skeleton loading state
- Empty state with custom message
- Optional header notes
- Accessible table structure

---

## FormGroup Component

**File:** `_Components/_FormGroup.cshtml`

Renders a complete form field with label, input, help text, and validation.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `FieldId` | string | "field" | Unique ID for the input element |
| `FieldLabel` | string | "" | Label text displayed above input |
| `FieldType` | string | "text" | Input type: `text`, `number`, `email`, `password`, `textarea`, `select` |
| `FieldValue` | string | "" | Current field value |
| `FieldPlaceholder` | string | "" | Placeholder text (use sparingly) |
| `FieldRequired` | bool | false | Mark field as required |
| `FieldDisabled` | bool | false | Disable the input |
| `FieldHelpText` | string | "" | Helper text below input |
| `FieldError` | string | "" | Validation error message |
| `FieldClass` | string | "" | Additional CSS classes for input |
| `SelectOptions` | string[] | [] | Options for select type |
| `FieldRows` | int | 4 | Rows for textarea type |

### Example - Text Input

```razor
@{
    ViewData["FieldId"] = "collectionName";
    ViewData["FieldLabel"] = "Collection Name";
    ViewData["FieldType"] = "text";
    ViewData["FieldRequired"] = true;
    ViewData["FieldPlaceholder"] = "e.g., application-logs";
    ViewData["FieldHelpText"] = "Unique name for this log collection";
}
@await Html.PartialAsync("_Components/_FormGroup")
```

### Example - Select Dropdown

```razor
@{
    ViewData["FieldId"] = "compression";
    ViewData["FieldLabel"] = "Compression Algorithm";
    ViewData["FieldType"] = "select";
    ViewData["FieldValue"] = "brotli";
    ViewData["SelectOptions"] = new[] { "none", "gzip", "brotli" };
    ViewData["FieldRequired"] = true;
}
@await Html.PartialAsync("_Components/_FormGroup")
```

### Example - Textarea

```razor
@{
    ViewData["FieldId"] = "description";
    ViewData["FieldLabel"] = "Description";
    ViewData["FieldType"] = "textarea";
    ViewData["FieldRows"] = 3;
    ViewData["FieldHelpText"] = "Optional description of this collection";
}
@await Html.PartialAsync("_Components/_FormGroup")
```

### Features

- Automatic ARIA associations
- Visual required indicator (*)
- Validation error display
- Help text support
- Disabled state styling
- Multiple input types

### Accessibility

- Label always associated with input (`for` attribute)
- `aria-describedby` for help text and errors
- `required` attribute for screen readers
- High contrast validation states

---

## PageHeader Component

**File:** `_Components/_PageHeader.cshtml`

Renders a consistent page header with optional breadcrumb, subtitle, and action buttons.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `PageTitle` | string | "Page" | Main page title (H1) |
| `PageSubtitle` | string | null | Optional subtitle below title |
| `ShowBreadcrumb` | bool | false | Display breadcrumb navigation |
| `BreadcrumbItems` | dynamic[] | null | Array of breadcrumb items |
| `HeaderActions` | bool | false | Use layout with action buttons |

### Breadcrumb Item Structure

```csharp
new { Text = "Home", Url = "/" }              // Link
new { Text = "Current Page", Url = (string)null }  // Active (no link)
```

### Example - Simple Header

```razor
@{
    ViewData["PageTitle"] = "Log Collections";
    ViewData["PageSubtitle"] = "Manage log collections and monitor metrics";
}
@await Html.PartialAsync("_Components/_PageHeader")
```

### Example - Header with Breadcrumb

```razor
@{
    ViewData["PageTitle"] = "Edit Collection";
    ViewData["PageSubtitle"] = "Modify collection settings";
    ViewData["ShowBreadcrumb"] = true;
    ViewData["BreadcrumbItems"] = new[] {
        new { Text = "Home", Url = "/" },
        new { Text = "Collections", Url = "/LogCollections" },
        new { Text = "Edit", Url = (string)null }
    };
}
@await Html.PartialAsync("_Components/_PageHeader")
```

### Example - Header with Actions

```razor
@{
    ViewData["PageTitle"] = "Log Collections";
    ViewData["PageSubtitle"] = "Manage collections";
    ViewData["HeaderActions"] = true;
}
@await Html.PartialAsync("_Components/_PageHeader")

@section PageHeaderActions {
    <a href="/LogCollections/Manage" class="btn btn-primary">Create Collection</a>
}
```

### Features

- Consistent page title styling
- Breadcrumb navigation with ARIA labels
- Flexible action button placement
- Responsive layout (stacks on mobile)

---

## ActionButtons Component

**File:** `_Components/_ActionButtons.cshtml`

Renders a button group with common actions (Edit, Delete, View, etc.) for table rows or cards.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EntityId` | string | null | Unique identifier for the entity |
| `EntityName` | string | "item" | Human-readable entity name (for confirmations) |
| `EditUrl` | string | null | URL for Edit action |
| `DeleteUrl` | string | null | API endpoint for Delete action |
| `ViewUrl` | string | null | URL for View action |
| `ManageAttributesUrl` | string | null | URL for Manage Attributes action |
| `CustomButtons` | dynamic[] | null | Array of custom button definitions |
| `OnDeleteSuccess` | string | "location.reload()" | JavaScript to execute after successful delete |
| `ButtonSize` | string | "sm" | Button size: `sm`, `md`, `lg` |

### Custom Button Structure

```csharp
new { Text = "Custom", Url = "/custom", Class = "btn-info", Icon = "icon-name" }
```

### Example - Basic Actions

```razor
@{
    ViewData["EntityId"] = collection.Id.ToString();
    ViewData["EntityName"] = collection.Name;
    ViewData["EditUrl"] = $"/LogCollections/Manage?id={collection.Id}";
    ViewData["DeleteUrl"] = $"/api/log-collections/{collection.Id}";
    ViewData["ManageAttributesUrl"] = $"/LogAttributes?collectionId={collection.Id}";
}
@await Html.PartialAsync("_Components/_ActionButtons")
```

### Example - Custom Callback

```razor
@{
    ViewData["EntityId"] = attribute.Id.ToString();
    ViewData["EntityName"] = attribute.Name;
    ViewData["EditUrl"] = $"/LogAttributes/Edit?id={attribute.Id}";
    ViewData["DeleteUrl"] = $"/api/log-attributes/{attribute.Id}";
    ViewData["OnDeleteSuccess"] = "refreshAttributesList()";
}
@await Html.PartialAsync("_Components/_ActionButtons")
```

### Features

- Consistent button group styling
- Confirmation dialog for delete actions
- Async DELETE API calls
- Error handling with user feedback
- Custom success callbacks
- ARIA labels for accessibility

### Delete Confirmation

The component automatically generates a confirmation dialog:
```
Are you sure you want to delete "Collection Name"? This action cannot be undone.
```

---

## LoadingSpinner Component

**File:** `_Components/_LoadingSpinner.cshtml`

Displays an animated loading spinner with optional message.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `SpinnerId` | string | "loading-spinner" | Unique ID for the spinner element |
| `SpinnerSize` | string | "md" | Spinner size: `sm`, `md`, `lg` |
| `SpinnerMessage` | string | "Loading..." | Optional message below spinner |
| `SpinnerVisible` | bool | false | Initial visibility state |

### Example

```razor
@{
    ViewData["SpinnerId"] = "data-loading";
    ViewData["SpinnerSize"] = "lg";
    ViewData["SpinnerMessage"] = "Loading collections...";
    ViewData["SpinnerVisible"] = true;
}
@await Html.PartialAsync("_Components/_LoadingSpinner")
```

### JavaScript Usage

```javascript
// Show spinner
document.getElementById('data-loading').style.display = 'block';

// Hide spinner
document.getElementById('data-loading').style.display = 'none';
```

### Features

- Three size variants
- Optional text message
- Accessible with `visually-hidden` label
- Smooth animation
- Centered layout

---

## EmptyState Component

**File:** `_Components/_EmptyState.cshtml`

Displays a helpful empty state when no data is available, with optional call-to-action.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EmptyStateId` | string | "empty-state" | Unique ID for the element |
| `EmptyStateTitle` | string | "No items found" | Main heading |
| `EmptyStateMessage` | string | null | Optional description text |
| `EmptyStateActionText` | string | null | Call-to-action button text |
| `EmptyStateActionUrl` | string | null | Call-to-action button URL |
| `EmptyStateVisible` | bool | true | Initial visibility state |

### Example

```razor
@{
    ViewData["EmptyStateId"] = "no-collections";
    ViewData["EmptyStateTitle"] = "No Collections Yet";
    ViewData["EmptyStateMessage"] = "Get started by creating your first log collection";
    ViewData["EmptyStateActionText"] = "Create Collection";
    ViewData["EmptyStateActionUrl"] = "/LogCollections/Manage";
}
@await Html.PartialAsync("_Components/_EmptyState")
```

### Features

- Centered layout
- Icon illustration
- Optional action button
- Helpful messaging
- Show/hide via JavaScript

---

## Usage Examples

### Complete Page Example

```razor
@page
@model MyPageModel
@{
    ViewData["Title"] = "My Page";
}

<!-- Page Header with Breadcrumb -->
@{
    ViewData["PageTitle"] = "My Page";
    ViewData["PageSubtitle"] = "Description of this page";
    ViewData["ShowBreadcrumb"] = true;
    ViewData["BreadcrumbItems"] = new[] {
        new { Text = "Home", Url = "/" },
        new { Text = "My Page", Url = (string)null }
    };
    ViewData["HeaderActions"] = true;
}
@await Html.PartialAsync("_Components/_PageHeader")

@section PageHeaderActions {
    <a href="/create" class="btn btn-primary">Create New</a>
}

<!-- Alert Messages -->
@{
    ViewData["AlertId"] = "error-message";
    ViewData["AlertType"] = "danger";
    ViewData["AlertVisible"] = false;
}
@await Html.PartialAsync("_Components/_Alert")

<!-- Data Table -->
<div class="section">
    <h2 class="section-title">Data List</h2>

    @{
        ViewData["TableId"] = "data-table";
        ViewData["Columns"] = new[] { "ID", "Name", "Actions" };
        ViewData["ColumnWidths"] = new[] { "col-id", "col-expand", "col-actions" };
    }
    @await Html.PartialAsync("_Components/_DataTable")
</div>

@section Scripts {
    <script src="~/js/pages/myPage.js"></script>
}
```

### Form Example

```razor
<form method="post" class="needs-validation" novalidate>
    <div class="section">
        <h2 class="section-title">Collection Details</h2>

        <div class="row">
            <div class="col-md-6">
                @{
                    ViewData["FieldId"] = "name";
                    ViewData["FieldLabel"] = "Collection Name";
                    ViewData["FieldType"] = "text";
                    ViewData["FieldRequired"] = true;
                    ViewData["FieldHelpText"] = "Unique identifier for this collection";
                }
                @await Html.PartialAsync("_Components/_FormGroup")
            </div>

            <div class="col-md-6">
                @{
                    ViewData["FieldId"] = "duration";
                    ViewData["FieldLabel"] = "Retention (Days)";
                    ViewData["FieldType"] = "number";
                    ViewData["FieldRequired"] = true;
                    ViewData["FieldValue"] = "30";
                }
                @await Html.PartialAsync("_Components/_FormGroup")
            </div>
        </div>
    </div>

    <div class="form-actions">
        <button type="submit" class="btn btn-primary">Save</button>
        <a href="/cancel" class="btn btn-secondary">Cancel</a>
    </div>
</form>
```

---

## Best Practices

### Component Selection

1. **Use components for repeated patterns** - Don't copy-paste markup
2. **Configure via ViewData** - Keep components flexible
3. **Follow accessibility guidelines** - All components are WCAG AA compliant
4. **Maintain consistency** - Use components the same way across pages

### Extending Components

To create a new component:

1. Create file in `/Pages/Shared/_Components/`
2. Document all properties in comment header
3. Use CSS variables from design system
4. Include ARIA labels and roles
5. Test keyboard navigation
6. Add to this documentation

### Testing Components

Before using a component:
- Test with required and optional properties
- Verify keyboard accessibility
- Check screen reader announcements
- Test on mobile viewports
- Validate HTML output

---

## Version History

**v1.0 (April 30, 2026)**
- Initial component library
- 7 core components created
- Full accessibility compliance
- Comprehensive documentation

---

## Support

For questions or issues with components:
1. Check this documentation
2. Review example usage in existing pages
3. Consult the Design System documentation
4. Check the Developer Guide for patterns

---

## Future Enhancements

Potential components for future versions:
- Pagination component
- Filter builder component
- Confirmation modal component
- Toast notification component
- Progress bar component
- File upload component
- Date picker component
