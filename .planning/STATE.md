# State

## Current Phase

Phase 1: MVP Vertical Slice complete.

Next planned work: Phase 2, refined Word-format engine and coverage reporting.

## Current Beads

- `WordFormatAnalyzer-wu2`: Phase 1 feature umbrella.
- `WordFormatAnalyzer-1xg`: GSD planning artifacts.
- `WordFormatAnalyzer-zqm`: ASP.NET SQLite project scaffold.
- `WordFormatAnalyzer-jch`: Two-step workflow.
- `WordFormatAnalyzer-7oj`: Word annotation write-back.
- `WordFormatAnalyzer-ntk`: Report and HTML export.
- `WordFormatAnalyzer-cxf`: MVP verification.

## Decisions

- Project lives in `D:\github\WordFormatAnalyzer`.
- Use ASP.NET Core Razor Pages.
- Use SQLite for persistence.
- Use OpenXML SDK for Word read/write.
- Keep UI simple and sequential.
- Use `E:\md\word-format-analyzer-design.md` as the development source of truth.
- Future checking must use Word-user terminology instead of raw Open XML values where possible.
- Template comments outrank style-inheritance rules when they conflict.
- Checks should cover all document areas and report coverage by area.
- Multi-template reconciliation is now part of the roadmap: primary template plus qualified reference templates, with user-edited conflict resolution for the current session.
- Normal document generation should aim for full template compliance where safe, preserve body text, remove comments, and write automatic-fix records to HTML only.

## Verification

- `dotnet build` passed with 0 warnings and 0 errors.
- Smoke-tested home page at `http://localhost:5019`.
- Smoke-tested template upload using the provided reviewed Word sample.
- Smoke-tested actual document upload using the same sample.
- Verified report page renders with issue codes and download links.
- Verified annotated Word and HTML report download endpoints return HTTP 200.

## Exploration Capture

The advanced requirements from the latest `$gsd-explore` session have been captured in:

- `.planning/REQUIREMENTS.md`
- `.planning/ROADMAP.md`
- `.planning/WORD-ANALYZER-DESIGN.md`
- `E:\md\word-format-analyzer-design.md`
