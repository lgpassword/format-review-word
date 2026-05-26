using WordFormatAnalyzer.Models;

namespace WordFormatAnalyzer.Services;

public sealed class TemplateRuleConflictService
{
    private readonly WordTerminologyService _terms;

    public TemplateRuleConflictService(WordTerminologyService terms)
    {
        _terms = terms;
    }

    public List<TemplateRuleConflict> FindConflicts(DocumentAnalysisResult primary, DocumentAnalysisResult reference)
    {
        var conflicts = new List<TemplateRuleConflict>();
        AddConflict(conflicts, "字体字号", "常见字体", primary.Baseline.CommonFontName, reference.Baseline.CommonFontName, primary.Baseline.CommonFontNameRaw, reference.Baseline.CommonFontNameRaw, reference.FileName);
        AddConflict(conflicts, "字体字号", "常见字号", primary.Baseline.CommonFontSize, reference.Baseline.CommonFontSize, primary.Baseline.CommonFontSizeRaw, reference.Baseline.CommonFontSizeRaw, reference.FileName);
        AddConflict(conflicts, "段落", "对齐方式", primary.Baseline.CommonJustification, reference.Baseline.CommonJustification, primary.Baseline.CommonJustificationRaw, reference.Baseline.CommonJustificationRaw, reference.FileName);
        AddConflict(conflicts, "段落", "段前间距", primary.Baseline.CommonSpacingBefore, reference.Baseline.CommonSpacingBefore, primary.Baseline.CommonSpacingBeforeRaw, reference.Baseline.CommonSpacingBeforeRaw, reference.FileName);
        AddConflict(conflicts, "段落", "段后间距", primary.Baseline.CommonSpacingAfter, reference.Baseline.CommonSpacingAfter, primary.Baseline.CommonSpacingAfterRaw, reference.Baseline.CommonSpacingAfterRaw, reference.FileName);
        AddConflict(conflicts, "段落", "行距", primary.Baseline.CommonLineSpacing, reference.Baseline.CommonLineSpacing, primary.Baseline.CommonLineSpacingRaw, reference.Baseline.CommonLineSpacingRaw, reference.FileName);
        AddConflict(conflicts, "段落", "首行缩进", primary.Baseline.CommonFirstLineIndent, reference.Baseline.CommonFirstLineIndent, primary.Baseline.CommonFirstLineIndentRaw, reference.Baseline.CommonFirstLineIndentRaw, reference.FileName);
        AddConflict(conflicts, "表格", "常见列数", ToText(primary.Baseline.CommonTableColumnCount), ToText(reference.Baseline.CommonTableColumnCount), primary.Baseline.CommonTableColumnCount.ToString(), reference.Baseline.CommonTableColumnCount.ToString(), reference.FileName);
        return conflicts;
    }

    public void MergeConflicts(ICollection<TemplateRuleConflict> existingConflicts, IEnumerable<TemplateRuleConflict> newConflicts)
    {
        foreach (var newConflict in newConflicts)
        {
            var existing = existingConflicts.FirstOrDefault(item =>
                item.Category == newConflict.Category &&
                item.RuleName == newConflict.RuleName);

            if (existing is null)
            {
                existingConflicts.Add(newConflict);
                continue;
            }

            foreach (var option in newConflict.ReferenceOptions)
            {
                AddReferenceOption(existing, option);
            }
        }
    }

