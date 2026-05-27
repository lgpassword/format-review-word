using WordFormatAnalyzer.Models;

namespace WordFormatAnalyzer.Services;

public sealed class TemplateRuleService
{
    private readonly TemplateRuleValueNormalizer _normalizer;

    public TemplateRuleService(TemplateRuleValueNormalizer normalizer)
    {
        _normalizer = normalizer;
    }

    public List<TemplateFormatRule> BuildRules(DocumentAnalysisResult analysis, string source)
    {
        return analysis.Paragraphs
            .Where(paragraph => !paragraph.IsEmpty)
            .GroupBy(paragraph => new { paragraph.Area, paragraph.BlockType })
            .Select(group => BuildRule(group.Key.Area, group.Key.BlockType, group, source))
            .OrderBy(rule => AreaOrder(rule.Area))
            .ThenBy(rule => rule.Area)
            .ThenBy(rule => rule.BlockType)
            .ToList();
    }

    public List<TemplateFormatRule> MergeRules(
        IEnumerable<TemplateFormatRule> primaryRules,
        IEnumerable<TemplateFormatRule> referenceRules)
    {
        var merged = primaryRules.Select(Clone).ToDictionary(rule => rule.RuleId, StringComparer.OrdinalIgnoreCase);

        foreach (var reference in referenceRules)
        {
            var copy = Clone(reference);
            copy.Source = "合格参考模板";
            merged[copy.RuleId] = copy;
        }

        return merged.Values
            .OrderBy(rule => AreaOrder(rule.Area))
            .ThenBy(rule => rule.Area)
            .ThenBy(rule => rule.BlockType)
            .ToList();
    }

    public List<TemplateFormatRule> ApplyEdits(
        IReadOnlyList<TemplateFormatRule> rules,
        IReadOnlyDictionary<string, TemplateRuleEditInput> edits)
    {
        var result = rules.Select(Clone).ToList();
        foreach (var rule in result)
        {
            if (!edits.TryGetValue(rule.RuleId, out var edit))
            {
                continue;
            }

            ApplyEdit(rule, edit);
        }

        return result;
    }

