using WordFormatAnalyzer.Models;

namespace WordFormatAnalyzer.Services;

public sealed class WordComparisonService
{
    private readonly Dictionary<string, int> _counters = new(StringComparer.OrdinalIgnoreCase);
    private readonly PunctuationIssueService _punctuation;

    public WordComparisonService(PunctuationIssueService punctuation)
    {
        _punctuation = punctuation;
    }

    public AnalysisReport Compare(DocumentAnalysisResult template, DocumentAnalysisResult target)
    {
        _counters.Clear();
        var issues = new List<FormatIssue>();
        var baseline = template.Baseline;

        AddPageSetupIssues(issues, baseline.PageSetup, target.PageSetup);
        AddParagraphIssues(issues, baseline, target.Paragraphs);
        AddRunIssues(issues, baseline, target.Paragraphs);
        AddTableIssues(issues, baseline, target.Tables);
        AddBlankAreaIssues(issues, target.BlankAreas);
        AddPunctuationIssues(issues, template, target);
        AddUncheckedIssues(issues, target.UncheckedItems);

        return new AnalysisReport
        {
            ReportId = $"R{DateTime.UtcNow:yyyyMMddHHmmss}",
            CreatedAt = DateTime.Now,
            TemplateFileName = template.FileName,
            TargetFileName = target.FileName,
            Conclusion = issues.Any(issue => issue.Severity is "严重" or "错误") ? "未达到合格标准" : "未发现明显格式问题",
            Issues = issues,
            CoverageAreas = target.CoverageAreas,
            UncheckedItems = target.UncheckedItems
        };
    }

    private void AddParagraphIssues(List<FormatIssue> issues, TemplateBaseline baseline, IEnumerable<ParagraphSnapshot> paragraphs)
    {
        foreach (var paragraph in paragraphs.Where(p => !p.IsEmpty))
        {
            AddIfDifferent(issues, "F", "错误", "字体字号", paragraph, "字体不符合模板",
                baseline.CommonFontName, paragraph.FontName, $"选中第 {paragraph.Index} 段文字，将字体改为模板常用字体。");

            AddIfDifferent(issues, "F", "错误", "字体字号", paragraph, "字号不符合模板",
                baseline.CommonFontSize, paragraph.FontSize, $"选中第 {paragraph.Index} 段文字，将字号改为模板常用字号。");

            AddIfDifferent(issues, "S", "错误", "段落", paragraph, "对齐方式不符合模板",
                baseline.CommonJustification, paragraph.Justification, "打开段落设置，将对齐方式改为模板要求。");

            AddIfDifferent(issues, "S", "错误", "段落", paragraph, "段前间距不符合模板",
                baseline.CommonSpacingBefore, paragraph.SpacingBefore, "打开段落设置，调整段前间距。");

            AddIfDifferent(issues, "S", "错误", "段落", paragraph, "段后间距不符合模板",
                baseline.CommonSpacingAfter, paragraph.SpacingAfter, "打开段落设置，调整段后间距。");

            AddIfDifferent(issues, "S", "错误", "段落", paragraph, "行距不符合模板",
                baseline.CommonLineSpacing, paragraph.LineSpacing, "打开段落设置，调整行距。");
        }
    }

