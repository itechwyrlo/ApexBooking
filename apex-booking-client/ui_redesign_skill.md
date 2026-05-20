# ApexBooking UI Redesign Skill Guide

---

## Scope

Defines how AI must redesign UI safely without breaking architecture or DTO flow.

---

## Core Redesign Principle

UI redesign must never change:

- Backend DTO structure
- Hook structure
- API flow
- Feature boundaries

Only the UI layer changes.

---

## Redesign Flow

### Step 1: Analyze Existing Structure

Identify:

- Pages involved
- Hooks involved
- Components involved
- DTOs used

No assumptions allowed.

---

### Step 2: Preserve Data Flow

Always maintain: Page, Hook, DTO, Component, UI. Never modify this flow.

---

### Step 3: UI Refactor Strategy

Allowed actions:

- Replace component styling
- Improve Bootstrap layout structure
- Introduce card standardization
- Normalize spacing system
- Improve responsiveness

Forbidden:

- Adding new API calls
- Changing hook logic without approval
- Changing DTO structure
- Adding new state systems

---

## Component Replacement Rules

When replacing UI:

- Reuse existing components first
- Do not create duplicates
- Extend only when necessary

---

## Layout Redesign Rules

Allowed:

- Switch grid arrangement
- Improve column structure
- Standardize spacing
- Normalize card usage

Not allowed:

- Absolute layout systems
- Custom CSS grids
- Non-Bootstrap systems

---

## Responsiveness Enforcement

All redesigned UI must:

- Support sm, md, lg, xl, xxl
- Avoid overflow
- Avoid fixed widths
- Stack naturally on mobile

---

## Visual Consistency Rule

Redesigned UI must match:

- Same button system
- Same card system
- Same spacing system
- Same typography system

No new visual system creation.

---

## Safe Refactor Rule

If redesign affects multiple features:

- Stop
- Request approval
- Do not proceed automatically

---

## No Assumption Rule

Never assume:

- Missing components
- Missing hooks
- Missing DTO fields
- Missing API behavior

Always verify before changes.

---

## Output Rule

All redesign output must:

- Compile
- Match existing architecture
- Follow UI design system
- Remain DTO-driven
- Remain Bootstrap-based