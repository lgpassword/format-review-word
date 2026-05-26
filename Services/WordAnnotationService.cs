using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WordFormatAnalyzer.Models;

namespace WordFormatAnalyzer.Services;

public sealed class WordAnnotationService
{
    private readonly IssueTextService _issueText;

    public WordAnnotationService(IssueTextService issueText)
    {
        _issueText = issueText;
    }

    public string CreateAnnotatedCopy(string targetPath, string outputPath, IReadOnlyList<FormatIssue> issues)
    {
        File.Copy(targetPath, outputPath, overwrite: true);

        using var document = WordprocessingDocument.Open(outputPath, true);
        var mainPart = document.MainDocumentPart ?? throw new InvalidOperationException("Word document has no main document part.");
        var commentsPart = mainPart.WordprocessingCommentsPart ?? mainPart.AddNewPart<WordprocessingCommentsPart>();

        commentsPart.Comments ??= new Comments();
        var nextId = commentsPart.Comments.Elements<Comment>()
            .Select(comment => int.TryParse(comment.Id?.Value, out var id) ? id : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var body = mainPart.Document.Body ?? throw new InvalidOperationException("Word document has no body.");
        var paragraphs = body.Descendants<Paragraph>().ToList();

        foreach (var issue in issues.Take(200))
        {
            var paragraphIndex = ResolveParagraphIndex(issue.TargetElementId, paragraphs.Count);
            var paragraph = paragraphs[paragraphIndex];
            AddComment(paragraph, commentsPart.Comments, nextId.ToString(), BuildCommentText(issue));
            nextId++;
        }

        commentsPart.Comments.Save();
        mainPart.Document.Save();
        return outputPath;
    }

    private static int ResolveParagraphIndex(string targetElementId, int paragraphCount)
    {
        if (paragraphCount <= 0)
        {
            return 0;
        }

        if (targetElementId.StartsWith("p:", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(targetElementId[2..], out var index))
        {
            return Math.Clamp(index, 0, paragraphCount - 1);
        }

        return 0;
    }

    private static void AddComment(Paragraph paragraph, Comments comments, string id, string text)
    {
        var comment = new Comment
        {
            Id = id,
            Author = "Word格式检查系统",
            Date = DateTime.UtcNow
        };

        foreach (var line in text.Split(Environment.NewLine, StringSplitOptions.None))
        {
            comment.AppendChild(new Paragraph(new Run(new Text(line))));
        }

        comments.AppendChild(comment);

        paragraph.InsertAt(new CommentRangeStart { Id = id }, 0);
        paragraph.AppendChild(new CommentRangeEnd { Id = id });
        paragraph.AppendChild(new Run(new CommentReference { Id = id }));
    }

    private string BuildCommentText(FormatIssue issue)
    {
        return _issueText.BuildComment(issue);
    }
}
