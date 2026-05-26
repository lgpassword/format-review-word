# Phase 1 Plan: MVP Vertical Slice

## Goal

Create a working ASP.NET Core + SQLite Word format analyzer that follows the development document and supports:

1. Upload/analyze template first.
2. Upload actual document only after template analysis.
3. Generate a readable issue report.
4. Generate an annotated Word copy.
5. Export an offline HTML report.

## Tasks

### 1. Scaffold Project

- Create Razor Pages project.
- Add packages:
  - `DocumentFormat.OpenXml`
  - `Microsoft.Data.Sqlite`
- Configure upload/generated-file folders.
- Configure services in `Program.cs`.

### 2. Add Persistence

- Create SQLite initialization service.
- Store sessions, reports, and generated file paths.
- Keep document analysis data serialized as JSON for MVP simplicity.

### 3. Add Models

- `DocumentAnalysisResult`
- `CommentInfo`
- `ParagraphSnapshot`
- `RunSnapshot`
- `TableSnapshot`
- `BlankAreaFinding`
- `FormatIssue`
- `AnalysisReport`

### 4. Add Word Analysis

- Read `.docx` from stream/path.
- Extract comments.
- Extract paragraphs and runs.
- Extract tables.
- Extract section/page settings.
- Detect suspicious blank paragraphs.
- Infer template baseline rules from common values.

### 5. Add Comparison

- Compare target against template baseline.
- Generate issue codes by category.
- Include expected, actual, location, target id, and fix suggestion.

### 6. Add Annotation Write-Back

- Copy target `.docx`.
- Add comments part if needed.
- Insert comments near target paragraphs/tables.
- Do not modify original upload.

### 7. Add UI

- Index page step 1: template upload.
- Template analysis result page/section.
- Step 2: actual document upload.
- Report page with simple filters.
- Download annotated Word.
- Export HTML report.

### 8. Verify

- Run `dotnet build`.
- Run the app.
- Smoke test upload/report/download path.

## Acceptance Criteria

- `dotnet build` succeeds.
- First screen only accepts template upload.
- Actual document upload appears only after template analysis succeeds.
- Report contains issue code, severity, category, location, expected, actual, and suggestion.
- Annotated Word file is generated and downloadable.
- HTML report export is generated and downloadable.
