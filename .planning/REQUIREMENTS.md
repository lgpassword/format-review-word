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

The system must support a primary template plus optional already-qualified reference templates. When reference templates differ from the primary template, the UI must show a conflict dialog so the user can selectively edit the final rule set. The edited rules drive both document checking and normal document generation for the current detection session.

Template comments are the highest-priority rule source. Recognizable comment rules should be converted into executable rules; unrecognized comments should be preserved, marked as requiring manual confirmation, and shown in reports.

## R3 Actual Document Comparison

The application must compare the actual document with inferred template rules and produce issues for:

- Font and size mismatches
- Paragraph spacing/line spacing/alignment/indent issues
- Table structure issues
- Page setup issues
- Suspicious blank areas
- Punctuation issues, including Chinese/English punctuation usage, full-width/half-width punctuation, duplicate punctuation, missing English punctuation spaces, and missing sentence-ending punctuation
- Full-document coverage across正文、封面、摘要、目录、参考文献、附录、表格、页眉页脚、文本框、脚注、尾注, and other detectable objects
- Character-level and object-level style checks, merged into readable paragraph-level issue rows and Word comments

Checks must prefer Word-user terminology. Font names should use Word display names when recognized, otherwise fall back to the raw name. Font size must display Word size names such as `小二`, `小三`, and `小初` where possible. Paragraph, page, table, image, text box, footnote, and endnote values should use Word-style wording first, with raw values only as fallback.

The checker must resolve direct formatting first and then style inheritance. Template comments override inherited styles when they conflict.

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
- Per-area coverage statistics by character count, paragraph count, and object count
- Unchecked or partially checked items with clear reasons
- Page filters, document-area filters, paragraph/page location, auto-fix status, and manual-confirmation status
- Page list pagination with 100 issues per page

Unchecked items must appear both at the top of the page report and in the issue list. They must also be exported to HTML.

## R5 Annotated Word Output

The application must create a new `.docx` copy of the actual document with comments inserted near detected problems. The original upload must not be overwritten.

Generated comments must use clear multi-line language: issue number, problem, location, expected result, current state, and fix method.

Each detected problem should create its own Word comment. Comments must use category numbering such as `F-001`, `S-001`, and `P-001`, restarting from `001` for each report.

Global uncheckable issues such as directory/page-number validation should be commented near the relevant area when possible. If the directory cannot be identified, place the comment near the document start.

## R6 HTML Export

The application should provide an offline HTML report export using the same issue codes as the report page and Word comments.

HTML export must include automatic-fix records grouped by document area. Each record should show location, change, before/after wording, reason, and a concise template-rule summary. The normal Word output itself should remain clean without revision marks.

## R7 Normal Document Output

The application must generate a separate normalized `.docx` using template-derived rules where possible. It should preserve document content while applying common template page setup, paragraph formatting, font/size, and safe punctuation normalization.

Normal document generation should aim for full template compliance where safe:

- Automatically fix cover, directory, body, table, header/footer, page-number, text box, footnote, and endnote formatting.
- Update directory fields when supported by the runtime environment; otherwise produce clear page and Word-comment warnings.
- Fix inconsistent heading/body numbering only when numbering exists and is inconsistent; do not invent missing numbering.
- Do not change body text content.
- Remove existing comments from the clean normal document.
- Continue generating the normal document when some items cannot be auto-fixed; leave those items unchanged and record them in report/HTML/Word comments.
- Split a long table into continuation tables only when it is too long and paginates badly; use the original title plus `（续表）`, repeat table headers, and do not split short tables.
- Resize images only when they cause page blank-area problems; keep original aspect ratio, do not crop, and do not change position or wrapping.
- Auto-fix manual page breaks and section breaks according to template rules where possible.

Rules that cannot be safely judged should not be applied. They remain report issues and, where appropriate, Word comments.

## R8 Background Processing And Rule Confirmation

Long-running checks must run as background work. The page must show progress percentage, current area/object, and issue count found so far, then refresh and show a completion message when done.

Manual rule confirmation must use a problem-detail modal with area, check item, current value, template requirement, scope, and notes. Confirmed rules affect only the current detection session, re-check only the related area, and are recorded in the HTML report only.

## R9 SQLite Persistence

SQLite must store analysis sessions, template summaries, report metadata, generated file references, background job state, current-session manual rule confirmations, multi-template conflict choices, and coverage summaries.

## R10 Verification

The MVP must build with `dotnet build` and be smoke-tested for template upload, actual upload, report rendering, annotated Word download, normal document download, and HTML export.
