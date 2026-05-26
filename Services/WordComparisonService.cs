using WordFormatAnalyzer.Models;

namespace WordFormatAnalyzer.Services;

public sealed class WordComparisonService
{
    private readonly Dictionary<string, int> _counters = new(StringComparer.OrdinalIgnoreCase);

    public AnalysisReport Compare(DocumentAnalysisResult template, DocumentAnalysisResult target)
    {
        _counters.Clear();
        var issues = new List<FormatIssue>();
        var baseline = template.Baseline;

        AddPageSetupIssues(issues, baseline.PageSetup, target.PageSetup);
        AddParagraphIssues(issues, baseline, target.Paragraphs);
        AddTableIssues(issues, baseline, target.Tables);
        AddBlankAreaIssues(issues, target.BlankAreas);

        return new AnalysisReport
        {
            ReportId = $"R{DateTime.UtcNow:yyyyMMddHHmmss}",
            CreatedAt = DateTime.Now,
            TemplateFileName = template.FileName,
            TargetFileName = target.FileName,
            Conclusion = issues.Any(issue => issue.Severity is "严重" or "错误") ? "未达到合格标准" : "未发现明显格式问题",
            Issues = issues
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

    private void AddPageSetupIssues(List<FormatIssue> issues, PageSetupSnapshot expected, PageSetupSnapshot actual)
    {
        AddPageIssue(issues, "上边距", expected.TopMargin, actual.TopMargin);
        AddPageIssue(issues, "下边距", expected.BottomMargin, actual.BottomMargin);
        AddPageIssue(issues, "左边距", expected.LeftMargin, actual.LeftMargin);
        AddPageIssue(issues, "右边距", expected.RightMargin, actual.RightMargin);
        AddPageIssue(issues, "页眉边距", expected.HeaderMargin, actual.HeaderMargin);
        AddPageIssue(issues, "页脚边距", expected.FooterMargin, actual.FooterMargin);
        AddPageIssue(issues, "纸张宽度", expected.PageWidth, actual.PageWidth);
        AddPageIssue(issues, "纸张高度", expected.PageHeight, actual.PageHeight);
        AddPageIssue(issues, "纸张方向", expected.Orientation, actual.Orientation);
    }

    private void AddPageIssue(List<FormatIssue> issues, string title, string expected, string actual)
    {
        if (string.IsNullOrWhiteSpace(expected) || expected == actual)
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
            Title = $"{title}不符合模板",
            Expected = expected,
            Actual = string.IsNullOrWhiteSpace(actual) ? "未设置" : actual,
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
            Title = title,
            Expected = expected,
            Actual = string.IsNullOrWhiteSpace(actual) ? "未设置" : actual,
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
