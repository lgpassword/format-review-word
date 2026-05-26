using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WordFormatAnalyzer.Models;

namespace WordFormatAnalyzer.Services;

public sealed class WordAnalysisService
{
    public DocumentAnalysisResult Analyze(string path, string fileName)
    {
        using var document = WordprocessingDocument.Open(path, false);
        var mainPart = document.MainDocumentPart ?? throw new InvalidOperationException("Word document has no main document part.");
        var body = mainPart.Document.Body ?? throw new InvalidOperationException("Word document has no body.");

        var result = new DocumentAnalysisResult
        {
            FileName = fileName,
            Comments = ExtractComments(mainPart),
            Paragraphs = ExtractParagraphs(body),
            Tables = ExtractTables(body),
            PageSetup = ExtractPageSetup(body)
        };

        result.BlankAreas = DetectBlankAreas(result.Paragraphs);
        result.Baseline = BuildBaseline(result);
        return result;
    }

    private static List<CommentInfo> ExtractComments(MainDocumentPart mainPart)
    {
        var commentsPart = mainPart.WordprocessingCommentsPart;
        if (commentsPart?.Comments is null)
        {
            return [];
        }

        return commentsPart.Comments.Elements<Comment>()
            .Select(comment => new CommentInfo(
                comment.Author?.Value ?? "",
                comment.Date?.Value,
                string.Join("", comment.Descendants<Text>().Select(t => t.Text)).Trim(),
                ""))
            .Where(comment => !string.IsNullOrWhiteSpace(comment.Text))
            .ToList();
    }

    private static List<ParagraphSnapshot> ExtractParagraphs(Body body)
    {
        var paragraphs = body.Descendants<Paragraph>().ToList();
        var result = new List<ParagraphSnapshot>();

        for (var i = 0; i < paragraphs.Count; i++)
        {
            var paragraph = paragraphs[i];
            var properties = paragraph.ParagraphProperties;
            var firstRunProperties = paragraph.Descendants<RunProperties>().FirstOrDefault();

            result.Add(new ParagraphSnapshot
            {
                TargetElementId = $"p:{i}",
                Index = i + 1,
                Text = paragraph.InnerText?.Trim() ?? "",
                StyleId = properties?.ParagraphStyleId?.Val?.Value ?? "",
                Justification = properties?.Justification?.Val?.Value.ToString() ?? "",
                FontName = firstRunProperties?.RunFonts?.Ascii?.Value
                    ?? firstRunProperties?.RunFonts?.HighAnsi?.Value
                    ?? firstRunProperties?.RunFonts?.EastAsia?.Value
                    ?? "",
                FontSize = firstRunProperties?.FontSize?.Val?.Value ?? "",
                Bold = firstRunProperties?.Bold is null ? null : firstRunProperties.Bold.Val?.Value ?? true,
                SpacingBefore = properties?.SpacingBetweenLines?.Before?.Value ?? "",
                SpacingAfter = properties?.SpacingBetweenLines?.After?.Value ?? "",
                LineSpacing = properties?.SpacingBetweenLines?.Line?.Value ?? "",
                FirstLineIndent = properties?.Indentation?.FirstLine?.Value ?? ""
            });
        }

        return result;
    }

    private static List<TableSnapshot> ExtractTables(Body body)
    {
        var tables = body.Descendants<Table>().ToList();
        var result = new List<TableSnapshot>();

        for (var i = 0; i < tables.Count; i++)
        {
            var table = tables[i];
            var rows = table.Elements<TableRow>().ToList();
            result.Add(new TableSnapshot
            {
                TargetElementId = $"t:{i}",
                Index = i + 1,
                RowCount = rows.Count,
                MaxColumnCount = rows.Select(row => row.Elements<TableCell>().Count()).DefaultIfEmpty(0).Max(),
                PreviewText = Limit(table.InnerText?.Trim() ?? "", 80)
            });
        }

        return result;
    }

    private static PageSetupSnapshot ExtractPageSetup(Body body)
    {
        var section = body.Descendants<SectionProperties>().LastOrDefault();
        var margin = section?.GetFirstChild<PageMargin>();
        var size = section?.GetFirstChild<PageSize>();

        return new PageSetupSnapshot
        {
            TopMargin = margin?.Top?.Value.ToString() ?? "",
            BottomMargin = margin?.Bottom?.Value.ToString() ?? "",
            LeftMargin = margin?.Left?.Value.ToString() ?? "",
            RightMargin = margin?.Right?.Value.ToString() ?? "",
            HeaderMargin = margin?.Header?.Value.ToString() ?? "",
            FooterMargin = margin?.Footer?.Value.ToString() ?? "",
            PageWidth = size?.Width?.Value.ToString() ?? "",
            PageHeight = size?.Height?.Value.ToString() ?? "",
            Orientation = size?.Orient?.Value.ToString() ?? ""
        };
    }

    private static List<BlankAreaFinding> DetectBlankAreas(IReadOnlyList<ParagraphSnapshot> paragraphs)
    {
        var findings = new List<BlankAreaFinding>();
        var start = -1;
        var count = 0;

        for (var i = 0; i <= paragraphs.Count; i++)
        {
            var isEmpty = i < paragraphs.Count && paragraphs[i].IsEmpty;
            if (isEmpty)
            {
                if (start < 0)
                {
                    start = i;
                }

                count++;
                continue;
            }

            if (count >= 3 && start >= 0)
            {
                var paragraph = paragraphs[start];
                findings.Add(new BlankAreaFinding(
                    paragraph.TargetElementId,
                    $"第 {paragraph.Index} 段附近",
                    count,
                    $"连续 {count} 个空段落，可能形成大空白。"));
            }

            start = -1;
            count = 0;
        }

        return findings;
    }

    private static TemplateBaseline BuildBaseline(DocumentAnalysisResult result)
    {
        var nonEmpty = result.Paragraphs.Where(p => !p.IsEmpty).ToList();

        return new TemplateBaseline
        {
            CommonFontName = MostCommon(nonEmpty.Select(p => p.FontName)),
            CommonFontSize = MostCommon(nonEmpty.Select(p => p.FontSize)),
            CommonJustification = MostCommon(nonEmpty.Select(p => p.Justification)),
            CommonSpacingBefore = MostCommon(nonEmpty.Select(p => p.SpacingBefore)),
            CommonSpacingAfter = MostCommon(nonEmpty.Select(p => p.SpacingAfter)),
            CommonLineSpacing = MostCommon(nonEmpty.Select(p => p.LineSpacing)),
            CommonFirstLineIndent = MostCommon(nonEmpty.Select(p => p.FirstLineIndent)),
            CommonTableColumnCount = result.Tables.Select(t => t.MaxColumnCount)
                .Where(value => value > 0)
                .GroupBy(value => value)
                .OrderByDescending(group => group.Count())
                .Select(group => group.Key)
                .FirstOrDefault(),
            PageSetup = result.PageSetup
        };
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

    private static string Limit(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
}
