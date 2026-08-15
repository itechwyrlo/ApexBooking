
# UI Design Philosophy

The application must present a modern, professional, enterprise-quality user experience.

Design every screen as if it will be used daily by business owners and employees.

The user interface should feel:

- Modern
- Clean
- Professional
- Minimal
- Spacious
- Consistent
- Easy to navigate

Avoid visual clutter.

Every screen should have a clear visual hierarchy.

Content should always be easier to scan than decorate.

Never sacrifice usability for aesthetics.

--------------------------------------------------

# Design Principles

Every page should prioritize:

- Readability
- Consistency
- Simplicity
- Accessibility
- Responsiveness

Use whitespace intentionally.

Maintain consistent spacing throughout the application.

Keep typography consistent.

Use Bootstrap's spacing utilities whenever possible.

--------------------------------------------------

# Theme Standards

The application supports:

Light Theme only.

Do not generate dark mode.

Maintain a consistent color palette throughout the application.

Avoid excessive gradients.

Avoid glassmorphism.

Avoid neumorphism.

Avoid overly decorative elements.

--------------------------------------------------

# Layout Standards

Every page should follow a consistent layout.

Typical page structure:

Header

↓

Navigation

↓

Page Header

↓

Page Content

↓

Footer (when applicable)

Each page should have:

- Clear page title
- Optional page description
- Primary actions
- Secondary actions
- Main content

Avoid placing unrelated actions together.

--------------------------------------------------

# Responsive Design Standards

The application must follow a Mobile First approach.

Every screen must work correctly on:

- Mobile
- Tablet
- Laptop
- Desktop
- Wide Screen

Never assume a fixed screen width.

Avoid fixed-width layouts.

Prefer fluid layouts.

Prefer Bootstrap Grid.

Use Bootstrap breakpoints consistently.

The application should remain usable on smaller screens without horizontal scrolling.

--------------------------------------------------

# Bootstrap Standards

Bootstrap 5 is the primary UI framework.

Always prefer Bootstrap utilities before writing custom CSS.

Use Bootstrap for:

- Grid
- Flexbox
- Cards
- Buttons
- Forms
- Navigation
- Utilities
- Modals
- Alerts
- Badges
- Pagination

Avoid unnecessary custom CSS.

Only create custom CSS when Bootstrap cannot satisfy the requirement.

--------------------------------------------------

# Spacing Standards

Maintain consistent spacing throughout the application.

Use Bootstrap spacing utilities.

Avoid random margin and padding values.

Create predictable spacing between:

- Sections
- Cards
- Forms
- Buttons
- Tables
- Navigation

Whitespace should improve readability.

--------------------------------------------------

# Typography Standards

Use clear typography hierarchy.

Each page should have:

One primary heading.

Supporting headings when necessary.

Avoid multiple competing headings.

Text should remain readable on all screen sizes.

--------------------------------------------------

# Card Standards

Cards should:

- Have consistent padding
- Have consistent border radius
- Have subtle shadows
- Maintain equal spacing

Avoid overly decorative cards.

Cards should organize information, not decorate it.

--------------------------------------------------

# Table Standards

Tables should remain responsive.

Support horizontal scrolling when necessary.

Avoid overflowing content.

Actions should remain accessible.

Large tables should support future pagination.

--------------------------------------------------

# Form Standards

Forms should:

- Be easy to scan
- Group related fields
- Clearly indicate required fields
- Display validation messages near the affected field

Disable submission while processing.

Prevent duplicate submissions.

Provide clear success and error feedback.

--------------------------------------------------

# Button Standards

Buttons should communicate hierarchy.

Primary actions should be visually prominent.

Secondary actions should be less prominent.

Danger actions should clearly communicate risk.

Buttons performing asynchronous work should:

- Display loading state
- Disable while processing

Avoid multiple competing primary buttons.

--------------------------------------------------

# Navigation Standards

Navigation should remain simple.

Users should always know where they are.

Current page should be visually identifiable.

Sidebar navigation must be configuration driven.

Menus should never be hardcoded.

--------------------------------------------------

# Icon Standards

Never generate icons.

Always reference icon assets.

Example:

assets/icons/dashboard.svg

Assume icons will be manually created.

Every icon should come from the assets folder.

--------------------------------------------------

# Image Standards

Never generate illustrations.

Never generate screenshots.

Reference image assets only.

Example:

assets/images/dashboard-preview.png

Assume images will be manually created.

--------------------------------------------------

# Animation Standards

Animations should be subtle.

Use animations only when they improve usability.

Examples:

- Collapse
- Fade
- Toast appearance

Avoid:

- Bounce
- Spin effects
- Large transitions
- Decorative animations

Animations should never distract users.

--------------------------------------------------

# Progressive Web App Standards

The application must behave as a Progressive Web App.

Configure:

- Manifest
- Service Worker
- Theme Color
- Application Metadata

Support installation on compatible browsers.

The application should expose an Install button only when installation is available.

Hide the Install button:

- After installation
- When running in standalone mode
- When installation is unavailable

Never implement custom installation behavior.

Always use the browser's native installation flow.

Encapsulate installation behavior into reusable components and hooks.

Example:

components/pwa/

hooks/useInstallPrompt.ts

Do not place PWA installation logic directly inside page components.

--------------------------------------------------

# Accessibility Standards

Every screen must be accessible.

Use semantic HTML.

Maintain proper heading hierarchy.

Associate labels with form controls.

Provide descriptive button labels.

Support keyboard navigation.

Avoid relying solely on color to communicate meaning.

Maintain sufficient color contrast.

Interactive elements should display visible focus indicators.

Accessibility is required for every implementation.

--------------------------------------------------

# Empty State Standards

Every page displaying data must support an empty state.

Never leave blank pages.

Explain why no data is available.

Provide a helpful primary action when appropriate.

Example:

"No bookings yet."

Button:

"Create Booking"

--------------------------------------------------

# Loading State Standards

Every asynchronous operation must display an appropriate loading state.

Never leave users uncertain whether work is being performed.

Prefer Skeleton Loading for:

- Pages
- Cards
- Lists
- Tables
- Calendar
- Reports
- Dashboard widgets

Prefer Loading Spinners for:

- Form submission
- Login
- Saving changes
- Deleting records
- Refreshing data

Buttons performing asynchronous work should:

- Show loading indicators
- Disable while processing

Create reusable loading components.

Avoid duplicating loading implementations.

--------------------------------------------------

# Error State Standards

Every API request should handle failures gracefully.

Display user-friendly error messages.

Do not expose technical server errors.

Provide retry actions when appropriate.

Never crash the application because of recoverable errors.

--------------------------------------------------

# Notification Standards

Use toast notifications for user feedback.

Examples:

- Success
- Warning
- Information
- Error

Never use browser alert dialogs.

Notifications should be concise, informative, and non-intrusive.

--------------------------------------------------

# Confirmation Standards

Potentially destructive actions require confirmation.

Examples:

- Delete
- Cancel Booking
- Remove Staff

Use reusable confirmation dialogs.

Never use browser confirm().