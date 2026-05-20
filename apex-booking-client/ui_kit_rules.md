# UI Kit Rules

---

## Component Philosophy

Components are:

- Presentation only
- DTO-driven
- Reusable
- Stateless by default

---

## Component Structure

Each component must follow:

- ComponentName.tsx
- ComponentName.styles.css

Rules:

- One component = one style file
- No shared CSS between unrelated components
- Styles must be imported via index.css

---

## Allowed Component Behavior

Components may:

- Render props
- Format UI display values
- Apply Bootstrap classes
- Handle UI-only state (hover, toggle, local UI state)

---

## Forbidden in Components

- API calls
- axios usage
- Business logic
- DTO transformation into domain models
- State management for server data

---

## Input Component Rules

Inputs must:

- Be controlled via props
- Not manage external state
- Not fetch data
- Remain reusable

---

## Button Rules

Buttons must:

- Follow Bootstrap base style
- Not implement business logic
- Remain stateless

---

## Card Rules

Cards must:

- Display DTO data only
- Not compute business rules
- Remain layout consistent across the system

---

## Table Rules

Tables must:

- Render DTO arrays only
- Avoid inline logic
- Avoid pagination logic inside the component (handled in hooks)