    public TemplateBaseline ApplySelections(TemplateBaseline baseline, IEnumerable<TemplateRuleConflict> conflicts)
    {
        foreach (var conflict in conflicts.Where(item => item.IsResolved))
        {
            switch (conflict.RuleName)
            {
                case "常见字体":
                    baseline.CommonFontName = conflict.SelectedValue;
                    baseline.CommonFontNameRaw = conflict.RawSelectedValue;
                    break;
                case "常见字号":
                    baseline.CommonFontSize = conflict.SelectedValue;
                    baseline.CommonFontSizeRaw = conflict.RawSelectedValue;
                    break;
                case "对齐方式":
                    baseline.CommonJustification = conflict.SelectedValue;
                    baseline.CommonJustificationRaw = conflict.RawSelectedValue;
                    break;
                case "段前间距":
                    baseline.CommonSpacingBefore = conflict.SelectedValue;
                    baseline.CommonSpacingBeforeRaw = conflict.RawSelectedValue;
                    break;
                case "段后间距":
                    baseline.CommonSpacingAfter = conflict.SelectedValue;
                    baseline.CommonSpacingAfterRaw = conflict.RawSelectedValue;
                    break;
                case "行距":
                    baseline.CommonLineSpacing = conflict.SelectedValue;
                    baseline.CommonLineSpacingRaw = conflict.RawSelectedValue;
                    break;
                case "首行缩进":
                    baseline.CommonFirstLineIndent = conflict.SelectedValue;
                    baseline.CommonFirstLineIndentRaw = conflict.RawSelectedValue;
                    break;
                case "常见列数":
                    if (int.TryParse(conflict.RawSelectedValue, out var columnCount))
                    {
                        baseline.CommonTableColumnCount = columnCount;
                    }
                    break;
            }
        }

        return baseline;
    }

