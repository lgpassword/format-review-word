namespace WordFormatAnalyzer.Models;

public sealed class DocumentAnalysisResult
{
    public string FileName { get; set; } = "";
    public List<CommentInfo> Comments { get; set; } = [];
    public List<ParagraphSnapshot> Paragraphs { get; set; } = [];
    public List<TableSnapshot> Tables { get; set; } = [];
    public List<BlankAreaFinding> BlankAreas { get; set; } = [];
    public PageSetupSnapshot PageSetup { get; set; } = new();
    public TemplateBaseline Baseline { get; set; } = new();
}

public sealed record CommentInfo(string Author, DateTime? CreatedAt, string Text, string ContextText);

public sealed class ParagraphSnapshot
{
    public string TargetElementId { get; set; } = "";
    public int Index { get; set; }
    public string Text { get; set; } = "";
    public string StyleId { get; set; } = "";
    public string Justification { get; set; } = "";
    public string FontName { get; set; } = "";
    public string FontSize { get; set; } = "";
    public bool? Bold { get; set; }
    public string SpacingBefore { get; set; } = "";
    public string SpacingAfter { get; set; } = "";
    public string LineSpacing { get; set; } = "";
    public string FirstLineIndent { get; set; } = "";
    public bool IsEmpty => string.IsNullOrWhiteSpace(Text);
}

public sealed class TableSnapshot
{
    public string TargetElementId { get; set; } = "";
    public int Index { get; set; }
    public int RowCount { get; set; }
    public int MaxColumnCount { get; set; }
    public string PreviewText { get; set; } = "";
}

public sealed record BlankAreaFinding(string TargetElementId, string Location, int EmptyParagraphCount, string Description);

public sealed class PageSetupSnapshot
{
    public string TopMargin { get; set; } = "";
    public string BottomMargin { get; set; } = "";
    public string LeftMargin { get; set; } = "";
    public string RightMargin { get; set; } = "";
    public string HeaderMargin { get; set; } = "";
    public string FooterMargin { get; set; } = "";
    public string PageWidth { get; set; } = "";
    public string PageHeight { get; set; } = "";
    public string Orientation { get; set; } = "";
}

public sealed class TemplateBaseline
{
    public string CommonFontName { get; set; } = "";
    public string CommonFontSize { get; set; } = "";
    public string CommonJustification { get; set; } = "";
    public string CommonSpacingBefore { get; set; } = "";
    public string CommonSpacingAfter { get; set; } = "";
    public string CommonLineSpacing { get; set; } = "";
    public string CommonFirstLineIndent { get; set; } = "";
    public int CommonTableColumnCount { get; set; }
    public PageSetupSnapshot PageSetup { get; set; } = new();
}

public sealed class FormatIssue
{
    public string IssueCode { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Category { get; set; } = "";
    public string Location { get; set; } = "";
    public string TargetElementId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Expected { get; set; } = "";
    public string Actual { get; set; } = "";
    public string Suggestion { get; set; } = "";
}

public sealed class AnalysisReport
{
    public string ReportId { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string TemplateFileName { get; set; } = "";
    public string TargetFileName { get; set; } = "";
    public string Conclusion { get; set; } = "";
    public List<FormatIssue> Issues { get; set; } = [];
    public string AnnotatedWordDownloadName { get; set; } = "";
    public string HtmlReportDownloadName { get; set; } = "";
}

public sealed class AnalysisSession
{
    public string Id { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string TemplateFileName { get; set; } = "";
    public string TemplatePath { get; set; } = "";
    public DocumentAnalysisResult TemplateAnalysis { get; set; } = new();
    public string? TargetFileName { get; set; }
    public string? TargetPath { get; set; }
    public AnalysisReport? Report { get; set; }
    public string? AnnotatedPath { get; set; }
    public string? HtmlReportPath { get; set; }
}
