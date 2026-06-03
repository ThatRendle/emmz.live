# app-styling Specification

## Purpose

Defines the application's shared visual design system: a single brand stylesheet, the Outfit typeface, a consistent styled look across pages, a full-bleed projector inbox, removal of scaffold artifacts, and the requirement that styling remain presentation-only without altering behaviour or safety.

## Requirements

### Requirement: Shared design system stylesheet

The system SHALL provide a single shared stylesheet, linked by the main layout, that defines the application's visual design via CSS custom properties (color palette, typography, spacing, buttons, form controls, links, and a card container). The palette SHALL be the brand's violet-and-teal scheme (light surfaces with a `#7c3aed` primary and `#14b8a6` accent; a dark `#17132a` projector variant). The stylesheet SHALL be authored in vanilla CSS with no CSS framework dependency, and SHALL NOT contain styling for classes the application does not use.

#### Scenario: Shared stylesheet is linked and applied

- **WHEN** any page rendered through the main layout is loaded
- **THEN** the shared stylesheet SHALL be linked in the document head and its base typography and brand palette SHALL apply to the page

#### Scenario: No scaffold CSS remains

- **WHEN** the shared stylesheet is inspected
- **THEN** it SHALL NOT contain Bootstrap-scaffold selectors that the application never renders (e.g. `.btn`, `.form-floating`, `.nav-link`, `.navbar-brand`)

### Requirement: Application uses the Outfit typeface

The system SHALL render its UI in the Outfit typeface, loaded from Google Fonts in the document head of every layout, with a `system-ui` fallback so text remains legible if the font fails to load.

#### Scenario: Outfit is loaded and applied

- **WHEN** any page is loaded
- **THEN** the Outfit font SHALL be requested in the document head and applied as the primary UI font

#### Scenario: Graceful fallback

- **WHEN** the Outfit font cannot be fetched
- **THEN** text SHALL still render immediately in the `system-ui` fallback without invisible or broken text

### Requirement: Layout exposes a styles section

The main layout SHALL render an optional named "Styles" section in the document head so that an individual page can contribute page-scoped CSS without inlining it in the page body.

#### Scenario: Page contributes head CSS

- **WHEN** a page defines a Styles section
- **THEN** that CSS SHALL be emitted inside the document `<head>`, before the body content

#### Scenario: Page omits the styles section

- **WHEN** a page does not define a Styles section
- **THEN** the page SHALL still render without error

### Requirement: Pages share a consistent look

The magic-link request page, the magic-link verification/error page, the landing page, and the error page SHALL be styled consistently using the shared design system, presenting their content in a centered card on the standard page background. The public submission page SHALL also use the shared brand palette and Outfit font (its existing structure and layout retained) so the application presents one cohesive brand.

#### Scenario: Auth request page is styled

- **WHEN** the magic-link request page is loaded
- **THEN** its content SHALL be presented in a styled centered card using the shared palette and typography, with a styled submit control

#### Scenario: Auth verification error page is styled

- **WHEN** the magic-link verification page renders an invalid or expired result
- **THEN** the message and the "request a new link" action SHALL be presented in the shared styled card

#### Scenario: Landing and error pages are styled

- **WHEN** the landing page or the error page is loaded
- **THEN** it SHALL render using the shared design system with no unstyled, raw-HTML appearance

#### Scenario: Submission page matches the brand

- **WHEN** the public submission page is loaded
- **THEN** it SHALL use the shared violet/teal palette and the Outfit font, while retaining its existing form structure, QR placement, and behaviour

### Requirement: Inbox viewer renders full-bleed without site chrome

The inbox viewer SHALL render as a full-viewport, chrome-free screen optimised for projection: it SHALL NOT display the site header, navigation, or footer. The existing projector behaviour SHALL be preserved — a two-panel layout, detail-panel text of at least 2rem, high contrast, ArrowUp/ArrowDown keyboard navigation, 5-second list auto-refresh, and preservation of the selected message across refresh.

#### Scenario: No site chrome on the inbox

- **WHEN** an authenticated owner loads the inbox page
- **THEN** the page SHALL fill the viewport and SHALL NOT render the site header, navigation, or footer

#### Scenario: Projector behaviour preserved

- **WHEN** the inbox page is displayed and a message is selected
- **THEN** the two-panel layout, ≥2rem high-contrast detail text, keyboard navigation, 5-second auto-refresh, and selected-message preservation SHALL continue to function as before

#### Scenario: Inbox CSS is not inlined in the body

- **WHEN** the inbox page source is inspected
- **THEN** its styling SHALL be delivered via the shared stylesheet and/or the head Styles section rather than a `<style>` block in the page body

### Requirement: Scaffold artifacts are removed

The system SHALL NOT retain unused default scaffold pages or dead stylesheet remnants left over from project generation.

#### Scenario: Unused Privacy page removed

- **WHEN** the application's pages are enumerated
- **THEN** the default scaffold Privacy page SHALL NOT be present

### Requirement: Styling change preserves behaviour and safety

The styling pass SHALL be presentation-only and SHALL NOT alter application behaviour or safety properties. Anonymous message content SHALL remain HTML-auto-encoded and rendered into the detail panel via text assignment (never `innerHTML`); antiforgery protection, magic-link/QR proxy-aware URLs, and all existing automated tests SHALL remain intact.

#### Scenario: Anonymous input stays inert

- **WHEN** a message whose body contains HTML/script markup is shown in the list and detail panel after the styling change
- **THEN** the markup SHALL render as inert text, exactly as before the change

#### Scenario: Existing tests still pass

- **WHEN** the test suite is run after the styling change
- **THEN** all existing tests SHALL pass and the build SHALL remain warning-clean
