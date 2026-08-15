# Technology Stack

The application must use the following technologies.

Frontend

- React
- Vite
- TypeScript

Styling

- Bootstrap 5

Routing

- React Router

HTTP Client

- Axios

Progressive Web App

- vite-plugin-pwa

Linting

- Oxlint

Package Manager

- npm

Only use technologies that already exist in the project.

Never introduce additional frameworks or third-party libraries unless explicitly requested.

If a requested implementation requires a package that is not installed, stop and recommend the required package before generating code.

--------------------------------------------------

# Project Initialization

Assume this project is initialized using the official React TypeScript Vite template.

Expected command:

npm create vite@latest . -- --template react-ts

The project must use TypeScript from the beginning.

Never migrate a JavaScript React project to TypeScript.

Never generate:

- .jsx
- .js React components

Only generate:

- .ts
- .tsx

If the existing project does not match these requirements, stop and explain the issue before continuing.

--------------------------------------------------

# Project Directory

Project Root

C:\Users\Wyrlo\projects\ApexBooking

All generated files must remain inside this project.

Never create files outside the project root.

--------------------------------------------------

# Folder Structure

Organize the project using a feature-oriented and reusable architecture.

Example structure:

src/

    api/
        clients/
        interceptors/

    assets/
        icons/
        images/
        fonts/

    components/
        common/
        layout/
        landing/
        booking/
        shared/

    config/

    constants/

    contexts/

    hooks/

    layouts/

    pages/

    providers/

    routes/

    services/

    styles/

    types/

    interfaces/

    utils/

Only create folders when they are required.

Do not generate empty folders.

Do not generate placeholder files.

--------------------------------------------------

# Folder Responsibilities

api/

Contains API clients, Axios configuration, and interceptors.

components/

Reusable UI components.

Never place page logic inside common components.

pages/

Top-level pages only.

Pages compose components.

Pages should contain minimal business logic.

hooks/

Reusable custom React hooks.

Do not create hooks for one-time use unless requested.

services/

Business-related services.

Do not place UI code inside services.

config/

Application configuration.

Examples:

- Menu configuration
- Route configuration
- Environment configuration

constants/

Application constants.

Avoid hardcoded values throughout the project.

types/

Type aliases.

interfaces/

Interface definitions.

utils/

Pure utility functions.

Utilities should not depend on React.

styles/

Global styling only.

Prefer Bootstrap utilities before writing custom CSS.

--------------------------------------------------

# Naming Conventions

Folders

lowercase

Examples

components

hooks

services

Files

PascalCase

Examples

LandingPage.tsx

BookingCard.tsx

Sidebar.tsx

Hooks

Always begin with "use"

Examples

useAuth.ts

useInstallPrompt.ts

useAxios.ts

Components

PascalCase

Examples

Header.tsx

Footer.tsx

PricingCard.tsx

Variables

camelCase

Functions

camelCase

Interfaces

Prefix with "I"

Examples

IBooking

ICustomer

IStaff

Types

Suffix with "Type"

Examples

BookingType

CustomerType

Constants

UPPER_SNAKE_CASE

Examples

DEFAULT_PAGE_SIZE

API_TIMEOUT

Enum

PascalCase

Enum members

PascalCase

Routes

Use kebab-case.

Examples

/bookings

/login

/request-access

--------------------------------------------------

# Import Standards

Organize imports consistently.

Order:

1.

React

2.

Third-party packages

3.

Application configuration

4.

Services

5.

Hooks

6.

Components

7.

Utilities

8.

Types

9.

Interfaces

10.

Styles

Avoid unused imports.

Avoid wildcard imports unless appropriate.

--------------------------------------------------

# Component Organization

Each component should have one responsibility.

Large components should be decomposed into smaller reusable components.

Never create components that exceed a reasonable complexity.

If a component becomes difficult to understand, split it into smaller components.

Prefer composition over duplication.

--------------------------------------------------

# Reusable Component Philosophy

Before creating a new component, determine whether an existing component can be reused.

If the same UI appears more than once, convert it into a reusable component.

Examples of reusable components include:

- Button
- Card
- Input
- Modal
- Table
- EmptyState
- ErrorState
- LoadingSpinner
- Skeleton
- Badge
- Alert
- Pagination

Do not duplicate reusable UI across pages.

--------------------------------------------------

# Configuration Driven Development

Avoid hardcoding configuration whenever practical.

Prefer centralized configuration for:

- Navigation menus
- Routes
- Application settings
- Feature flags
- Role-based visibility
- Theme configuration

Adding new functionality should require minimal code changes whenever configuration can be used instead.