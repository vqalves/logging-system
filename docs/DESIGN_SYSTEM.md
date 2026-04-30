# LogSystem Design System Documentation

**Version:** 1.0
**Last Updated:** April 30, 2026
**Status:** Production Ready

---

## Table of Contents

1. [Overview](#overview)
2. [Color System](#color-system)
3. [Typography](#typography)
4. [Spacing & Layout](#spacing--layout)
5. [Components](#components)
6. [Accessibility](#accessibility)
7. [Responsive Design](#responsive-design)
8. [Best Practices](#best-practices)

---

## Overview

The LogSystem design system is built on top of Bootstrap 5, with custom CSS variables providing a consistent, maintainable foundation for the entire application. This system prioritizes:

- **Consistency:** Unified visual language across all pages
- **Accessibility:** WCAG AA compliance for all users
- **Maintainability:** Centralized design tokens via CSS variables
- **Responsive:** Mobile-first approach with desktop optimization
- **Professional:** Clean, information-dense layouts for technical users

### Architecture

```
wwwroot/css/
├── variables.css      # Design tokens (colors, spacing, typography)
├── utilities.css      # Utility classes for common patterns
├── components.css     # Reusable component styles
├── layouts.css        # Page layout patterns
├── tables.css         # Enhanced table styling
├── forms.css          # Form controls and validation
├── accessibility.css  # WCAG AA compliance enhancements
└── site.css          # Main import file
```

---

## Color System

### Primary Colors

```css
--color-primary: #1b6ec2;         /* Main brand color */
--color-primary-hover: #1557a0;   /* Hover state */
--color-primary-active: #124a85;  /* Active/pressed state */
--color-primary-light: #e7f1fb;   /* Light backgrounds */
--color-primary-dark: #0f3d6b;    /* Dark variant */
```

**Usage:** Primary actions, links, brand elements
**Contrast Ratio:** 4.5:1 on white (WCAG AA compliant)

### Semantic Colors

#### Success (Green)
```css
--color-success: #198754;         /* Success states */
--color-success-hover: #157347;
--color-success-light: #d1e7dd;   /* Success backgrounds */
--color-success-dark: #0f5132;
```
**Usage:** Success messages, positive metrics, confirmation actions

#### Warning (Yellow/Orange)
```css
--color-warning: #ffc107;         /* Warning states */
--color-warning-hover: #e0a800;
--color-warning-light: #fff3cd;
--color-warning-dark: #997404;
```
**Usage:** Warning messages, cautionary information

#### Danger (Red)
```css
--color-danger: #dc3545;          /* Error/destructive states */
--color-danger-hover: #bb2d3b;
--color-danger-light: #f8d7da;
--color-danger-dark: #842029;
```
**Usage:** Error messages, destructive actions, failed states

#### Info (Cyan)
```css
--color-info: #0dcaf0;            /* Informational states */
--color-info-hover: #0aa2c0;
--color-info-light: #cff4fc;
--color-info-dark: #055160;
```
**Usage:** Informational messages, neutral highlights

### Neutral Grays

```css
--color-gray-100: #f8f9fa;  /* Lightest gray - backgrounds */
--color-gray-200: #e9ecef;  /* Light gray - borders, dividers */
--color-gray-300: #dee2e6;  /* Border color */
--color-gray-400: #ced4da;  /* Darker borders */
--color-gray-500: #adb5bd;  /* Muted elements */
--color-gray-600: #6c757d;  /* Secondary text */
--color-gray-700: #495057;  /* Body text (lighter) */
--color-gray-800: #343a40;  /* Dark gray */
--color-gray-900: #212529;  /* Primary text */
```

### Text Colors

```css
--color-text-primary: var(--color-gray-900);    /* Primary body text */
--color-text-secondary: var(--color-gray-700);  /* Secondary text */
--color-text-muted: var(--color-gray-600);      /* Muted/helper text */
--color-text-disabled: var(--color-gray-500);   /* Disabled state */
```

### Color Contrast Guidelines

All color combinations meet WCAG AA standards:
- **Normal text:** Minimum 4.5:1 contrast ratio
- **Large text (18px+):** Minimum 3:1 contrast ratio
- **UI components:** Minimum 3:1 contrast ratio

**Testing:** Use browser DevTools contrast analyzer or online tools like WebAIM Contrast Checker.

---

## Typography

### Font Families

```css
/* System Font Stack (Default) */
--font-family-base: -apple-system, BlinkMacSystemFont, "Segoe UI",
                    Roboto, "Helvetica Neue", Arial, "Noto Sans",
                    sans-serif, "Apple Color Emoji", "Segoe UI Emoji";

/* Monospace (for code/data) */
--font-family-monospace: SFMono-Regular, Menlo, Monaco, Consolas,
                         "Liberation Mono", "Courier New", monospace;
```

### Font Scale

```css
--font-size-xs: 0.75rem;    /* 12px - Captions, table notes */
--font-size-sm: 0.875rem;   /* 14px - Small text */
--font-size-base: 1rem;     /* 16px - Body text */
--font-size-lg: 1.125rem;   /* 18px - Lead text */
--font-size-xl: 1.25rem;    /* 20px - Subheadings */
--font-size-2xl: 1.5rem;    /* 24px - H3 */
--font-size-3xl: 1.875rem;  /* 30px - H2 */
--font-size-4xl: 2.25rem;   /* 36px - H1 */
```

### Heading Hierarchy

```html
<!-- Page Title (H1) -->
<h1 class="page-title">Log Collections</h1>

<!-- Section Title (H2) -->
<h2 class="section-title">Collections Overview</h2>

<!-- Subsection Title (H3) -->
<h3 class="h4 mb-3">Statistics</h3>

<!-- Card/Component Title (H4-H6) -->
<h4 class="h5">Quick Actions</h4>
```

### Font Weights

```css
--font-weight-normal: 400;      /* Regular text */
--font-weight-medium: 500;      /* Emphasis */
--font-weight-semibold: 600;    /* Subheadings, buttons */
--font-weight-bold: 700;        /* Headings, strong emphasis */
```

### Line Heights

```css
--line-height-tight: 1.2;       /* Headings */
--line-height-normal: 1.5;      /* Body text (optimal) */
--line-height-relaxed: 1.6;     /* Long-form content */
```

### Typography Best Practices

1. **Readability:** Limit line length to 65-90 characters (use `--reading-max-width: 65ch`)
2. **Hierarchy:** Use consistent heading levels (don't skip levels)
3. **Contrast:** Ensure 4.5:1 contrast for all body text
4. **Alignment:** Left-align text in LTR languages (no justified text)
5. **Responsive:** Base font size is 14px on mobile, 16px on desktop (768px+)

---

## Spacing & Layout

### Spacing Scale

Based on a 4px (0.25rem) base unit:

```css
--spacing-0: 0;
--spacing-1: 0.25rem;   /* 4px */
--spacing-2: 0.5rem;    /* 8px */
--spacing-3: 0.75rem;   /* 12px */
--spacing-4: 1rem;      /* 16px - Base element spacing */
--spacing-5: 1.25rem;   /* 20px */
--spacing-6: 1.5rem;    /* 24px - Component spacing */
--spacing-8: 2rem;      /* 32px - Section spacing */
--spacing-10: 2.5rem;   /* 40px */
--spacing-12: 3rem;     /* 48px */
--spacing-16: 4rem;     /* 64px */
```

### Semantic Spacing

```css
--spacing-section: 2rem;      /* Between major page sections */
--spacing-component: 1.5rem;  /* Between components */
--spacing-element: 1rem;      /* Between elements */
```

### Layout Patterns

#### Page Structure
```html
<div class="container">
    <main role="main" class="pb-3">
        <!-- Breadcrumb (optional) -->
        <nav class="breadcrumb-nav" aria-label="breadcrumb">...</nav>

        <!-- Page Header -->
        <div class="page-header-with-actions mb-4">
            <div class="page-header-content">
                <h1 class="page-title">Title</h1>
                <p class="page-subtitle">Subtitle</p>
            </div>
            <div class="page-header-actions">
                <a href="#" class="btn btn-primary">Action</a>
            </div>
        </div>

        <!-- Page Content -->
        <div class="section">
            <!-- Content here -->
        </div>
    </main>
</div>
```

#### Grid System
Uses Bootstrap 5 grid with standard breakpoints:
- **xs:** <576px (mobile)
- **sm:** ≥576px (large mobile)
- **md:** ≥768px (tablet)
- **lg:** ≥992px (desktop)
- **xl:** ≥1200px (large desktop)
- **xxl:** ≥1400px (wide desktop)

**Target viewport:** 1366-1920px (laptop/desktop)

### Container Widths

```css
--container-max-width: 1400px;   /* Max container width */
--content-max-width: 1200px;     /* Max content width */
--reading-max-width: 65ch;       /* Optimal reading width */
```

---

## Components

### Buttons

#### Variants
```html
<!-- Primary Action -->
<button class="btn btn-primary">Primary</button>

<!-- Secondary Action -->
<button class="btn btn-secondary">Secondary</button>

<!-- Success/Confirmation -->
<button class="btn btn-success">Success</button>

<!-- Destructive Action -->
<button class="btn btn-danger">Delete</button>

<!-- Outline Variants (less emphasis) -->
<button class="btn btn-outline-primary">Outline</button>
```

#### Sizes
```html
<button class="btn btn-lg btn-primary">Large</button>
<button class="btn btn-primary">Default</button>
<button class="btn btn-sm btn-primary">Small</button>
```

#### States
```html
<!-- Loading State -->
<button class="btn btn-primary" disabled>
    <span class="spinner-border spinner-border-sm me-2"></span>
    Loading...
</button>

<!-- Disabled State -->
<button class="btn btn-primary" disabled>Disabled</button>
```

### Cards

```html
<div class="card">
    <div class="card-header">
        <h3 class="h5 mb-0">Card Title</h3>
    </div>
    <div class="card-body">
        <p class="card-text">Card content goes here.</p>
    </div>
    <div class="card-footer text-muted">
        Optional footer
    </div>
</div>
```

### Alerts

```html
<div class="alert alert-success alert-dismissible" role="alert">
    <div class="alert-icon" role="img" aria-label="Success icon"></div>
    <div class="alert-content">Success message here</div>
    <button type="button" class="btn-close" aria-label="Close">
        <span aria-hidden="true">&times;</span>
    </button>
</div>
```

**Variants:** `alert-success`, `alert-danger`, `alert-warning`, `alert-info`

### Tables

```html
<div class="table-card">
    <div class="table-responsive">
        <table class="table table-hover">
            <thead>
                <tr>
                    <th class="col-id">ID</th>
                    <th class="col-expand">Name</th>
                    <th class="col-actions">Actions</th>
                </tr>
            </thead>
            <tbody>
                <tr>
                    <td>1</td>
                    <td>Item Name</td>
                    <td>
                        <button class="btn btn-sm btn-outline-primary">Edit</button>
                    </td>
                </tr>
            </tbody>
        </table>
    </div>
</div>
```

**Column Classes:**
- `col-id` - Fixed width for ID columns (~80px)
- `col-numeric` - Right-aligned numeric data
- `col-actions` - Fixed width for action buttons (~120px)
- `col-actions-lg` - Wider action column (~180px)
- `col-expand` - Flexible width (fills remaining space)

---

## Accessibility

### Keyboard Navigation

All interactive elements must be keyboard accessible:
- **Tab:** Navigate forward
- **Shift+Tab:** Navigate backward
- **Enter/Space:** Activate buttons/links
- **Esc:** Close modals/dropdowns

### Focus Indicators

```css
/* Visible focus outline for all interactive elements */
--shadow-focus: 0 0 0 0.1rem white, 0 0 0 0.25rem var(--color-primary);
```

**Requirements:**
- Minimum 2px outline
- High contrast (3:1 minimum)
- Visible on all interactive elements

### ARIA Labels

```html
<!-- Icon-only buttons -->
<button class="btn btn-primary" aria-label="Delete collection">
    <span aria-hidden="true">&times;</span>
</button>

<!-- Dynamic content -->
<div role="alert" aria-live="polite">
    <div class="alert alert-success">Operation successful</div>
</div>

<!-- Form labels -->
<label for="collection-name" class="form-label">
    Collection Name
    <span class="text-danger" aria-label="required">*</span>
</label>
<input type="text" id="collection-name" required>
```

### Screen Reader Support

- Use semantic HTML (`<nav>`, `<main>`, `<header>`, `<footer>`)
- Provide text alternatives for images
- Use proper heading hierarchy
- Label all form inputs
- Announce dynamic content changes

---

## Responsive Design

### Breakpoint Strategy

#### Desktop (≥1366px)
- Full multi-column layouts
- Side-by-side forms
- Visible secondary navigation
- Optimal for target users

#### Tablet (768-1365px)
- Reduced spacing
- Some columns stack
- Hamburger menu (below 992px)
- Touch-friendly targets (min 44x44px)

#### Mobile (<768px)
- Single column layout
- Full-width inputs
- Collapsible sections by default
- Horizontal scroll for wide tables
- Touch targets min 44x44px

### Mobile Optimizations

#### Tables
```html
<!-- Card view on mobile, table on desktop -->
<div class="table-responsive">
    <table class="table">...</table>
</div>
```

#### Forms
```html
<!-- Full-width inputs on mobile -->
<div class="row">
    <div class="col-md-6 mb-3">
        <input type="text" class="form-control">
    </div>
</div>
```

#### Navigation
The navbar automatically collapses below 992px with hamburger menu.

---

## Best Practices

### CSS Usage

1. **Use CSS Variables:**
   ```css
   /* Good */
   color: var(--color-text-primary);
   margin-bottom: var(--spacing-4);

   /* Avoid */
   color: #212529;
   margin-bottom: 1rem;
   ```

2. **Avoid Inline Styles:**
   ```html
   <!-- Good -->
   <div class="text-danger mb-3">Error</div>

   <!-- Avoid -->
   <div style="color: red; margin-bottom: 1rem;">Error</div>
   ```

3. **Use Utility Classes:**
   ```html
   <div class="d-flex justify-content-between align-items-center mb-4">
       <h2>Title</h2>
       <button class="btn btn-primary">Action</button>
   </div>
   ```

### Component Composition

1. **Use Razor Partials for Reusable Components:**
   ```razor
   @{
       ViewData["AlertType"] = "success";
       ViewData["AlertMessage"] = "Operation completed";
   }
   @await Html.PartialAsync("_Components/_Alert")
   ```

2. **Follow Established Patterns:**
   Look at existing pages (LogCollections.cshtml, SearchLogs.cshtml) for examples.

3. **Maintain Consistency:**
   Use the same spacing, button sizes, and layouts across similar pages.

### Performance

1. **Minimize Reflows:** Batch DOM updates
2. **Optimize Images:** Use appropriate formats and sizes
3. **Lazy Load:** Load data as needed (pagination, infinite scroll)
4. **Cache API Responses:** Reduce redundant network calls

### Testing Checklist

- [ ] Test on Chrome, Firefox, Safari, Edge
- [ ] Verify keyboard navigation
- [ ] Check color contrast (4.5:1 minimum)
- [ ] Test at 1366px, 1920px, 768px viewports
- [ ] Verify touch targets (44x44px minimum)
- [ ] Run Lighthouse audit (90+ scores)
- [ ] Test with screen reader (NVDA/JAWS)
- [ ] Validate HTML (W3C validator)

---

## Version History

**v1.0 (April 30, 2026)**
- Initial design system documentation
- Complete CSS variable system
- Reusable Razor components
- WCAG AA compliance
- Responsive design patterns

---

## Resources

- [Bootstrap 5 Documentation](https://getbootstrap.com/docs/5.0/)
- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
- [MDN Web Accessibility](https://developer.mozilla.org/en-US/docs/Web/Accessibility)
- [WebAIM Contrast Checker](https://webaim.org/resources/contrastchecker/)
- [A11y Project Checklist](https://www.a11yproject.com/checklist/)
