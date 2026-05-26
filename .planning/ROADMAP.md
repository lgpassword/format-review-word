# Roadmap

## Phase 1: MVP Vertical Slice

Goal: Ship a usable local ASP.NET Core + SQLite application that performs the full template-first workflow and generates a report plus annotated Word copy.

Deliverables:

- GSD and beads tracking initialized.
- ASP.NET Core Razor Pages project.
- SQLite database initialization.
- OpenXML document analysis service.
- Two-step upload UI.
- Comparison service producing clear issues.
- Word annotation write-back.
- Report page and HTML export.
- Build and smoke verification.

## Phase 2: Rule Quality Improvements

Goal: Improve rule inference, style inheritance handling, and issue accuracy.

Candidate work:

- Better style resolution.
- More page setup checks.
- Better table formatting checks.
- More precise blank-area heuristics.
- Reduced false positives.

## Phase 3: Report Polish And Exports

Goal: Improve user experience after MVP.

Candidate work:

- Better filtering/search.
- Excel/CSV export.
- Report history page.
- Template library.
