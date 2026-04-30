# LogSystem Testing Guide

**Version:** 1.0
**Last Updated:** April 30, 2026

---

## Table of Contents

1. [Testing Overview](#testing-overview)
2. [Browser Testing](#browser-testing)
3. [Responsive Testing](#responsive-testing)
4. [Accessibility Testing](#accessibility-testing)
5. [Performance Testing](#performance-testing)
6. [Functional Testing](#functional-testing)
7. [Testing Checklist](#testing-checklist)
8. [Tools & Resources](#tools--resources)

---

## Testing Overview

### Testing Philosophy

The LogSystem testing approach prioritizes:
1. **Accessibility First** - WCAG AA compliance on all pages
2. **Real User Testing** - Test with actual browsers and devices
3. **Progressive Enhancement** - Core functionality works without JavaScript
4. **Cross-Browser Support** - Chrome, Firefox, Safari, Edge
5. **Performance** - Fast load times, responsive interactions

### Testing Levels

**Level 1: Developer Testing** (Required before PR)
- Manual testing in Chrome at 1366px
- Keyboard navigation check
- Basic accessibility review
- Console error check

**Level 2: QA Testing** (Before deployment)
- All browsers (Chrome, Firefox, Safari, Edge)
- Multiple viewports (mobile, tablet, desktop)
- Accessibility audit with tools
- Performance testing

**Level 3: User Acceptance Testing** (Production)
- Real user feedback
- Analytics monitoring
- Error tracking
- Performance monitoring

---

## Browser Testing

### Supported Browsers

**Primary (Tier 1):**
- Chrome/Chromium (latest 2 versions)
- Firefox (latest 2 versions)
- Safari (latest 2 versions)
- Edge (latest 2 versions)

**Secondary (Tier 2):**
- Chrome (1 year old)
- Firefox ESR
- Safari (1 year old)

**Not Supported:**
- Internet Explorer (any version)
- Browsers older than 2 years

### Browser Testing Checklist

Test each major browser at the following viewports:
- [ ] 1920px (large desktop)
- [ ] 1366px (laptop - target viewport)
- [ ] 768px (tablet)

#### Chrome Testing

**Version:** Latest stable

**Focus Areas:**
- Primary development browser
- Verify all features work correctly
- Use DevTools for debugging
- Check console for errors
- Verify Service Worker (if applicable)

**Tools:**
- Chrome DevTools
- Lighthouse
- axe DevTools extension

#### Firefox Testing

**Version:** Latest stable

**Focus Areas:**
- CSS Grid/Flexbox compatibility
- Form validation behavior
- Fetch API calls
- Console errors

**Tools:**
- Firefox Developer Tools
- Accessibility Inspector

#### Safari Testing

**Version:** Latest stable (macOS/iOS)

**Focus Areas:**
- CSS compatibility (especially modern features)
- Date input styling
- Smooth scrolling
- Touch interactions (iOS)

**Known Issues:**
- Date pickers look different
- Some CSS features need -webkit- prefix
- Flexbox behavior slightly different

#### Edge Testing

**Version:** Latest stable (Chromium-based)

**Focus Areas:**
- Similar to Chrome testing
- Windows-specific issues
- Touch screen support

### Cross-Browser Testing Process

1. **Visual Review**
   - Check layout and spacing
   - Verify fonts render correctly
   - Check colors and contrast
   - Verify images load

2. **Interaction Testing**
   - Click all buttons and links
   - Submit forms
   - Test dropdowns and modals
   - Verify hover states

3. **JavaScript Functionality**
   - API calls succeed
   - Dynamic content loads
   - Error messages display
   - Loading states work

4. **Console Review**
   - No JavaScript errors
   - No CSS warnings
   - No network failures
   - No console spam

---

## Responsive Testing

### Target Viewports

**Primary Viewports:**
1. **1366px** - Standard laptop (PRIMARY TARGET)
2. **1920px** - Desktop monitor
3. **768px** - Tablet portrait

**Secondary Viewports:**
4. **375px** - Mobile (iPhone SE, older Android)
5. **414px** - Mobile (iPhone Plus, newer Android)
6. **1024px** - Tablet landscape

### Responsive Testing Checklist

#### Desktop (≥1366px)

- [ ] Multi-column layouts display correctly
- [ ] Tables show all columns
- [ ] Forms use side-by-side layout
- [ ] Navigation fully expanded
- [ ] Touch targets appropriate size
- [ ] No horizontal scroll
- [ ] Text readable (not too small)
- [ ] Whitespace balanced

#### Tablet (768-1365px)

- [ ] Columns stack appropriately
- [ ] Tables remain usable (scroll if needed)
- [ ] Navigation collapses at 992px
- [ ] Forms adjust to single column when needed
- [ ] Touch targets min 44x44px
- [ ] Images scale properly
- [ ] Modals fit viewport

#### Mobile (<768px)

- [ ] Single column layout
- [ ] Hamburger menu works
- [ ] Forms full width
- [ ] Tables scroll horizontally OR convert to cards
- [ ] Buttons full width (if appropriate)
- [ ] Touch targets min 44x44px
- [ ] Text remains readable
- [ ] No pinch-zoom required for content
- [ ] Modals fit viewport

### Testing Procedure

**Using Browser DevTools:**

1. **Open DevTools** (F12 or Cmd+Opt+I)
2. **Toggle Device Toolbar** (Ctrl+Shift+M or Cmd+Shift+M)
3. **Test Each Viewport:**
   ```
   - Select preset (e.g., "iPad", "iPhone 12 Pro")
   - Or enter custom dimensions
   - Test portrait and landscape
   ```

4. **Verify Breakpoints:**
   ```
   - 576px (sm)
   - 768px (md)
   - 992px (lg)
   - 1200px (xl)
   - 1400px (xxl)
   ```

5. **Test Interactions:**
   - Tap buttons (touch simulation)
   - Scroll tables
   - Open/close menu
   - Submit forms

**Physical Device Testing:**

Test on real devices when possible:
- [ ] iPhone (iOS Safari)
- [ ] Android phone (Chrome)
- [ ] iPad (Safari)
- [ ] Android tablet (Chrome)

---

## Accessibility Testing

### WCAG AA Compliance

The application must meet WCAG 2.1 Level AA standards.

### Keyboard Navigation Testing

**Goal:** All functionality accessible via keyboard only (no mouse).

**Testing Steps:**

1. **Navigate with Tab Key**
   - [ ] Tab through all interactive elements
   - [ ] Tab order is logical (top to bottom, left to right)
   - [ ] All interactive elements receive focus
   - [ ] Focus indicator is clearly visible (2px minimum)
   - [ ] Skip links work (Skip to main content)

2. **Test Keyboard Shortcuts**
   - [ ] Enter/Space activates buttons
   - [ ] Enter submits forms
   - [ ] Escape closes modals/dropdowns
   - [ ] Arrow keys navigate dropdowns (if applicable)

3. **Focus Management**
   - [ ] Focus moves to modal when opened
   - [ ] Focus returns to trigger when modal closes
   - [ ] Focus doesn't get trapped unexpectedly
   - [ ] Focus visible at all times

**Common Issues:**
- Invisible focus indicators
- Illogical tab order
- Keyboard traps
- No focus on custom controls

### Screen Reader Testing

**Goal:** Content understandable and navigable with screen reader.

**Recommended Tools:**
- **NVDA** (Windows - free)
- **JAWS** (Windows - commercial)
- **VoiceOver** (macOS/iOS - built-in)
- **TalkBack** (Android - built-in)

**Testing Checklist:**

1. **Semantic HTML**
   - [ ] Proper heading hierarchy (H1 → H2 → H3)
   - [ ] Landmarks used (`<nav>`, `<main>`, `<header>`, `<footer>`)
   - [ ] Lists use `<ul>`, `<ol>`, `<li>`
   - [ ] Tables use `<th>`, `<caption>`

2. **ARIA Labels**
   - [ ] Icon-only buttons have `aria-label`
   - [ ] Form inputs have associated labels
   - [ ] Dynamic content uses `aria-live`
   - [ ] Complex widgets have appropriate ARIA roles

3. **Alternative Text**
   - [ ] Images have descriptive `alt` text
   - [ ] Decorative images use `alt=""`
   - [ ] Icons have text alternative or `aria-label`

4. **Screen Reader Announcements**
   - [ ] Page title announces correctly
   - [ ] Headings announce correctly
   - [ ] Links describe destination
   - [ ] Buttons describe action
   - [ ] Form errors announced
   - [ ] Success/error alerts announced

**Testing Procedure (NVDA on Windows):**

1. **Start NVDA** (Insert+N)
2. **Navigate by Headings** (H key)
   - Verify heading hierarchy
3. **Navigate by Landmarks** (D key)
   - Verify page structure
4. **Navigate by Forms** (F key)
   - Verify form controls labeled
5. **Read Content** (Down arrow)
   - Verify content makes sense
6. **Test Interactions**
   - Submit forms
   - Click buttons
   - Navigate tables

### Color Contrast Testing

**Goal:** All text meets minimum contrast ratios.

**Requirements (WCAG AA):**
- Normal text (< 18px): 4.5:1 minimum
- Large text (≥ 18px or ≥ 14px bold): 3:1 minimum
- UI components: 3:1 minimum

**Tools:**
- Chrome DevTools (Inspect element → Contrast ratio)
- [WebAIM Contrast Checker](https://webaim.org/resources/contrastchecker/)
- [Colour Contrast Analyser](https://www.tpgi.com/color-contrast-checker/)

**Testing Steps:**

1. **Open Chrome DevTools**
2. **Inspect text element**
3. **Check contrast ratio** (shown in color picker)
4. **Verify passes WCAG AA** (✓ symbol)

**Common Issues:**
- Light gray text on white background
- Colored buttons with insufficient contrast
- Placeholder text too light
- Disabled controls too light

### Automated Accessibility Testing

**Tools:**

1. **axe DevTools** (Browser Extension)
   - Install in Chrome/Firefox
   - Run automated scan
   - Review issues
   - Fix critical and serious issues

2. **Lighthouse** (Chrome DevTools)
   - Open DevTools → Lighthouse tab
   - Run Accessibility audit
   - Aim for 90+ score
   - Review and fix issues

3. **WAVE** (Browser Extension)
   - Visual feedback on page
   - Identifies errors and warnings
   - Shows structure

**Testing Procedure (axe DevTools):**

1. **Install axe DevTools** extension
2. **Open page to test**
3. **Open DevTools** (F12)
4. **Click axe DevTools tab**
5. **Click "Scan ALL of my page"**
6. **Review issues:**
   - Critical (must fix)
   - Serious (should fix)
   - Moderate (consider fixing)
   - Minor (nice to fix)
7. **Fix issues and re-scan**

**Target Results:**
- 0 Critical issues
- 0 Serious issues
- < 5 Moderate issues
- Lighthouse Accessibility score 90+

---

## Performance Testing

### Goals

- **First Contentful Paint:** < 1.5s
- **Largest Contentful Paint:** < 2.5s
- **Time to Interactive:** < 3.5s
- **Cumulative Layout Shift:** < 0.1
- **Total Blocking Time:** < 200ms

### Lighthouse Audit

**Tool:** Chrome DevTools → Lighthouse

**Testing Steps:**

1. **Open page in Incognito mode** (Ctrl+Shift+N)
   - Disables extensions that could affect results
2. **Open DevTools** (F12)
3. **Click Lighthouse tab**
4. **Configure audit:**
   - Mode: Navigation
   - Categories: Performance, Accessibility, Best Practices
   - Device: Desktop (for 1366px testing)
5. **Click "Analyze page load"**
6. **Review results**

**Target Scores:**
- Performance: 90+
- Accessibility: 90+
- Best Practices: 90+
- SEO: 80+ (if applicable)

**Common Performance Issues:**
- Large images not optimized
- Unminified JavaScript/CSS
- Too many network requests
- Blocking render resources
- No caching headers

### Network Performance Testing

**Test with Throttling:**

1. **Open DevTools → Network tab**
2. **Enable throttling:**
   - Fast 3G (mobile)
   - Slow 3G (poor connection)
   - Custom (adjust as needed)
3. **Reload page**
4. **Verify:**
   - Page loads within acceptable time
   - Loading states appear
   - Progressive enhancement works
   - No timeouts

**Metrics to Monitor:**
- Total page size (< 2MB target)
- Number of requests (< 50 target)
- Largest resource size
- Time to interactive

### Performance Optimization Checklist

- [ ] Images optimized (compressed, appropriate format)
- [ ] CSS/JS minified in production
- [ ] Gzip/Brotli compression enabled
- [ ] Caching headers configured
- [ ] Lazy loading for images
- [ ] Code splitting for JavaScript
- [ ] Remove unused CSS/JS
- [ ] Optimize font loading
- [ ] Minimize render-blocking resources

---

## Functional Testing

### Page-by-Page Testing

#### Index Page

- [ ] Page loads without errors
- [ ] Quick action cards display
- [ ] All links work
- [ ] Getting Started section visible
- [ ] Navigation links highlighted correctly

#### LogCollections Page

- [ ] Collections load from API
- [ ] Table displays all columns
- [ ] Metrics update (if polling enabled)
- [ ] "Create New Collection" button works
- [ ] Edit/Delete buttons work
- [ ] Confirmation dialog for delete
- [ ] Error messages display
- [ ] Success messages display
- [ ] Loading state appears while fetching

#### LogCollections/Manage Page

- [ ] Create mode: Empty form
- [ ] Edit mode: Form populated with data
- [ ] All form fields work
- [ ] Validation works (required fields)
- [ ] Duration presets apply correctly
- [ ] Submit button shows loading state
- [ ] Success redirects to collections list
- [ ] Errors display inline
- [ ] Cancel button works

#### LogAttributes Page

- [ ] Attributes load for selected collection
- [ ] Collection filter works
- [ ] Table displays all columns
- [ ] Create/Edit/Delete buttons work
- [ ] Confirmation for delete
- [ ] Error/success messages display

#### LogAttributes/Manage & Edit Pages

- [ ] Form fields work correctly
- [ ] Extraction expression input accepts regex
- [ ] Validation works
- [ ] Save/Cancel buttons work
- [ ] Errors display appropriately

#### SearchLogs Page

- [ ] Collection selector loads options
- [ ] Attribute filters load for selected collection
- [ ] Add/Remove filter works
- [ ] Filter operators work (=, !=, >, <, etc.)
- [ ] Date inputs work
- [ ] Search executes correctly
- [ ] Results table displays
- [ ] Download button works for each log
- [ ] Load More button works
- [ ] Clear All filters works
- [ ] Loading states display
- [ ] Empty state when no results
- [ ] Error messages for failed search

### Form Testing

**For each form:**

1. **Valid Input**
   - [ ] Submit with all valid data
   - [ ] Success message appears
   - [ ] Redirects to correct page
   - [ ] Data saved correctly

2. **Invalid Input**
   - [ ] Submit with empty required fields
   - [ ] Validation errors appear
   - [ ] Form not submitted
   - [ ] Error messages helpful

3. **Edge Cases**
   - [ ] Maximum length inputs
   - [ ] Special characters
   - [ ] SQL injection attempts (should be sanitized)
   - [ ] XSS attempts (should be escaped)

4. **Loading States**
   - [ ] Button disabled during submit
   - [ ] Loading spinner appears
   - [ ] User can't double-submit

### API Testing

**For each API endpoint:**

1. **Success Case**
   - [ ] Returns expected status code
   - [ ] Returns expected data format
   - [ ] Response time acceptable

2. **Error Cases**
   - [ ] Returns appropriate error codes (400, 404, 500)
   - [ ] Error messages are clear
   - [ ] No sensitive data leaked

3. **Validation**
   - [ ] Rejects invalid input
   - [ ] Returns validation errors
   - [ ] Prevents SQL injection
   - [ ] Prevents XSS

---

## Testing Checklist

### Pre-Deployment Checklist

**Functionality:**
- [ ] All pages load without errors
- [ ] All forms submit correctly
- [ ] All API calls succeed
- [ ] All interactive elements work
- [ ] No console errors in any browser

**Design:**
- [ ] Layout matches design at target viewport (1366px)
- [ ] Colors consistent across pages
- [ ] Spacing consistent across pages
- [ ] Typography hierarchy clear
- [ ] No visual bugs or glitches

**Accessibility:**
- [ ] Keyboard navigation works on all pages
- [ ] Focus indicators visible
- [ ] ARIA labels on interactive elements
- [ ] Screen reader friendly
- [ ] Color contrast meets WCAG AA
- [ ] axe DevTools shows 0 critical/serious issues
- [ ] Lighthouse accessibility score 90+

**Responsive:**
- [ ] Layout works at 1920px
- [ ] Layout works at 1366px (target)
- [ ] Layout works at 768px
- [ ] Tables usable on mobile
- [ ] Forms usable on mobile
- [ ] Touch targets 44x44px minimum

**Performance:**
- [ ] Lighthouse performance score 90+
- [ ] Page load time < 3s
- [ ] No layout shifts
- [ ] Images optimized
- [ ] No memory leaks

**Cross-Browser:**
- [ ] Chrome: All features work
- [ ] Firefox: All features work
- [ ] Safari: All features work
- [ ] Edge: All features work

**Security:**
- [ ] User input sanitized
- [ ] XSS prevention in place
- [ ] SQL injection prevention
- [ ] CSRF tokens (if applicable)
- [ ] No sensitive data in URLs
- [ ] No sensitive data in console logs

---

## Tools & Resources

### Browser Extensions

- **axe DevTools** - Accessibility testing
- **WAVE** - Visual accessibility feedback
- **Lighthouse** - Performance and accessibility audits
- **React DevTools** - React debugging (if applicable)
- **Redux DevTools** - State management debugging (if applicable)

### Online Tools

- [WebAIM Contrast Checker](https://webaim.org/resources/contrastchecker/)
- [Can I Use](https://caniuse.com/) - Browser compatibility
- [PageSpeed Insights](https://pagespeed.web.dev/)
- [W3C Markup Validator](https://validator.w3.org/)
- [CSS Validator](https://jigsaw.w3.org/css-validator/)

### Screen Readers

- **NVDA** (Windows) - https://www.nvaccess.org/
- **JAWS** (Windows) - https://www.freedomscientific.com/products/software/jaws/
- **VoiceOver** (macOS/iOS) - Built-in
- **TalkBack** (Android) - Built-in

### Documentation

- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
- [MDN Accessibility](https://developer.mozilla.org/en-US/docs/Web/Accessibility)
- [A11y Project Checklist](https://www.a11yproject.com/checklist/)
- [WebAIM](https://webaim.org/)

---

## Testing Schedule

### Developer Testing (Before PR)
- Run on every code change
- Fix critical issues immediately

### QA Testing (Before Deployment)
- Full test suite
- All browsers and viewports
- Accessibility audit
- Performance audit

### Regression Testing (After Deployment)
- Smoke test critical paths
- Monitor error logs
- Monitor performance metrics

### Periodic Testing (Monthly/Quarterly)
- Full accessibility audit
- Browser compatibility check
- Performance baseline
- User feedback review

---

## Reporting Issues

When reporting a bug or issue, include:

1. **Title:** Brief description
2. **Environment:**
   - Browser and version
   - Viewport size
   - Operating system
3. **Steps to Reproduce:**
   - Detailed step-by-step
4. **Expected Behavior:**
   - What should happen
5. **Actual Behavior:**
   - What actually happened
6. **Screenshots/Videos:**
   - Visual evidence
7. **Console Errors:**
   - JavaScript errors
8. **Severity:**
   - Critical, High, Medium, Low

---

## Version History

**v1.0 (April 30, 2026)**
- Initial testing guide
- Browser testing procedures
- Accessibility testing guidelines
- Performance testing criteria
- Functional testing checklists
