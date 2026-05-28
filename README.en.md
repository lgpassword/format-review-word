# WordFormatAnalyzer

[中文](README.md)

WordFormatAnalyzer is a local ASP.NET Core Razor Pages application for reviewing Word document formatting against a template. It analyzes a Word template or user-defined module rules, compares a target document, then produces a readable report, an annotated Word copy, an HTML report, and a normalized Word document.

## Purpose

WordFormatAnalyzer helps users review Word document formatting against a template. It is designed for documents such as theses, reports, contracts, proposals, and bidding files where formatting consistency matters.

Users can upload a template first, let the application extract comments and formatting rules, and then upload the target document for comparison. The application produces clear issues with expected values, actual values, and suggested fixes.

The application also supports a custom-rule workflow: upload a document, let the system split it into document modules, enter rules for each module, and run the check against those rules.

## Features

- `.docx` analysis.
- `.doc` upload with server-side conversion on Windows when Microsoft Word is installed.
- Template-first review workflow.
- Custom module-rule workflow.
- Template comment, paragraph, run, table, and page setup extraction.
- Chinese font, Western font, font size, alignment, spacing, line spacing, and first-line-indent rules.
- Optional qualified reference templates with conflict resolution.
- Editable per-session rule table.
- Format checks for fonts, paragraph settings, page setup, table structure, blank areas, punctuation, and partially unchecked items.
- Severity and category filters in the report page.
- Annotated Word output.
- Offline HTML report export.
- Normalized Word output.
- SQLite-backed session persistence.

## Tech Stack

- .NET 8
- ASP.NET Core Razor Pages
- DocumentFormat.OpenXml 3.1.1
- Microsoft.Data.Sqlite 8.0.11
- Bootstrap / jQuery static assets
- Microsoft Word automation for legacy `.doc` conversion

## Quick Start

Prerequisites:

- .NET 8 SDK.
- Microsoft Word on Windows if legacy `.doc` conversion is needed.
- `.docx` analysis does not require Microsoft Word.

Run:

```bash
dotnet restore
dotnet run --urls "http://localhost:5088"
```

On Windows, you can also run:

```bat
start-web.bat
```

Open:

```text
http://localhost:5088
```

## Usage

Template workflow:

1. Open the home page.
2. Upload a template document in the template workflow.
3. Review the template analysis summary.
4. Optionally upload one or more qualified reference templates.
5. Resolve rule conflicts if they exist.
6. Review and edit the effective rules for the current session.
7. Upload the target document.
8. Review the report.
9. Download the annotated Word document, HTML report, or normalized Word document.

Custom-rule workflow:

1. Upload the document in the custom-rule workflow.
2. Let the system classify document modules.
3. Fill in module rules in the rule table.
4. Run the check.
5. Review and download generated outputs.

## Directory Structure

```text
.
├── Models/
│   └── AnalysisModels.cs
├── Options/
│   └── AppStorageOptions.cs
├── Pages/
│   ├── Index.cshtml
│   ├── Index.cshtml.cs
│   └── Shared/
├── Services/
│   ├── WordAnalysisService.cs
│   ├── WordComparisonService.cs
│   ├── WordAnnotationService.cs
│   ├── ReportRenderService.cs
│   ├── NormalDocumentService.cs
│   ├── WordConversionService.cs
│   ├── AnalysisSessionRepository.cs
│   └── ...
├── wwwroot/
│   ├── css/
│   ├── js/
│   └── lib/
├── Program.cs
├── WordFormatAnalyzer.csproj
├── appsettings.json
└── start-web.bat
```

## Architecture

The application is a single ASP.NET Core web application with clear internal boundaries. Razor Pages handle interaction and workflow coordination, the model layer carries document and report data, the service layer owns Word processing and comparison logic, and local storage keeps uploads, generated files, and SQLite session data.

```text
Browser
  |
  v
Razor Pages
  |
  v
Domain/Application Services
  |
  +--> Open XML analysis
  +--> Style resolution and terminology conversion
  +--> Rule extraction and conflict resolution
  +--> Target comparison
  +--> Word comments, HTML reports, normalized documents
  |
  v
Local files + SQLite
```

### Page Layer

`Pages/Index.cshtml` is the main UI for uploads, rule editing, conflict resolution, reports, and downloads.

`Pages/Index.cshtml.cs` coordinates the request workflow:

- Validates upload type and size.
- Saves uploaded files.
- Invokes `.doc` conversion when needed.
- Calls template analysis, rule services, comparison, and report services.
- Persists sessions to SQLite.
- Serves annotated Word, HTML report, and normalized Word downloads.

### Model Layer

`Models/AnalysisModels.cs` defines the core data structures:

