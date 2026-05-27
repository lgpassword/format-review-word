using WordFormatAnalyzer.Models;

namespace WordFormatAnalyzer.Services;

public sealed class IssueTextService
{
    public string BuildShortTitle(FormatIssue issue)
    {
        return issue.Category switch
        {
            "页面设置" => issue.Title.Replace("不符合模板", "与模板不一致"),
            "字体字号" => issue.Title switch
            {
                "字体不符合模板" => "字体与模板不一致",
                "中文字体不符合模板" => "中文字体与模板不一致",
                "西文字体不符合模板" => "西文字体与模板不一致",
                "字号不符合模板" => "字号与模板不一致",
                _ => issue.Title
            },
            "段落" => issue.Title switch
            {
                "对齐方式不符合模板" => "段落对齐方式与模板不一致",
                "段前间距不符合模板" => "段前间距与模板不一致",
                "段后间距不符合模板" => "段后间距与模板不一致",
                "行距不符合模板" => "行距与模板不一致",
                _ => issue.Title
            },
            "标点" => issue.Title,
            "表格" => "表格格式与模板不一致",
            "大空白" => "疑似存在多余空白",
            _ => issue.Title
        };
    }

    public string BuildComment(FormatIssue issue)
    {
        return $"""
            问题编号：{issue.IssueCode}
            问题类型：{issue.Category}
            所属位置：{LocationText(issue)}
            发现问题：{BuildShortTitle(issue)}
            当前情况：{NormalizeValue(issue.Actual)}
            应改为：{NormalizeValue(issue.Expected)}
            修改办法：{issue.Suggestion}
            """;
    }

    public string BuildReportTitle(FormatIssue issue)
    {
        return BuildShortTitle(issue);
    }

    public string BuildExpectedText(FormatIssue issue)
    {
        return NormalizeValue(issue.Expected);
    }

    public string BuildActualText(FormatIssue issue)
    {
        return NormalizeValue(issue.Actual);
    }

    private static string NormalizeValue(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "未识别，请按 Word 对话框核对" : value;
    }

    private static string LocationText(FormatIssue issue)
    {
        if (!string.IsNullOrWhiteSpace(issue.Area) &&
            !issue.Location.Contains(issue.Area, StringComparison.OrdinalIgnoreCase))
        {
            return $"{issue.Area} / {issue.Location}";
        }

        return issue.Location;
    }
}
