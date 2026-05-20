# ApexBooking UI Design System

---

## System Scope

Defines visual structure, layout rules, spacing, typography, and responsive behavior. All UI must follow this system. No deviations.

---

## Layout System

### Framework

- Bootstrap grid system only
- container, row, col structure required

### Rules

- No custom layout framework
- No fixed pixel page layout
- No absolute positioning for structure

### Page Structure Standard

Every page follows: container, row, col(s), components

---

## Spacing System

Bootstrap spacing only.

Allowed scale:

- 0
- 1
- 2
- 3
- 4
- 5

Rules:

- No arbitrary spacing values
- No inline margin or padding logic for layout
- Components must use consistent spacing tokens

---

## Typography System

Bootstrap defaults extended with consistent rules.

### Rules

- One global font family only
- Headings follow Bootstrap hierarchy
- No random font sizes in components

### Hierarchy

- H1 = page title
- H2 = section title
- H3 = subsection
- Body = default text

---

## Color System

Bootstrap base colors only.

Allowed:

- primary
- secondary
- success
- danger
- warning
- info
- light
- dark

Rules:

- No custom ad-hoc colors in components
- Custom colors must be defined in index.css :root only

---

## Card System

Standard UI container pattern:

- border
- shadow-sm
- rounded
- padding (p-3 or p-4)

Rules:

- All list items use the card pattern
- All grouped data uses the card pattern
- No custom container styles outside this system

---

## Form System

All forms must follow Bootstrap form structure.

Rules:

- form-group spacing required
- Label above input
- Validation feedback below input
- Consistent input height across all forms

---

## Button System

Bootstrap button system only:

- btn-primary
- btn-secondary
- btn-success
- btn-danger
- btn-outline-*

Rules:

- No custom button styles unless the variant extends Bootstrap
- Consistent button sizing across the app

---

## Responsive System

Bootstrap breakpoints only:

- sm
- md
- lg
- xl
- xxl

Rules:

- Mobile first design required
- No fixed width layouts
- No horizontal overflow allowed
- Components must reflow naturally

---

## UI Consistency Rule

Same pattern across all pages:

- Same card layout
- Same spacing rules
- Same form layout
- Same button placement logic

No page-specific UI systems.

---

## Forbidden UI Patterns

- Tailwind
- Material UI
- Custom grid system
- Inline layout styling
- Pixel-based layout design
- Mixed spacing systems