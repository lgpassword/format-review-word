namespace WordFormatAnalyzer.Models;

public sealed class DocumentAnalysisResult
{
    public string FileName { get; set; } = "";
    public List<CommentInfo> Comments { get; set; } = [];
    public List<ParagraphSnapshot> Paragraphs { get; set; } = [];
    public List<TableSnapshot> Tables { get; set; } = [];
    public List<BlankAreaFinding> BlankAreas { get; set; } = [];
    public List<CoverageAreaSummary> CoverageAreas { get; set; } = [];
    public List<UncheckedItem> UncheckedItems { get; set; } = [];
    public PageSetupSnapshot PageSetup { get; set; } = new();
    public TemplateBaseline Baseline { get; set; } = new();
}

public sealed class TemplateReferenceAnalysis
{
    public string FileName { get; set; } = "";
    public string Path { get; set; } = "";
    public DocumentAnalysisResult Analysis { get; set; } = new();
}

public sealed class TemplateRuleConflict
{
    public string ConflictId { get; set; } = "";
    public string Category { get; set; } = "";
    public string RuleName { get; set; } = "";
    public string Location { get; set; } = "模板规则";
    public string PrimaryValue { get; set; } = "";
    public string PrimaryRawValue { get; set; } = "";
    public string ReferenceValue { get; set; } = "";
    public string ReferenceRawValue { get; set; } = "";
    public List<TemplateRuleOption> ReferenceOptions { get; set; } = [];
    public string SelectedValue { get; set; } = "";
    public string RawSelectedValue { get; set; } = "";
    public string Source { get; set; } = "总模板";
    public bool IsResolved { get; set; }
}

public sealed class TemplateRuleOption
{
    public string Source { get; set; } = "";
    public string DisplayValue { get; set; } = "";
    public string RawValue { get; set; } = "";
}

public sealed record CommentInfo(string Author, DateTime? CreatedAt, string Text, string ContextText);

public sealed class ParagraphSnapshot
{
    public string TargetElementId { get; set; } = "";
    public int Index { get; set; }
    public string Text { get; set; } = "";
    public string FieldCodeText { get; set; } = "";
    public string Area { get; set; } = "正文";
    public string BlockType { get; set; } = "正文";
    public int BlockIndex { get; set; }
    public string BlockKey { get; set; } = "";
    public string StyleId { get; set; } = "";
    public string Justification { get; set; } = "";
    public string JustificationRaw { get; set; } = "";
    public string FontName { get; set; } = "";
    public string FontNameRaw { get; set; } = "";
    public string FontSize { get; set; } = "";
    public string FontSizeRaw { get; set; } = "";
    public bool? Bold { get; set; }
    public string SpacingBefore { get; set; } = "";
    public string SpacingBeforeRaw { get; set; } = "";
    public string SpacingAfter { get; set; } = "";
    public string SpacingAfterRaw { get; set; } = "";
    public string LineSpacing { get; set; } = "";
    public string LineSpacingRaw { get; set; } = "";
    public string FirstLineIndent { get; set; } = "";
    public string FirstLineIndentRaw { get; set; } = "";
    public int CharacterCount { get; set; }
    public int RunCount { get; set; }
    public bool HasDrawing { get; set; }
    public List<RunFormatSnapshot> Runs { get; set; } = [];
    public bool IsEmpty => string.IsNullOrWhiteSpace(Text);
}

public sealed class RunFormatSnapshot
{
    public int Index { get; set; }
    public string Text { get; set; } = "";
    public string FontName { get; set; } = "";
    public string FontNameRaw { get; set; } = "";
    public string FontSize { get; set; } = "";
    public string FontSizeRaw { get; set; } = "";
    public bool? Bold { get; set; }
    public int CharacterCount { get; set; }
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
    public string OrientationRaw { get; set; } = "";

    public double? TopMarginCmValue { get; set; }
    public double? BottomMarginCmValue { get; set; }
    public double? LeftMarginCmValue { get; set; }
    public double? RightMarginCmValue { get; set; }
    public double? HeaderMarginCmValue { get; set; }
    public double? FooterMarginCmValue { get; set; }
    public double? PageWidthCmValue { get; set; }
    public double? PageHeightCmValue { get; set; }
}

public sealed class TemplateBaseline
{
    public string CommonFontName { get; set; } = "";
    public string CommonFontNameRaw { get; set; } = "";
    public string CommonFontSize { get; set; } = "";
    public string CommonFontSizeRaw { get; set; } = "";
    public string CommonJustification { get; set; } = "";
    public string CommonJustificationRaw { get; set; } = "";
    public string CommonSpacingBefore { get; set; } = "";
    public string CommonSpacingBeforeRaw { get; set; } = "";
    public string CommonSpacingAfter { get; set; } = "";
    public string CommonSpacingAfterRaw { get; set; } = "";
    public string CommonLineSpacing { get; set; } = "";
    public string CommonLineSpacingRaw { get; set; } = "";
    public string CommonFirstLineIndent { get; set; } = "";
    public string CommonFirstLineIndentRaw { get; set; } = "";
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
    public bool RequiresConfirmation { get; set; }
    public bool IsUnchecked { get; set; }
    public string Area { get; set; } = "";
}

public sealed class CoverageAreaSummary
{
    public string Area { get; set; } = "";
    public int CharacterCount { get; set; }
    public int ParagraphCount { get; set; }
    public int ObjectCount { get; set; }
    public int CheckedCharacterCount { get; set; }
    public int CheckedParagraphCount { get; set; }
    public int CheckedObjectCount { get; set; }
    public List<string> Notes { get; set; } = [];

    public int CoveragePercent
    {
        get
        {
            var total = CharacterCount + ParagraphCount + ObjectCount;
            if (total <= 0)
            {
                return 100;
            }

            var checkedCount = CheckedCharacterCount + CheckedParagraphCount + CheckedObjectCount;
            return (int)Math.Round(checkedCount * 100.0 / total);
        }
    }
}

public sealed class UncheckedItem
{
    public string Area { get; set; } = "";
    public string Location { get; set; } = "";
    public string Reason { get; set; } = "";
    public string TargetElementId { get; set; } = "";
}

public sealed class AnalysisReport
{
    public string ReportId { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string TemplateFileName { get; set; } = "";
    public string TargetFileName { get; set; } = "";
    public string Conclusion { get; set; } = "";
    public List<FormatIssue> Issues { get; set; } = [];
    public List<CoverageAreaSummary> CoverageAreas { get; set; } = [];
    public List<UncheckedItem> UncheckedItems { get; set; } = [];
    public string AnnotatedWordDownloadName { get; set; } = "";
    public string HtmlReportDownloadName { get; set; } = "";
    public string NormalDocumentDownloadName { get; set; } = "";
}

public sealed class AnalysisSession
{
    public string Id { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string TemplateFileName { get; set; } = "";
    public string TemplatePath { get; set; } = "";
    public DocumentAnalysisResult TemplateAnalysis { get; set; } = new();
    public List<TemplateReferenceAnalysis> ReferenceTemplates { get; set; } = [];
    public List<TemplateRuleConflict> RuleConflicts { get; set; } = [];
    public string? TargetFileName { get; set; }
    public string? TargetPath { get; set; }
    public AnalysisReport? Report { get; set; }
    public string? AnnotatedPath { get; set; }
    public string? HtmlReportPath { get; set; }
}
