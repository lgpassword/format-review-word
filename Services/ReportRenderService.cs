using System.Net;
using System.Text;
using WordFormatAnalyzer.Models;

namespace WordFormatAnalyzer.Services;

public sealed class ReportRenderService
{
    private readonly IssueTextService _issueText;

    public ReportRenderService(IssueTextService issueText)
    {
        _issueText = issueText;
    }

    public string WriteHtmlReport(AnalysisReport report, string outputPath)
    {
        var html = RenderHtml(report);
        File.WriteAllText(outputPath, html, Encoding.UTF8);
        return outputPath;
    }

    public string RenderHtml(AnalysisReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"zh-CN\"><head><meta charset=\"utf-8\"><title>格式检测报告</title>");
        builder.AppendLine("<style>body{font-family:Arial,'Microsoft YaHei',sans-serif;margin:24px;color:#1f2937}table{border-collapse:collapse;width:100%;margin-top:16px}th,td{border:1px solid #d1d5db;padding:8px;text-align:left;vertical-align:top}th{background:#f3f4f6}.summary{display:flex;gap:16px;flex-wrap:wrap}.box{border:1px solid #d1d5db;padding:12px;min-width:160px}.sev{font-weight:700;color:#b91c1c}.muted{color:#6b7280}</style>");
        builder.AppendLine("</head><body>");
        builder.AppendLine("<h1>Word 格式检测报告</h1>");
        builder.AppendLine($"<p class=\"muted\">报告编号：{Encode(report.ReportId)}　生成时间：{Encode(report.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"))}</p>");
        builder.AppendLine("<div class=\"summary\">");
        builder.AppendLine($"<div class=\"box\"><strong>模板文档</strong><br>{Encode(report.TemplateFileName)}</div>");
        builder.AppendLine($"<div class=\"box\"><strong>实际文档</strong><br>{Encode(report.TargetFileName)}</div>");
        builder.AppendLine($"<div class=\"box\"><strong>检测结论</strong><br>{Encode(report.Conclusion)}</div>");
        builder.AppendLine($"<div class=\"box\"><strong>问题总数</strong><br>{report.Issues.Count}</div>");
        builder.AppendLine("</div>");
        builder.AppendLine("<h2>问题列表</h2>");
        builder.AppendLine("<table><thead><tr><th>编号</th><th>级别</th><th>类型</th><th>位置</th><th>问题</th><th>模板要求</th><th>实际情况</th><th>修改建议</th></tr></thead><tbody>");

        foreach (var issue in report.Issues)
        {
            builder.AppendLine("<tr>");
            builder.AppendLine($"<td>{Encode(issue.IssueCode)}</td>");
            builder.AppendLine($"<td class=\"sev\">{Encode(issue.Severity)}</td>");
            builder.AppendLine($"<td>{Encode(issue.Category)}</td>");
            builder.AppendLine($"<td>{Encode(issue.Location)}</td>");
            builder.AppendLine($"<td>{Encode(_issueText.BuildReportTitle(issue))}</td>");
            builder.AppendLine($"<td>{Encode(RenderValue(issue, true))}</td>");
            builder.AppendLine($"<td>{Encode(RenderValue(issue, false))}</td>");
            builder.AppendLine($"<td>{Encode(issue.Suggestion)}</td>");
            builder.AppendLine("</tr>");
        }

        builder.AppendLine("</tbody></table>");
        builder.AppendLine("</body></html>");
        return builder.ToString();
    }

    private static string Encode(string value)
    {
        return WebUtility.HtmlEncode(value);
    }

    private string RenderValue(FormatIssue issue, bool expected)
    {
        if (issue.Category == "页面设置")
        {
            return expected ? NormalizePageValue(issue.Expected) : NormalizePageValue(issue.Actual);
        }

        return expected ? _issueText.BuildExpectedText(issue) : _issueText.BuildActualText(issue);
    }

    private static string NormalizePageValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        if (value.Contains("厘米", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        return value;
    }
}
