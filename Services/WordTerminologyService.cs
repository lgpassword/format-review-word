using System.Globalization;

namespace WordFormatAnalyzer.Services;

public sealed class WordTerminologyService
{
    private static readonly Dictionary<string, string> FontSizeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["84"] = "初号",
        ["72"] = "小初",
        ["52"] = "一号",
        ["48"] = "小一",
        ["44"] = "二号",
        ["36"] = "小二",
        ["32"] = "三号",
        ["30"] = "小三",
        ["28"] = "四号",
        ["24"] = "小四",
        ["21"] = "五号",
        ["18"] = "小五",
        ["15"] = "六号",
        ["13"] = "小六",
        ["11"] = "七号",
        ["10"] = "八号"
    };

    private static readonly Dictionary<string, string> FontAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["楷体_gb2312"] = "楷体",
        ["楷体-gb2312"] = "楷体",
        ["kaiti"] = "楷体",
        ["simkai"] = "楷体",
        ["simsun"] = "宋体",
        ["nsimsun"] = "新宋体",
        ["simhei"] = "黑体",
        ["fangsong"] = "仿宋",
        ["仿宋_gb2312"] = "仿宋",
        ["仿宋-gb2312"] = "仿宋",
        ["microsoft yahei"] = "微软雅黑"
    };

    public string FontName(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return "";
        }

        var trimmed = rawValue.Trim();
        return FontAliases.TryGetValue(trimmed, out var displayName) ? displayName : trimmed;
    }

    public string FontSize(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return "";
        }

        var trimmed = rawValue.Trim();
        if (FontSizeNames.TryGetValue(trimmed, out var displayName))
        {
            return displayName;
        }

        return int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var halfPoints)
            ? $"{halfPoints / 2.0:0.#} 磅"
            : trimmed;
    }

    public string Justification(string rawValue)
    {
        return NormalizeOpenXmlValue(rawValue) switch
        {
            "left" or "start" => "左对齐",
            "center" => "居中对齐",
            "right" or "end" => "右对齐",
            "both" => "两端对齐",
            "distribute" => "分散对齐",
            "" => "",
            _ => rawValue
        };
    }

    public string JustificationRaw(string rawValue)
    {
        return NormalizeOpenXmlValue(rawValue);
    }

    public string Spacing(string rawTwips)
    {
        if (string.IsNullOrWhiteSpace(rawTwips))
        {
            return "0 磅";
        }

        return int.TryParse(rawTwips, NumberStyles.Integer, CultureInfo.InvariantCulture, out var twips)
            ? $"{twips / 20.0:0.#} 磅"
            : rawTwips;
    }

    public string LineSpacing(string rawLine)
    {
        if (string.IsNullOrWhiteSpace(rawLine))
        {
            return "单倍行距";
        }

        if (!int.TryParse(rawLine, NumberStyles.Integer, CultureInfo.InvariantCulture, out var line))
        {
            return rawLine;
        }

        return line switch
        {
            240 => "单倍行距",
            360 => "1.5 倍行距",
            480 => "2 倍行距",
            _ => $"固定值 {line / 20.0:0.#} 磅"
        };
    }

    public string FirstLineIndent(string rawTwips)
    {
        if (string.IsNullOrWhiteSpace(rawTwips))
        {
            return "无首行缩进";
        }

        return int.TryParse(rawTwips, NumberStyles.Integer, CultureInfo.InvariantCulture, out var twips)
            ? twips == 0 ? "无首行缩进" : $"首行缩进 {twips / 20.0:0.#} 磅"
            : rawTwips;
    }

    public string PageOrientation(string rawValue)
    {
        return NormalizeOpenXmlValue(rawValue) switch
        {
            "portrait" => "纵向",
            "landscape" => "横向",
            "" => "",
            _ => rawValue
        };
    }

    private static string NormalizeOpenXmlValue(string rawValue)
    {
        var trimmed = rawValue.Trim();
        if (trimmed.Length == 0)
        {
            return "";
        }

        var lower = trimmed.ToLowerInvariant();
        return lower.Contains("justificationvalues", StringComparison.OrdinalIgnoreCase) ||
               lower.Contains("pageorientationvalues", StringComparison.OrdinalIgnoreCase)
            ? ""
            : lower;
    }
}
