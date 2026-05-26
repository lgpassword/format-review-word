# WordFormatAnalyzer Project

## Vision

Build a local ASP.NET Core web application that checks Word `.docx` documents against an uploaded Word template. The application analyzes the template first, then accepts the actual document, produces a clear human-readable report, and generates a copy of the document with Word comments at the problem locations.

## Source Of Truth

Development source document:

```text
E:\md\word-format-analyzer-design.md
```

If requirements change, update that document first, then update planning and code.

## Stack

- ASP.NET Core Razor Pages
- .NET 8 target framework
- SQLite for local persistence
- Open XML SDK for `.docx` reading and annotation write-back
- Simple Bootstrap/Razor UI

## Product Principles

- Keep the UI simple.
- The first screen only uploads and analyzes the template.
- Show actual document upload only after template analysis succeeds.
- Do not overwrite uploaded files.
- Reports and Word comments must be understandable to non-developers.
- Use issue codes consistently between report rows and Word comments.

## Non-Goals For MVP

- No user accounts.
- No remote storage.
- No `.doc` support.
- No full visual page rendering engine.
- No complex dashboard UI.