    private void AddRunIssues(List<FormatIssue> issues, TemplateBaseline baseline, IEnumerable<ParagraphSnapshot> paragraphs)
    {
        if (string.IsNullOrWhiteSpace(baseline.CommonFontName) && string.IsNullOrWhiteSpace(baseline.CommonFontSize))
        {
            return;
        }

        foreach (var paragraph in paragraphs.Where(p => !p.IsEmpty))
        {
            var mismatchedFonts = paragraph.Runs
                .Where(run => !string.IsNullOrWhiteSpace(baseline.CommonFontName) && !string.IsNullOrWhiteSpace(run.FontName) && run.FontName != baseline.CommonFontName)
                .Select(run => run.FontName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .ToList();

            if (mismatchedFonts.Count > 0 && paragraph.FontName == baseline.CommonFontName)
            {
                issues.Add(new FormatIssue
                {
                    IssueCode = NextCode("F"),
                    Severity = "错误",
                    Category = "字体字号",
                    Location = $"第 {paragraph.Index} 段",
                    TargetElementId = paragraph.TargetElementId,
                    Area = "正文",
                    Title = "段内部分文字字体不符合模板",
                    Expected = baseline.CommonFontName,
                    Actual = string.Join("、", mismatchedFonts),
                    Suggestion = $"选中第 {paragraph.Index} 段中字体不一致的文字，将字体改为 {baseline.CommonFontName}。"
                });
            }

            var mismatchedSizes = paragraph.Runs
                .Where(run => !string.IsNullOrWhiteSpace(baseline.CommonFontSize) && !string.IsNullOrWhiteSpace(run.FontSize) && run.FontSize != baseline.CommonFontSize)
                .Select(run => run.FontSize)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .ToList();

            if (mismatchedSizes.Count > 0 && paragraph.FontSize == baseline.CommonFontSize)
            {
                issues.Add(new FormatIssue
                {
                    IssueCode = NextCode("F"),
                    Severity = "错误",
                    Category = "字体字号",
                    Location = $"第 {paragraph.Index} 段",
                    TargetElementId = paragraph.TargetElementId,
                    Area = "正文",
                    Title = "段内部分文字字号不符合模板",
                    Expected = baseline.CommonFontSize,
                    Actual = string.Join("、", mismatchedSizes),
                    Suggestion = $"选中第 {paragraph.Index} 段中字号不一致的文字，将字号改为 {baseline.CommonFontSize}。"
                });
            }
        }
    }

    private void AddTableIssues(List<FormatIssue> issues, TemplateBaseline baseline, IEnumerable<TableSnapshot> tables)
    {
        if (baseline.CommonTableColumnCount <= 0)
        {
            return;
        }

        foreach (var table in tables)
        {
            if (table.MaxColumnCount != baseline.CommonTableColumnCount)
            {
                issues.Add(new FormatIssue
                {
                    IssueCode = NextCode("T"),
                    Severity = "提醒",
                    Category = "表格",
                    Location = $"第 {table.Index} 个表格",
                    TargetElementId = table.TargetElementId,
                    Title = "表格列数与模板常见表格不一致",
                    Expected = $"{baseline.CommonTableColumnCount} 列",
                    Actual = $"{table.MaxColumnCount} 列",
                    Suggestion = "检查该表格是否套用了正确的模板表格结构。"
                });
            }
        }
    }

    private void AddBlankAreaIssues(List<FormatIssue> issues, IEnumerable<BlankAreaFinding> findings)
    {
        foreach (var finding in findings)
        {
            issues.Add(new FormatIssue
            {
                IssueCode = NextCode("B"),
                Severity = "提醒",
                Category = "大空白",
                Location = finding.Location,
                TargetElementId = finding.TargetElementId,
                Title = "疑似存在大空白",
                Expected = "不应出现连续大量空段落",
                Actual = finding.Description,
                Suggestion = "删除多余空段落，或确认该空白是否为模板要求。"
            });
        }
    }

    private void AddUncheckedIssues(List<FormatIssue> issues, IEnumerable<UncheckedItem> uncheckedItems)
    {
        foreach (var item in uncheckedItems)
        {
            issues.Add(new FormatIssue
            {
                IssueCode = NextCode("I"),
                Severity = "提醒",
                Category = "未检查项",
                Location = item.Location,
                TargetElementId = item.TargetElementId,
                Area = item.Area,
                Title = $"{item.Area}暂未完整检查",
                Expected = "需要 Word 排版结果或更明确的模板规则",
                Actual = item.Reason,
                Suggestion = "请在 Word 中人工确认该项，或后续提供明确模板规则后再检查。",
                RequiresConfirmation = true,
                IsUnchecked = true
            });
        }
    }

    private void AddPunctuationIssues(List<FormatIssue> issues, DocumentAnalysisResult template, DocumentAnalysisResult target)
    {
        foreach (var paragraph in target.Paragraphs.Where(p => !p.IsEmpty))
        {
            foreach (var issue in _punctuation.Detect(paragraph))
            {
                issue.IssueCode = NextCode("I");
                issues.Add(issue);
            }
        }
    }

    private void AddPageSetupIssues(List<FormatIssue> issues, PageSetupSnapshot expected, PageSetupSnapshot actual)
    {
        AddPageIssue(issues, "上边距", expected.TopMarginCmValue, actual.TopMarginCmValue, expected.TopMargin, actual.TopMargin);
        AddPageIssue(issues, "下边距", expected.BottomMarginCmValue, actual.BottomMarginCmValue, expected.BottomMargin, actual.BottomMargin);
        AddPageIssue(issues, "左边距", expected.LeftMarginCmValue, actual.LeftMarginCmValue, expected.LeftMargin, actual.LeftMargin);
        AddPageIssue(issues, "右边距", expected.RightMarginCmValue, actual.RightMarginCmValue, expected.RightMargin, actual.RightMargin);
        AddPageIssue(issues, "页眉边距", expected.HeaderMarginCmValue, actual.HeaderMarginCmValue, expected.HeaderMargin, actual.HeaderMargin);
        AddPageIssue(issues, "页脚边距", expected.FooterMarginCmValue, actual.FooterMarginCmValue, expected.FooterMargin, actual.FooterMargin);
        AddPageIssue(issues, "纸张宽度", expected.PageWidthCmValue, actual.PageWidthCmValue, expected.PageWidth, actual.PageWidth);
        AddPageIssue(issues, "纸张高度", expected.PageHeightCmValue, actual.PageHeightCmValue, expected.PageHeight, actual.PageHeight);
        if (!string.Equals(expected.Orientation, actual.Orientation, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new FormatIssue
            {
                IssueCode = NextCode("P"),
                Severity = "严重",
                Category = "页面设置",
                Location = "全文页面设置",
                TargetElementId = "p:0",
                Title = "纸张方向不符合模板",
                Expected = expected.Orientation,
                Actual = actual.Orientation,
                Suggestion = "打开页面设置，将纸张方向改为模板要求。"
            });
        }
    }

    private void AddPageIssue(List<FormatIssue> issues, string title, double? expectedValue, double? actualValue, string expectedText, string actualText)
    {
        if (expectedValue is null || actualValue is null || Math.Abs(expectedValue.Value - actualValue.Value) < 0.01)
        {
            return;
        }

        issues.Add(new FormatIssue
        {
            IssueCode = NextCode("P"),
            Severity = "严重",
            Category = "页面设置",
            Location = "全文页面设置",
            TargetElementId = "p:0",
            Area = "页面设置",
            Title = $"{title}不符合模板",
            Expected = expectedText,
            Actual = string.IsNullOrWhiteSpace(actualText) ? "未设置" : actualText,
            Suggestion = $"打开页面设置，将{title}改为模板要求。"
        });
    }

    private void AddIfDifferent(
        List<FormatIssue> issues,
        string prefix,
        string severity,
        string category,
        ParagraphSnapshot paragraph,
        string title,
        string expected,
        string actual,
        string suggestion)
    {
        if (string.IsNullOrWhiteSpace(expected) || expected == actual)
        {
            return;
        }

        issues.Add(new FormatIssue
        {
            IssueCode = NextCode(prefix),
            Severity = severity,
            Category = category,
            Location = $"第 {paragraph.Index} 段",
            TargetElementId = paragraph.TargetElementId,
            Area = "正文",
            Title = title,
            Expected = expected,
            Actual = string.IsNullOrWhiteSpace(actual) ? "未识别" : actual,
            Suggestion = suggestion
        });
    }

    private string NextCode(string prefix)
    {
        _counters.TryGetValue(prefix, out var value);
        value++;
        _counters[prefix] = value;
        return $"{prefix}-{value:000}";
    }
}