    public void ResolveConflict(TemplateRuleConflict conflict, string selectedSource, string? customValue)
    {
        var option = conflict.ReferenceOptions.FirstOrDefault(item => item.Source == selectedSource);
        if (option is not null)
        {
            conflict.SelectedValue = option.DisplayValue;
            conflict.RawSelectedValue = option.RawValue;
            conflict.Source = option.Source;
        }
        else if (selectedSource.Equals("reference", StringComparison.OrdinalIgnoreCase))
        {
            conflict.SelectedValue = conflict.ReferenceValue;
            conflict.RawSelectedValue = conflict.ReferenceRawValue;
            conflict.Source = "合格参考模板";
        }
        else if (selectedSource.Equals("custom", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(customValue))
        {
            var normalized = NormalizeCustomValue(conflict.RuleName, customValue);
            conflict.SelectedValue = normalized.DisplayValue;
            conflict.RawSelectedValue = normalized.RawValue;
            conflict.Source = "本次自定义";
        }
        else
        {
            conflict.SelectedValue = conflict.PrimaryValue;
            conflict.RawSelectedValue = conflict.PrimaryRawValue;
            conflict.Source = "总模板";
        }

        conflict.IsResolved = true;
    }

    private static void AddConflict(
        ICollection<TemplateRuleConflict> conflicts,
        string category,
        string ruleName,
        string primaryValue,
        string referenceValue,
        string primaryRawValue,
        string referenceRawValue,
        string referenceFileName)
    {
        if (string.IsNullOrWhiteSpace(primaryValue) || string.IsNullOrWhiteSpace(referenceValue) ||
            string.Equals(primaryValue, referenceValue, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var conflict = new TemplateRuleConflict
        {
            ConflictId = $"{category}-{ruleName}-{conflicts.Count + 1}",
            Category = category,
            RuleName = ruleName,
            PrimaryValue = primaryValue,
            PrimaryRawValue = string.IsNullOrWhiteSpace(primaryRawValue) ? primaryValue : primaryRawValue,
            ReferenceValue = referenceValue,
            ReferenceRawValue = string.IsNullOrWhiteSpace(referenceRawValue) ? referenceValue : referenceRawValue,
            SelectedValue = primaryValue,
            RawSelectedValue = string.IsNullOrWhiteSpace(primaryRawValue) ? primaryValue : primaryRawValue,
            Source = "总模板",
            IsResolved = false
        };

        AddReferenceOption(conflict, new TemplateRuleOption
        {
            Source = string.IsNullOrWhiteSpace(referenceFileName) ? "合格参考模板" : referenceFileName,
            DisplayValue = referenceValue,
            RawValue = string.IsNullOrWhiteSpace(referenceRawValue) ? referenceValue : referenceRawValue
        });

        conflicts.Add(conflict);
    }

    private static void AddReferenceOption(TemplateRuleConflict conflict, TemplateRuleOption option)
    {
        if (conflict.ReferenceOptions.Any(existing =>
                existing.Source == option.Source ||
                string.Equals(existing.DisplayValue, option.DisplayValue, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        conflict.ReferenceOptions.Add(option);
    }

    private static string ToText(int value)
    {
        return value > 0 ? $"{value} 列" : "";
    }

    private (string DisplayValue, string RawValue) NormalizeCustomValue(string ruleName, string value)
    {
        var trimmed = value.Trim();
        return ruleName switch
        {
            "常见字体" => (_terms.FontName(trimmed), trimmed),
            "常见字号" => NormalizeFontSize(trimmed),
            "对齐方式" => NormalizeJustification(trimmed),
            "段前间距" or "段后间距" or "首行缩进" => NormalizeSpacing(trimmed),
            "行距" => NormalizeLineSpacing(trimmed),
            "常见列数" => NormalizeColumnCount(trimmed),
            _ => (trimmed, trimmed)
        };
    }

    private (string DisplayValue, string RawValue) NormalizeFontSize(string value)
    {
        if (TryGetFontHalfPoints(value, out var halfPoints))
        {
            var raw = halfPoints.ToString();
            return (_terms.FontSize(raw), raw);
        }

        return (_terms.FontSize(value), value);
    }

    private (string DisplayValue, string RawValue) NormalizeJustification(string value)
    {
        var raw = value switch
        {
            "左对齐" => "left",
            "居中对齐" or "居中" => "center",
            "右对齐" => "right",
            "两端对齐" => "both",
            "分散对齐" => "distribute",
            _ => value
        };

        return (_terms.Justification(raw), raw);
    }

    private (string DisplayValue, string RawValue) NormalizeSpacing(string value)
    {
        if (TryGetPointValue(value, out var points))
        {
            var raw = Convert.ToInt32(Math.Round(points * 20)).ToString();
            return (_terms.Spacing(raw), raw);
        }

        return (value, value);
    }

    private (string DisplayValue, string RawValue) NormalizeLineSpacing(string value)
    {
        var raw = value switch
        {
            "单倍行距" => "240",
            "1.5倍行距" or "1.5 倍行距" => "360",
            "2倍行距" or "2 倍行距" => "480",
            _ => value
        };

        if (raw.Contains('磅') && TryGetPointValue(raw, out var points))
        {
            raw = Convert.ToInt32(Math.Round(points * 20)).ToString();
        }

        return (_terms.LineSpacing(raw), raw);
    }

    private static (string DisplayValue, string RawValue) NormalizeColumnCount(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var count) && count > 0
            ? ($"{count} 列", count.ToString())
            : (value, value);
    }

    private static bool TryGetFontHalfPoints(string value, out int halfPoints)
    {
        halfPoints = 0;
        var fontSizes = new Dictionary<string, int>
        {
            ["初号"] = 84,
            ["小初"] = 72,
            ["一号"] = 52,
            ["小一"] = 48,
            ["二号"] = 44,
            ["小二"] = 36,
            ["三号"] = 32,
            ["小三"] = 30,
            ["四号"] = 28,
            ["小四"] = 24,
            ["五号"] = 21,
            ["小五"] = 18,
            ["六号"] = 15,
            ["小六"] = 13,
            ["七号"] = 11,
            ["八号"] = 10
        };

        if (fontSizes.TryGetValue(value, out halfPoints))
        {
            return true;
        }

        var normalized = value.Replace("磅", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (!double.TryParse(normalized, out var points))
        {
            return false;
        }

        halfPoints = Convert.ToInt32(Math.Round(points * 2));
        return true;
    }

    private static bool TryGetPointValue(string value, out double points)
    {
        var normalized = value
            .Replace("固定值", "", StringComparison.OrdinalIgnoreCase)
            .Replace("首行缩进", "", StringComparison.OrdinalIgnoreCase)
            .Replace("磅", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        return double.TryParse(normalized, out points);
    }
}
