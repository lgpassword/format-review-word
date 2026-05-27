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
        AddParagraphIssues(issues, template.Paragraphs, template.FormatRules, baseline, target.Paragraphs);
        AddRunIssues(issues, template.Paragraphs, template.FormatRules, baseline, target.Paragraphs);
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
            Conclusion = issues.Any(issue => issue.Severity == "错误") ? "未达到合格标准" : "未发现明显格式问题",
            Issues = issues,
            AreaSummaries = BuildSummaries(issues.Select(issue => string.IsNullOrWhiteSpace(issue.Area) ? "未分类" : issue.Area)),
            CategorySummaries = BuildSummaries(issues.Select(issue => issue.Category)),
            SeveritySummaries = BuildSummaries(issues.Select(issue => issue.Severity)),
            CoverageAreas = target.CoverageAreas,
            UncheckedItems = target.UncheckedItems
        };
    }

    private void AddParagraphIssues(
        List<FormatIssue> issues,
        IReadOnlyList<ParagraphSnapshot> templateParagraphs,
        IReadOnlyList<TemplateFormatRule> rules,
        TemplateBaseline baseline,
        IEnumerable<ParagraphSnapshot> paragraphs)
    {
        foreach (var paragraph in paragraphs.Where(p => !p.IsEmpty))
        {
            AddUnmatchedBlockIssueIfNeeded(issues, templateParagraphs, paragraph);

            var expectedChineseFontName = ExpectedParagraphValue(templateParagraphs, rules, paragraph, p => p.ChineseFontName, r => r.ChineseFontName, baseline.CommonChineseFontName);
            var expectedWesternFontName = ExpectedParagraphValue(templateParagraphs, rules, paragraph, p => p.WesternFontName, r => r.WesternFontName, baseline.CommonWesternFontName);
            var expectedFontSize = ExpectedParagraphValue(templateParagraphs, rules, paragraph, p => p.FontSize, r => r.FontSize, baseline.CommonFontSize);
            var expectedJustification = ExpectedParagraphValue(templateParagraphs, rules, paragraph, p => p.Justification, r => r.Justification, baseline.CommonJustification);
            var expectedSpacingBefore = ExpectedParagraphValue(templateParagraphs, rules, paragraph, p => p.SpacingBefore, r => r.SpacingBefore, baseline.CommonSpacingBefore);
            var expectedSpacingAfter = ExpectedParagraphValue(templateParagraphs, rules, paragraph, p => p.SpacingAfter, r => r.SpacingAfter, baseline.CommonSpacingAfter);
            var expectedLineSpacing = ExpectedParagraphValue(templateParagraphs, rules, paragraph, p => p.LineSpacing, r => r.LineSpacing, baseline.CommonLineSpacing);
            var expectedFirstLineIndent = ExpectedParagraphValue(templateParagraphs, rules, paragraph, p => p.FirstLineIndent, r => r.FirstLineIndent, baseline.CommonFirstLineIndent);

            if (paragraph.Text.Any(IsCjkCharacter))
            {
                AddIfDifferent(issues, "F", "错误", "字体字号", paragraph, "中文字体不符合模板",
                    expectedChineseFontName, paragraph.ChineseFontName, $"选中第 {paragraph.Index} 段中文文字，将中文字体改为 {expectedChineseFontName}。");
            }

            if (paragraph.Text.Any(IsAsciiLetterOrDigit))
            {
                AddIfDifferent(issues, "F", "错误", "字体字号", paragraph, "西文字体不符合模板",
                    expectedWesternFontName, paragraph.WesternFontName, $"选中第 {paragraph.Index} 段英文和数字，将西文字体改为 {expectedWesternFontName}。");
            }

            AddIfDifferent(issues, "F", "错误", "字体字号", paragraph, "字号不符合模板",
                expectedFontSize, paragraph.FontSize, $"选中第 {paragraph.Index} 段文字，将字号改为 {expectedFontSize}。");

            AddIfDifferent(issues, "S", "错误", "段落", paragraph, "对齐方式不符合模板",
                expectedJustification, paragraph.Justification, $"打开段落设置，将对齐方式改为 {expectedJustification}。");

            AddIfDifferent(issues, "S", "错误", "段落", paragraph, "段前间距不符合模板",
                expectedSpacingBefore, paragraph.SpacingBefore, $"打开段落设置，将段前间距改为 {expectedSpacingBefore}。");

            AddIfDifferent(issues, "S", "错误", "段落", paragraph, "段后间距不符合模板",
                expectedSpacingAfter, paragraph.SpacingAfter, $"打开段落设置，将段后间距改为 {expectedSpacingAfter}。");

            AddIfDifferent(issues, "S", "错误", "段落", paragraph, "行距不符合模板",
                expectedLineSpacing, paragraph.LineSpacing, $"打开段落设置，将行距改为 {expectedLineSpacing}。");

            AddIfDifferent(issues, "S", "错误", "段落", paragraph, "首行缩进不符合模板",
                expectedFirstLineIndent, paragraph.FirstLineIndent, $"打开段落设置，将首行缩进改为 {expectedFirstLineIndent}。");
        }
    }

    private void AddUnmatchedBlockIssueIfNeeded(
        List<FormatIssue> issues,
        IReadOnlyList<ParagraphSnapshot> templateParagraphs,
        ParagraphSnapshot paragraph)
    {
        if (paragraph.Area == "正文" || FindTemplateParagraph(templateParagraphs, paragraph) is not null)
        {
            return;
        }

        issues.Add(new FormatIssue
        {
            IssueCode = NextCode("I"),
            Severity = "需确认",
            Category = "块匹配",
            Location = LocationText(paragraph),
            TargetElementId = paragraph.TargetElementId,
            Area = paragraph.Area,
            Title = "未找到对应模板块",
            Expected = "应匹配同类型模板块后再判断格式",
            Actual = $"当前识别为{paragraph.BlockType}",
            Suggestion = "请确认该区域块是否属于模板要求；系统不会用正文全文规则判断非正文区域。",
            RequiresConfirmation = true,
            IsUnchecked = true
        });
    }

    private void AddRunIssues(
        List<FormatIssue> issues,
        IReadOnlyList<ParagraphSnapshot> templateParagraphs,
        IReadOnlyList<TemplateFormatRule> rules,
        TemplateBaseline baseline,
        IEnumerable<ParagraphSnapshot> paragraphs)
    {
        if (string.IsNullOrWhiteSpace(baseline.CommonChineseFontName) &&
            string.IsNullOrWhiteSpace(baseline.CommonWesternFontName) &&
            string.IsNullOrWhiteSpace(baseline.CommonFontSize))
        {
            return;
        }

        foreach (var paragraph in paragraphs.Where(p => !p.IsEmpty))
        {
            var templateParagraph = FindTemplateParagraph(templateParagraphs, paragraph);
            var expectedChineseFontName = ExpectedParagraphValue(templateParagraphs, rules, paragraph, p => p.ChineseFontName, r => r.ChineseFontName, baseline.CommonChineseFontName);
            var expectedWesternFontName = ExpectedParagraphValue(templateParagraphs, rules, paragraph, p => p.WesternFontName, r => r.WesternFontName, baseline.CommonWesternFontName);
            var expectedFontSize = ExpectedParagraphValue(templateParagraphs, rules, paragraph, p => p.FontSize, r => r.FontSize, baseline.CommonFontSize);

            var mismatchedChineseFonts = paragraph.Runs
                .Where(run => run.CjkCharacterCount > 0)
                .Select(run => (Run: run, Expected: ExpectedRunValue(templateParagraph, paragraph, run.Index, template => template.ChineseFontName, expectedChineseFontName)))
                .Where(item => !string.IsNullOrWhiteSpace(item.Expected) && !string.IsNullOrWhiteSpace(item.Run.ChineseFontName) && item.Run.ChineseFontName != item.Expected)
                .Select(item => item.Run.ChineseFontName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .ToList();

            if (mismatchedChineseFonts.Count > 0 && paragraph.ChineseFontName == expectedChineseFontName)
            {
                issues.Add(new FormatIssue
                {
                    IssueCode = NextCode("F"),
                    Severity = "错误",
                    Category = "字体字号",
                    Location = LocationText(paragraph),
                    TargetElementId = paragraph.TargetElementId,
                    Area = paragraph.Area,
                    Title = "段内部分中文字体不符合模板",
                    Expected = expectedChineseFontName,
                    Actual = string.Join("、", mismatchedChineseFonts),
                    Suggestion = $"选中第 {paragraph.Index} 段中中文字体不一致的文字，将中文字体改为 {expectedChineseFontName}。"
                });
            }

            var mismatchedWesternFonts = paragraph.Runs
                .Where(run => run.WesternCharacterCount > 0)
                .Select(run => (Run: run, Expected: ExpectedRunValue(templateParagraph, paragraph, run.Index, template => template.WesternFontName, expectedWesternFontName)))
                .Where(item => !string.IsNullOrWhiteSpace(item.Expected) && !string.IsNullOrWhiteSpace(item.Run.WesternFontName) && item.Run.WesternFontName != item.Expected)
                .Select(item => item.Run.WesternFontName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .ToList();

            if (mismatchedWesternFonts.Count > 0 && paragraph.WesternFontName == expectedWesternFontName)
            {
                issues.Add(new FormatIssue
                {
                    IssueCode = NextCode("F"),
                    Severity = "错误",
                    Category = "字体字号",
                    Location = LocationText(paragraph),
                    TargetElementId = paragraph.TargetElementId,
                    Area = paragraph.Area,
                    Title = "段内部分西文字体不符合模板",
                    Expected = expectedWesternFontName,
                    Actual = string.Join("、", mismatchedWesternFonts),
                    Suggestion = $"选中第 {paragraph.Index} 段中英文或数字字体不一致的文字，将西文字体改为 {expectedWesternFontName}。"
                });
            }

            var mismatchedSizes = paragraph.Runs
                .Select(run => (Run: run, Expected: ExpectedRunValue(templateParagraph, paragraph, run.Index, template => template.FontSize, expectedFontSize)))
                .Where(item => !string.IsNullOrWhiteSpace(item.Expected) && !string.IsNullOrWhiteSpace(item.Run.FontSize) && item.Run.FontSize != item.Expected)
                .Select(item => item.Run.FontSize)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .ToList();

            if (mismatchedSizes.Count > 0 && paragraph.FontSize == expectedFontSize)
            {
                issues.Add(new FormatIssue
                {
                    IssueCode = NextCode("F"),
                    Severity = "错误",
                    Category = "字体字号",
                    Location = LocationText(paragraph),
                    TargetElementId = paragraph.TargetElementId,
                    Area = paragraph.Area,
                    Title = "段内部分文字字号不符合模板",
                    Expected = expectedFontSize,
                    Actual = string.Join("、", mismatchedSizes),
                    Suggestion = $"选中第 {paragraph.Index} 段中字号不一致的文字，将字号改为 {expectedFontSize}。"
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
                Severity = "需确认",
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
                Severity = "错误",
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
            Severity = "错误",
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
            Location = LocationText(paragraph),
            TargetElementId = paragraph.TargetElementId,
            Area = paragraph.Area,
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

    private static string ExpectedParagraphValue(
        IReadOnlyList<ParagraphSnapshot> templateParagraphs,
        IReadOnlyList<TemplateFormatRule> rules,
        ParagraphSnapshot targetParagraph,
        Func<ParagraphSnapshot, string> selector,
        Func<TemplateFormatRule, string> ruleSelector,
        string fallback)
    {
        var rule = FindRule(rules, targetParagraph);
        if (rule is not null)
        {
            var value = ruleSelector(rule);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        var templateParagraph = FindTemplateParagraph(templateParagraphs, targetParagraph);

        if (templateParagraph is not null && !templateParagraph.IsEmpty)
        {
            var value = selector(templateParagraph);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return "";
        }

        return targetParagraph.Area == "正文" ? fallback : "";
    }

    private static TemplateFormatRule? FindRule(IReadOnlyList<TemplateFormatRule> rules, ParagraphSnapshot paragraph)
    {
        return rules.FirstOrDefault(rule =>
            string.Equals(rule.Area, paragraph.Area, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(rule.BlockType, paragraph.BlockType, StringComparison.OrdinalIgnoreCase));
    }

    private static ParagraphSnapshot? FindTemplateParagraph(IReadOnlyList<ParagraphSnapshot> templateParagraphs, ParagraphSnapshot targetParagraph)
    {
        if (targetParagraph.Area == "正文")
        {
            var normalizedText = NormalizeMatchText(targetParagraph.Text);
            if (!string.IsNullOrWhiteSpace(normalizedText))
            {
                var byText = templateParagraphs
                    .Where(template => !template.IsEmpty && template.Area == targetParagraph.Area)
                    .Where(template => NormalizeMatchText(template.Text) == normalizedText)
                    .Take(2)
                    .ToList();

                if (byText.Count == 1)
                {
                    return byText[0];
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(targetParagraph.BlockKey))
        {
            var byBlockKey = templateParagraphs.FirstOrDefault(template =>
                !template.IsEmpty &&
                template.BlockKey == targetParagraph.BlockKey);

            if (byBlockKey is not null)
            {
                return byBlockKey;
            }
        }

        var byBlock = templateParagraphs.FirstOrDefault(template =>
            !template.IsEmpty &&
            template.Area == targetParagraph.Area &&
            template.BlockType == targetParagraph.BlockType &&
            template.BlockIndex == targetParagraph.BlockIndex);

        if (byBlock is not null)
        {
            return byBlock;
        }

        if (targetParagraph.Area == "正文" &&
            targetParagraph.Index > 0 &&
            targetParagraph.Index <= templateParagraphs.Count)
        {
            return templateParagraphs[targetParagraph.Index - 1];
        }

        return null;
    }

    private static string NormalizeMatchText(string value)
    {
        return new string(value.Where(character => !char.IsWhiteSpace(character)).ToArray()).Trim();
    }

    private static bool IsCjkCharacter(char character)
    {
        return character is >= '\u4e00' and <= '\u9fff';
    }

    private static bool IsAsciiLetterOrDigit(char character)
    {
        return character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9';
    }

    private static string LocationText(ParagraphSnapshot paragraph)
    {
        return $"{paragraph.Area} / {paragraph.BlockType}（第 {paragraph.Index} 段）";
    }

    private static string ExpectedRunValue(
        ParagraphSnapshot? templateParagraph,
        ParagraphSnapshot targetParagraph,
        int runIndex,
        Func<RunFormatSnapshot, string> selector,
        string paragraphFallback)
    {
        if (!string.IsNullOrWhiteSpace(paragraphFallback))
        {
            return paragraphFallback;
        }

        var templateRun = templateParagraph?.Runs.FirstOrDefault(run => run.Index == runIndex);
        if (templateRun is not null)
        {
            var value = selector(templateRun);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return "";
        }

        return templateParagraph is null && targetParagraph.Area == "正文" ? paragraphFallback : "";
    }

    private static List<IssueSummary> BuildSummaries(IEnumerable<string> names)
    {
        return names
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .GroupBy(name => name)
            .Select(group => new IssueSummary { Name = group.Key, Count = group.Count() })
            .OrderByDescending(summary => summary.Count)
            .ThenBy(summary => summary.Name)
            .ToList();
    }
}
