# Requirements

## R1 Two-Step Upload Flow

The application must first accept a template `.docx`, analyze it, show a template summary, and only then show the actual document upload.

The upload flow must also accept legacy `.doc` files and convert them to `.docx` on the server before analysis.

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
- Punctuation issues, including Chinese/English punctuation usage, full-width/half-width punctuation, duplicate punctuation, missing English punctuation spaces, and missing sentence-ending punctuation

## R4 Report

The report must include:

- Report id
- Template and actual file names
- Check time
- Conclusion
- Issue counts by severity and category
- Clear issue rows with issue code, location, expected value, actual value, and fix suggestion
- Simple severity/category filtering
- Plain-language wording that non-technical Word users can understand

## R5 Annotated Word Output

The application must create a new `.docx` copy of the actual document with comments inserted near detected problems. The original upload must not be overwritten.

Generated comments must use clear multi-line language: issue number, problem, location, expected result, current state, and fix method.

## R6 HTML Export

The application should provide an offline HTML report export using the same issue codes as the report page and Word comments.

## R7 Normal Document Output

The application must generate a separate normalized `.docx` using template-derived rules where possible. It should preserve document content while applying common template page setup, paragraph formatting, font/size, and safe punctuation normalization.

## R8 SQLite Persistence

SQLite must store analysis sessions, template summaries, report metadata, and generated file references.

## R9 Verification

The MVP must build with `dotnet build` and be smoke-tested for template upload, actual upload, report rendering, annotated Word download, normal document download, and HTML export.
