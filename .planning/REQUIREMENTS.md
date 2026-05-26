# Requirements

## R1 Two-Step Upload Flow

The application must first accept a template `.docx`, analyze it, show a template summary, and only then show the actual document upload.

## R2 Template Analysis

The application must extract:

- Template comments
- Paragraph formatting
- Run font/size/bold/italic/underline information
- Table structure
- Section/page settings
- Blank paragraph signals

## R3 Actual Document Comparison

The application must compare the actual document with inferred template rules and produce issues for:

- Font and size mismatches
- Paragraph spacing/line spacing/alignment/indent issues
- Table structure issues
- Page setup issues
- Suspicious blank areas

## R4 Report

The report must include:

- Report id
- Template and actual file names
- Check time
- Conclusion
- Issue counts by severity and category
- Clear issue rows with issue code, location, expected value, actual value, and fix suggestion
- Simple severity/category filtering

## R5 Annotated Word Output

The application must create a new `.docx` copy of the actual document with comments inserted near detected problems. The original upload must not be overwritten.

## R6 HTML Export

The application should provide an offline HTML report export using the same issue codes as the report page and Word comments.

## R7 SQLite Persistence

SQLite must store analysis sessions, template summaries, report metadata, and generated file references.

## R8 Verification

The MVP must build with `dotnet build` and be smoke-tested for template upload, actual upload, report rendering, annotated Word download, and HTML export.
