using WordFormatAnalyzer.Models;

namespace WordFormatAnalyzer.Services;

public sealed class TemplateRuleValueNormalizer
{
    private readonly WordTerminologyService _terms;

    public TemplateRuleValueNormalizer(WordTerminologyService terms)
    {
        _terms = terms;
    }

    public TemplateRuleValue NormalizeFont(string value)
    {
        var trimmed = value.Trim();
        return string.IsNullOrWhiteSpace(trimmed)
            ? TemplateRuleValue.Empty
            : new TemplateRuleValue(_terms.FontName(trimmed), trimmed);
    }

    public TemplateRuleValue NormalizeFontSize(string value)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return TemplateRuleValue.Empty;
        }

        if (TryGetFontHalfPoints(trimmed, out var halfPoints))
        {
            var raw = halfPoints.ToString();
            return new TemplateRuleValue(_terms.FontSize(raw), raw);
        }

        return new TemplateRuleValue(_terms.FontSize(trimmed), trimmed);
    }

    public TemplateRuleValue NormalizeJustification(string value)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return TemplateRuleValue.Empty;
        }

        var raw = trimmed switch
        {
            "左对齐" => "left",
            "居中对齐" or "居中" => "center",
            "右对齐" => "right",
            "两端对齐" => "both",
            "分散对齐" => "distribute",
            _ => trimmed
        };

        return new TemplateRuleValue(_terms.Justification(raw), raw);
    }

    public TemplateRuleValue NormalizeSpacing(string value)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return TemplateRuleValue.Empty;
        }

        if (TryGetPointValue(trimmed, out var points))
        {
            var raw = Convert.ToInt32(Math.Round(points * 20)).ToString();
            return new TemplateRuleValue(_terms.Spacing(raw), raw);
        }

        return new TemplateRuleValue(trimmed, trimmed);
    }

    public TemplateRuleValue NormalizeFirstLineIndent(string value)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return TemplateRuleValue.Empty;
        }

        if (trimmed.Equals("无首行缩进", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("无", StringComparison.OrdinalIgnoreCase))
        {
            return new TemplateRuleValue(_terms.FirstLineIndent("0"), "0");
        }

        if (TryGetPointValue(trimmed, out var points))
        {
            var raw = Convert.ToInt32(Math.Round(points * 20)).ToString();
            return new TemplateRuleValue(_terms.FirstLineIndent(raw), raw);
        }

        return new TemplateRuleValue(trimmed, trimmed);
    }

    public TemplateRuleValue NormalizeLineSpacing(string value)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return TemplateRuleValue.Empty;
        }

        var raw = trimmed switch
        {
            "单倍行距" => "240",
            "1.5倍行距" or "1.5 倍行距" => "360",
            "2倍行距" or "2 倍行距" => "480",
            _ => trimmed
        };

        if (raw.Contains('磅') && TryGetPointValue(raw, out var points))
        {
            raw = Convert.ToInt32(Math.Round(points * 20)).ToString();
        }

        return new TemplateRuleValue(_terms.LineSpacing(raw), raw);
    }

    public TemplateRuleValue NormalizeColumnCount(string value)
    {
        var trimmed = value.Trim();
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var count) && count > 0
            ? new TemplateRuleValue($"{count} 列", count.ToString())
            : new TemplateRuleValue(trimmed, trimmed);
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

public sealed record TemplateRuleValue(string DisplayValue, string RawValue)
{
    public static TemplateRuleValue Empty { get; } = new("", "");
    public bool IsEmpty => string.IsNullOrWhiteSpace(DisplayValue) && string.IsNullOrWhiteSpace(RawValue);
}