    public TemplateFormatRule? FindRule(IEnumerable<TemplateFormatRule> rules, string area, string blockType)
    {
        return rules.FirstOrDefault(rule =>
            string.Equals(rule.Area, area, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(rule.BlockType, blockType, StringComparison.OrdinalIgnoreCase));
    }

    public static TemplateFormatRule Clone(TemplateFormatRule source)
    {
        return new TemplateFormatRule
        {
            RuleId = source.RuleId,
            Area = source.Area,
            BlockType = source.BlockType,
            DisplayName = source.DisplayName,
            Source = source.Source,
            ChineseFontName = source.ChineseFontName,
            ChineseFontNameRaw = source.ChineseFontNameRaw,
            WesternFontName = source.WesternFontName,
            WesternFontNameRaw = source.WesternFontNameRaw,
            FontSize = source.FontSize,
            FontSizeRaw = source.FontSizeRaw,
            Justification = source.Justification,
            JustificationRaw = source.JustificationRaw,
            SpacingBefore = source.SpacingBefore,
            SpacingBeforeRaw = source.SpacingBeforeRaw,
            SpacingAfter = source.SpacingAfter,
            SpacingAfterRaw = source.SpacingAfterRaw,
            LineSpacing = source.LineSpacing,
            LineSpacingRaw = source.LineSpacingRaw,
            FirstLineIndent = source.FirstLineIndent,
            FirstLineIndentRaw = source.FirstLineIndentRaw
        };
    }

    private TemplateFormatRule BuildRule(
        string area,
        string blockType,
        IEnumerable<ParagraphSnapshot> paragraphs,
        string source)
    {
        var nonEmpty = paragraphs.Where(paragraph => !paragraph.IsEmpty).ToList();
        return new TemplateFormatRule
        {
            RuleId = RuleId(area, blockType),
            Area = area,
            BlockType = blockType,
            DisplayName = $"{area} / {blockType}",
            Source = source,
            ChineseFontName = MostCommon(nonEmpty.Select(paragraph => paragraph.ChineseFontName)),
            ChineseFontNameRaw = MostCommon(nonEmpty.Select(paragraph => paragraph.ChineseFontNameRaw)),
            WesternFontName = MostCommon(nonEmpty.Select(paragraph => paragraph.WesternFontName)),
            WesternFontNameRaw = MostCommon(nonEmpty.Select(paragraph => paragraph.WesternFontNameRaw)),
            FontSize = MostCommon(nonEmpty.Select(paragraph => paragraph.FontSize)),
            FontSizeRaw = MostCommon(nonEmpty.Select(paragraph => paragraph.FontSizeRaw)),
            Justification = MostCommon(nonEmpty.Select(paragraph => paragraph.Justification)),
            JustificationRaw = MostCommon(nonEmpty.Select(paragraph => paragraph.JustificationRaw)),
            SpacingBefore = MostCommon(nonEmpty.Select(paragraph => paragraph.SpacingBefore)),
            SpacingBeforeRaw = MostCommon(nonEmpty.Select(paragraph => paragraph.SpacingBeforeRaw)),
            SpacingAfter = MostCommon(nonEmpty.Select(paragraph => paragraph.SpacingAfter)),
            SpacingAfterRaw = MostCommon(nonEmpty.Select(paragraph => paragraph.SpacingAfterRaw)),
            LineSpacing = MostCommon(nonEmpty.Select(paragraph => paragraph.LineSpacing)),
            LineSpacingRaw = MostCommon(nonEmpty.Select(paragraph => paragraph.LineSpacingRaw)),
            FirstLineIndent = MostCommon(nonEmpty.Select(paragraph => paragraph.FirstLineIndent)),
            FirstLineIndentRaw = MostCommon(nonEmpty.Select(paragraph => paragraph.FirstLineIndentRaw))
        };
    }

    private void ApplyEdit(TemplateFormatRule rule, TemplateRuleEditInput edit)
    {
        SetFont(value => rule.ChineseFontName = value, value => rule.ChineseFontNameRaw = value, edit.ChineseFontName);
        SetFont(value => rule.WesternFontName = value, value => rule.WesternFontNameRaw = value, edit.WesternFontName);
        SetPair(value => rule.FontSize = value.DisplayValue, value => rule.FontSizeRaw = value.RawValue, _normalizer.NormalizeFontSize(edit.FontSize));
        SetPair(value => rule.Justification = value.DisplayValue, value => rule.JustificationRaw = value.RawValue, _normalizer.NormalizeJustification(edit.Justification));
        SetPair(value => rule.SpacingBefore = value.DisplayValue, value => rule.SpacingBeforeRaw = value.RawValue, _normalizer.NormalizeSpacing(edit.SpacingBefore));
        SetPair(value => rule.SpacingAfter = value.DisplayValue, value => rule.SpacingAfterRaw = value.RawValue, _normalizer.NormalizeSpacing(edit.SpacingAfter));
        SetPair(value => rule.LineSpacing = value.DisplayValue, value => rule.LineSpacingRaw = value.RawValue, _normalizer.NormalizeLineSpacing(edit.LineSpacing));
        SetPair(value => rule.FirstLineIndent = value.DisplayValue, value => rule.FirstLineIndentRaw = value.RawValue, _normalizer.NormalizeFirstLineIndent(edit.FirstLineIndent));

        if (edit.HasAnyValue)
        {
            rule.Source = "本次编辑";
        }
    }

    private void SetFont(Action<string> setDisplay, Action<string> setRaw, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var normalized = _normalizer.NormalizeFont(value);
        setDisplay(normalized.DisplayValue);
        setRaw(normalized.RawValue);
    }

    private static void SetPair(
        Action<TemplateRuleValue> set,
        Action<TemplateRuleValue> setRaw,
        TemplateRuleValue value)
    {
        if (value.IsEmpty)
        {
            return;
        }

        set(value);
        setRaw(value);
    }

    public List<TemplateFormatRule> ApplyResolvedConflicts(
        IReadOnlyList<TemplateFormatRule> rules,
        IEnumerable<TemplateRuleConflict> conflicts)
    {
        var result = rules.Select(Clone).ToList();
        foreach (var conflict in conflicts.Where(item => item.IsResolved))
        {
            foreach (var rule in result)
            {
                ApplyConflict(rule, conflict);
            }
        }

        return result;
    }

    private static void ApplyConflict(TemplateFormatRule rule, TemplateRuleConflict conflict)
    {
        switch (conflict.RuleName)
        {
            case "常见中文字体":
                rule.ChineseFontName = conflict.SelectedValue;
                rule.ChineseFontNameRaw = conflict.RawSelectedValue;
                break;
            case "常见西文字体":
                rule.WesternFontName = conflict.SelectedValue;
                rule.WesternFontNameRaw = conflict.RawSelectedValue;
                break;
            case "常见字体":
                return;
            case "常见字号":
                rule.FontSize = conflict.SelectedValue;
                rule.FontSizeRaw = conflict.RawSelectedValue;
                break;
            case "对齐方式":
                rule.Justification = conflict.SelectedValue;
                rule.JustificationRaw = conflict.RawSelectedValue;
                break;
            case "段前间距":
                rule.SpacingBefore = conflict.SelectedValue;
                rule.SpacingBeforeRaw = conflict.RawSelectedValue;
                break;
            case "段后间距":
                rule.SpacingAfter = conflict.SelectedValue;
                rule.SpacingAfterRaw = conflict.RawSelectedValue;
                break;
            case "行距":
                rule.LineSpacing = conflict.SelectedValue;
                rule.LineSpacingRaw = conflict.RawSelectedValue;
                break;
            case "首行缩进":
                rule.FirstLineIndent = conflict.SelectedValue;
                rule.FirstLineIndentRaw = conflict.RawSelectedValue;
                break;
            default:
                return;
        }

        rule.Source = conflict.Source;
    }

    private static string RuleId(string area, string blockType)
    {
        return $"{area}:{blockType}";
    }

    private static string MostCommon(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .GroupBy(value => value)
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key)
            .FirstOrDefault() ?? "";
    }

    private static int AreaOrder(string area)
    {
        return area switch
        {
            "封面" => 1,
            "声明" => 2,
            "摘要" => 3,
            "目录" => 4,
            "正文" => 5,
            "参考文献" => 6,
            "附录" => 7,
            "致谢" => 8,
            _ => 99
        };
    }
}

public sealed class TemplateRuleEditInput
{
    public string ChineseFontName { get; set; } = "";
    public string WesternFontName { get; set; } = "";
    public string FontSize { get; set; } = "";
    public string Justification { get; set; } = "";
    public string SpacingBefore { get; set; } = "";
    public string SpacingAfter { get; set; } = "";
    public string LineSpacing { get; set; } = "";
    public string FirstLineIndent { get; set; } = "";

    public bool HasAnyValue => new[]
    {
        ChineseFontName,
        WesternFontName,
        FontSize,
        Justification,
        SpacingBefore,
        SpacingAfter,
        LineSpacing,
        FirstLineIndent
    }.Any(value => !string.IsNullOrWhiteSpace(value));
}
