using System.Text.RegularExpressions;
using WordFormatAnalyzer.Models;

namespace WordFormatAnalyzer.Services;

public sealed class PunctuationIssueService
{
    private static readonly Regex EnglishPunctuationSpacing = new(@"[A-Za-z0-9][,.;:!?][A-Za-z0-9]", RegexOptions.Compiled);
    private static readonly Regex ChineseWithHalfWidthPunctuation = new(@"[\u4e00-\u9fff][,.;:!?]|[,.;:!?][\u4e00-\u9fff]", RegexOptions.Compiled);
    private static readonly Regex FullWidthPunctuationBeforeEnglish = new(@"[，。！？；】【：][A-Za-z0-9]", RegexOptions.Compiled);
    private static readonly Regex DuplicatePunctuation = new(@"([，。！？；：,.!?;:])\1+", RegexOptions.Compiled);
    private static readonly char[] SentenceEndings = ['。', '！', '？', '.', '!', '?', '；', ';', '：', ':', '”', '"', '）', ')'];

    public IEnumerable<FormatIssue> Detect(ParagraphSnapshot paragraph)
    {
        if (string.IsNullOrWhiteSpace(paragraph.Text))
        {
            yield break;
        }

        if (EnglishPunctuationSpacing.IsMatch(paragraph.Text))
        {
            yield return CreateIssue(paragraph, "英文标点后缺少空格", "英文逗号、句号、分号等后面通常需要保留一个空格", "英文标点后面直接连接了英文或数字", "在英文标点后补一个空格。");
        }

        if (ChineseWithHalfWidthPunctuation.IsMatch(paragraph.Text))
        {
            yield return CreateIssue(paragraph, "中文内容中使用了半角标点", "中文句子中的逗号、句号、问号、感叹号等应使用中文全角标点", "中文前后出现了英文半角标点", "把该处半角标点改为中文全角标点。");
        }

        if (FullWidthPunctuationBeforeEnglish.IsMatch(paragraph.Text))
        {
            yield return CreateIssue(paragraph, "中文标点后直接连接英文或数字", "中文标点后如继续出现英文或数字，应按模板保持合适空格", "中文标点后面直接跟了英文或数字", "检查该处中英文之间的空格，必要时补一个空格。");
        }

        if (DuplicatePunctuation.IsMatch(paragraph.Text))
        {
            yield return CreateIssue(paragraph, "存在重复标点", "同一位置通常只保留一个标点符号", "连续出现了重复标点", "删除多余标点，只保留一个符合语境的标点。");
        }

        if (NeedsSentenceEnding(paragraph.Text))
        {
            yield return CreateIssue(paragraph, "段落结尾缺少句末标点", "正文段落结尾应有句号、问号或感叹号等句末标点", "该段最后没有句末标点", "在段落末尾补上合适的句末标点。");
        }
    }

    private static FormatIssue CreateIssue(ParagraphSnapshot paragraph, string title, string expected, string actual, string suggestion)
    {
        return new FormatIssue
        {
            IssueCode = "",
            Severity = "提醒",
            Category = "标点",
            Location = $"第 {paragraph.Index} 段",
            TargetElementId = paragraph.TargetElementId,
            Title = title,
            Expected = expected,
            Actual = actual,
            Suggestion = suggestion
        };
    }

    private static bool NeedsSentenceEnding(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Length < 20 || trimmed.EndsWith("：", StringComparison.Ordinal) || trimmed.EndsWith(":", StringComparison.Ordinal))
        {
            return false;
        }

        if (trimmed.Any(char.IsWhiteSpace))
        {
            return !SentenceEndings.Contains(trimmed[^1]) && ContainsSentenceCharacter(trimmed);
        }

        return trimmed.Length >= 35 && !SentenceEndings.Contains(trimmed[^1]) && ContainsSentenceCharacter(trimmed);
    }

    private static bool ContainsSentenceCharacter(string text)
    {
        return text.Any(c => c >= '\u4e00' && c <= '\u9fff') || text.Any(char.IsLetter);
    }
}
