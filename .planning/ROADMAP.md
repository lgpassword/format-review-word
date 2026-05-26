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

Goal: Replace the MVP's coarse paragraph checks with a precise Word-format engine that reads direct formatting, style inheritance, character/run formatting, and object formatting, then reports results in Word-user terminology.

Candidate work:

- Word terminology conversion for fonts, font sizes, paragraph spacing, page setup, tables, images, text boxes, footnotes, and endnotes.
- Direct-format plus style-inheritance resolution to avoid misleading "未设置" results.
- Character/run/object-level scanning across the full document.
- Coverage statistics by document area, character count, paragraph count, and object count.
- Unchecked-item reporting with clear reasons.
- Category issue numbering that restarts per report.

## Phase 3: Multi-Template Rule Reconciliation

Goal: Support a primary template plus already-qualified reference templates, reconcile rule differences through user-controlled dialogs, and use the resulting rule set for the current detection session.

Candidate work:

- Optional upload flow for one or more qualified reference templates after the primary template.
- Rule extraction from primary template comments, primary styles, and reference templates.
- Conflict detection between templates.
- Conflict dialog that lets users choose or edit final rules.
- Manual-confirmation modal for unrecognized natural-language rules.
- Current-session rule storage in SQLite and HTML report capture.
- Re-check only affected document areas after a rule is confirmed.

## Phase 4: Background Jobs And Advanced Reports

Goal: Improve user experience after MVP.

Candidate work:

- Background checking jobs with progress percentage, current area/object, and discovered issue count.
- Completion notification and automatic result refresh.
- Report filtering by document area, page range, auto-fix status, and manual-confirmation status.
- 100-row issue pagination.
- HTML export with automatic-fix records grouped by document area.
- Unchecked-item export to HTML.

## Phase 5: Normal Document Generation Enhancements

Goal: Generate a clean, template-compliant Word document while preserving original body text and recording automatic changes in HTML only.

Candidate work:

- Full-template format correction across cover, directory, body, tables, headers/footers, page numbers, text boxes, footnotes, and endnotes.
- Directory update where runtime support exists; clear warnings when unavailable.
- Existing-comment removal from the normal document.
- Long-table continuation splitting with `（续表）` titles and repeated headers.
- Image resizing only when it causes page blank-area issues, preserving aspect ratio and position.
- Manual page break and section break correction according to template rules.
- Numbering repair only when existing numbering is inconsistent.