- `DocumentAnalysisResult`: analysis result for one document.
- `ParagraphSnapshot`: paragraph text, style, font, spacing, area, and module classification.
- `RunFormatSnapshot`: run-level font, size, and character statistics.
- `TableSnapshot`: table structure summary.
- `PageSetupSnapshot`: margins, header/footer margins, paper size, and orientation.
- `TemplateFormatRule`: template rule extracted by area/module.
- `TemplateRuleConflict`: rule conflict between templates.
- `FormatIssue`: one detected formatting issue.
- `AnalysisReport`: generated report.
- `AnalysisSession`: one user analysis session.

### Service Layer

`Services/` contains the business logic:

- `WordAnalysisService`: reads Word documents with Open XML and extracts comments, paragraphs, runs, tables, page setup, blank areas, coverage areas, and template rules.
- `WordStyleResolver`: resolves effective paragraph and run formatting from direct formatting and style inheritance.
- `WordTerminologyService`: converts raw Open XML values into Word-friendly wording such as font size names, spacing, and orientation.
- `TemplateRuleService`: extracts modular formatting rules and applies user edits.
- `TemplateRuleValueNormalizer`: normalizes user-entered fonts, sizes, indents, spacing, and line spacing into comparable values.
- `TemplateRuleConflictService`: detects rule differences between primary and reference templates and applies user choices.
- `EffectiveTemplateService`: builds the final template used for the current check.
- `WordComparisonService`: compares the target document against effective rules and creates issue lists and summaries.
- `PunctuationIssueService`: detects Chinese/English punctuation, duplicate punctuation, English punctuation spacing, and missing sentence-ending punctuation.
- `IssueTextService`: builds user-facing issue titles, expected/actual text, and Word comment text.
- `WordAnnotationService`: copies the target Word document and writes comments at issue locations.
- `ReportRenderService`: renders offline HTML reports.
- `NormalDocumentService`: creates a normalized Word document where changes are safe.
- `WordConversionService`: converts `.doc` to `.docx` when supported.
- `AppStorage`: creates upload, generated, and working file paths.
- `DatabaseInitializer`: creates or updates the SQLite schema.
- `AnalysisSessionRepository`: persists and loads sessions.

### Storage Layer

Runtime storage is configured in `appsettings.json`:

```json
{
  "Storage": {
    "DataDirectory": "App_Data",
    "UploadDirectory": "Uploads",
    "GeneratedDirectory": "Generated",
    "DatabaseFileName": "word-analyzer.db"
  }
}
```

The app creates:

- `App_Data/Uploads/` for uploaded documents.
- `App_Data/Generated/` for generated Word and HTML files.
- `App_Data/word-analyzer.db` for SQLite session data.

These runtime files are ignored by Git.

## Processing Flow

Template workflow:

```text
Upload template
  -> Save file
  -> Convert .doc to .docx when needed
  -> Analyze template with Open XML
  -> Extract baseline and module rules
  -> Persist session
  -> Optional reference template analysis
  -> Optional conflict resolution and rule editing
  -> Upload target document
  -> Analyze target document
  -> Build effective template
  -> Compare target against template rules
  -> Generate annotated Word
  -> Generate HTML report
  -> Generate normal document
  -> Persist report and generated file paths
```

Custom-rule workflow:

```text
Upload target document
  -> Analyze and classify modules
  -> Build empty editable rules
  -> User fills module rules
  -> Compare document against user rules
  -> Generate annotated Word, HTML report, and normal document
```

## Check Coverage

Current checks cover:

- Chinese and Western fonts.
- Font size.
- Paragraph alignment.
- Spacing before, spacing after, line spacing, and first-line indent.
- Run-level local font and size mismatches.
- Page setup differences.
- Table column-count differences.
- Large blank areas caused by repeated empty paragraphs.
- Chinese/English punctuation and sentence-ending punctuation.
- Items that cannot be fully judged automatically.

## Outputs

After a check, the page can download:

- `*-格式检查批注.docx` or `*-自定义规则批注.docx`: annotated Word document.
- `*-格式检测报告.html` or `*-自定义规则检测报告.html`: offline HTML report.
- `*-正常文档.docx` or `*-按自定义规则调整.docx`: normalized Word document.

## Configuration

Default settings are in `appsettings.json`:

- `Logging`: ASP.NET Core log levels.
- `AllowedHosts`: host filtering.
- `Storage`: runtime data directory, upload directory, generated-file directory, and SQLite database name.

For local overrides, use an uncommitted `appsettings.Local.json` or environment variables.

## Development And Verification

Common commands:

```bash
dotnet restore
dotnet build
dotnet run --urls "http://localhost:5088"
```

Suggested verification:

- Home page opens.
- Template upload shows an analysis summary.
- Reference template upload shows conflicts or merged rules.
- Rule edits can be saved.
- Target upload generates a report.
- Annotated Word, HTML report, and normalized Word downloads work.

## Limitations

- `.doc` conversion requires Windows and Microsoft Word.
- The application is intended for local or intranet use; it does not include user accounts or permission management.
- Uploaded and generated files are stored on the local runtime path, so the app is not ready for public internet deployment as-is.
- Open XML does not fully reproduce Word's pagination engine, so TOC page numbers, complex floating objects, and image pagination may require manual review.

