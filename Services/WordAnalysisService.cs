using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WordFormatAnalyzer.Models;

namespace WordFormatAnalyzer.Services;

public sealed class WordAnalysisService
{
    private readonly WordStyleResolver _styleResolver;
    private readonly WordTerminologyService _terminology;

    public WordAnalysisService(WordStyleResolver styleResolver, WordTerminologyService terminology)
    {
        _styleResolver = styleResolver;
        _terminology = terminology;
    }

    public DocumentAnalysisResult Analyze(string path, string fileName)
    {
        using var document = WordprocessingDocument.Open(path, false);
        var mainPart = document.MainDocumentPart ?? throw new InvalidOperationException("Word document has no main document part.");
        var body = mainPart.Document.Body ?? throw new InvalidOperationException("Word document has no body.");

        var result = new DocumentAnalysisResult
        {
            FileName = fileName,
            Comments = ExtractComments(mainPart),
            Paragraphs = ExtractParagraphs(body, mainPart),
            Tables = ExtractTables(body),
            PageSetup = ExtractPageSetup(body)
        };

        result.BlankAreas = DetectBlankAreas(result.Paragraphs);
        result.CoverageAreas = BuildCoverage(result);
        result.UncheckedItems = BuildUncheckedItems(result);
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

    private List<ParagraphSnapshot> ExtractParagraphs(Body body, MainDocumentPart mainPart)
    {
        var paragraphs = body.Descendants<Paragraph>().ToList();
        var result = new List<ParagraphSnapshot>();

        for (var i = 0; i < paragraphs.Count; i++)
        {
            var paragraph = paragraphs[i];
            var properties = paragraph.ParagraphProperties;
            var paragraphFormat = _styleResolver.ResolveParagraph(paragraph, mainPart);
            var runs = ExtractRuns(paragraph, mainPart);
            var dominantFontName = MostCommonWeighted(runs.Select(run => (run.FontName, run.CharacterCount)));
            var dominantFontNameRaw = MostCommonWeighted(runs.Select(run => (run.FontNameRaw, run.CharacterCount)));
            var dominantFontSize = MostCommonWeighted(runs.Select(run => (run.FontSize, run.CharacterCount)));
            var dominantFontSizeRaw = MostCommonWeighted(runs.Select(run => (run.FontSizeRaw, run.CharacterCount)));

            result.Add(new ParagraphSnapshot
            {
                TargetElementId = $"p:{i}",
                Index = i + 1,
                Text = paragraph.InnerText?.Trim() ?? "",
                StyleId = properties?.ParagraphStyleId?.Val?.Value ?? "",
                JustificationRaw = paragraphFormat.JustificationRaw,
                Justification = paragraphFormat.Justification,
                FontNameRaw = dominantFontNameRaw,
                FontName = dominantFontName,
                FontSizeRaw = dominantFontSizeRaw,
                FontSize = dominantFontSize,
                Bold = runs.Select(run => run.Bold).FirstOrDefault(value => value is not null),
                SpacingBeforeRaw = paragraphFormat.SpacingBeforeRaw,
                SpacingBefore = paragraphFormat.SpacingBefore,
                SpacingAfterRaw = paragraphFormat.SpacingAfterRaw,
                SpacingAfter = paragraphFormat.SpacingAfter,
                LineSpacingRaw = paragraphFormat.LineSpacingRaw,
                LineSpacing = paragraphFormat.LineSpacing,
                FirstLineIndentRaw = paragraphFormat.FirstLineIndentRaw,
                FirstLineIndent = paragraphFormat.FirstLineIndent,
                CharacterCount = paragraph.InnerText?.Length ?? 0,
                RunCount = runs.Count,
                HasDrawing = paragraph.Descendants<Drawing>().Any(),
                Runs = runs
            });
        }

        AssignDocumentBlocks(result);
        return result;
    }

    private static void AssignDocumentBlocks(IReadOnlyList<ParagraphSnapshot> paragraphs)
    {
        var firstBodyIndex = FindFirstBodyParagraphIndex(paragraphs);
        var counters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var paragraph in paragraphs)
        {
            var isCover = paragraph.Index <= firstBodyIndex;
            paragraph.Area = isCover ? "封面" : "正文";
            paragraph.BlockType = isCover ? ClassifyCoverBlock(paragraph) : "正文段落";
            paragraph.BlockIndex = NextBlockIndex(counters, paragraph.Area, paragraph.BlockType);
            paragraph.BlockKey = $"{paragraph.Area}:{paragraph.BlockType}:{paragraph.BlockIndex}";
        }
    }

    private static int FindFirstBodyParagraphIndex(IReadOnlyList<ParagraphSnapshot> paragraphs)
    {
        var bodyStart = paragraphs.FirstOrDefault(paragraph =>
            !paragraph.IsEmpty &&
            (paragraph.Text.Contains("摘", StringComparison.Ordinal) && paragraph.Text.Contains("要", StringComparison.Ordinal) ||
             paragraph.Text.Contains("独创性声明", StringComparison.Ordinal) ||
             paragraph.Text.Contains("目录", StringComparison.Ordinal) ||
             paragraph.Text.StartsWith("第1章", StringComparison.Ordinal) ||
             paragraph.Text.StartsWith("第一章", StringComparison.Ordinal)));

        if (bodyStart is not null)
        {
            return Math.Max(1, bodyStart.Index - 1);
        }

        var firstLongParagraph = paragraphs.FirstOrDefault(paragraph => paragraph.CharacterCount >= 80);
        return firstLongParagraph is null ? Math.Min(paragraphs.Count, 20) : Math.Max(1, firstLongParagraph.Index - 1);
    }

    private static string ClassifyCoverBlock(ParagraphSnapshot paragraph)
    {
        if (paragraph.HasDrawing)
        {
            return "封面图片";
        }

        if (paragraph.IsEmpty)
        {
            return "封面空行";
        }

        if (paragraph.Text.Contains("工程学院", StringComparison.Ordinal) ||
            paragraph.Text.Contains("大学", StringComparison.Ordinal) ||
            paragraph.Text.Contains("学院", StringComparison.Ordinal))
        {
            return "封面主标题";
        }

        if (paragraph.Text.Contains("毕业设计", StringComparison.Ordinal) ||
            paragraph.Text.Contains("毕业论文", StringComparison.Ordinal) ||
            paragraph.Text.Contains("学位论文", StringComparison.Ordinal))
        {
            return "封面副标题";
        }

        if (paragraph.Text.Contains("Design", StringComparison.OrdinalIgnoreCase) ||
            paragraph.Text.Contains("Implementation", StringComparison.OrdinalIgnoreCase))
        {
            return "封面英文题名";
        }

        if (paragraph.Text.Contains("题目", StringComparison.Ordinal) ||
            paragraph.Text.Contains("姓名", StringComparison.Ordinal) ||
            paragraph.Text.Contains("学号", StringComparison.Ordinal) ||
            paragraph.Text.Contains("专业", StringComparison.Ordinal) ||
            paragraph.Text.Contains("班级", StringComparison.Ordinal) ||
            paragraph.Text.Contains("指导教师", StringComparison.Ordinal) ||
            paragraph.Text.Contains("导师", StringComparison.Ordinal))
        {
            return "封面作者信息";
        }

        return paragraph.FontSize switch
        {
            "小初" or "初号" => "封面主标题",
            "小二" or "二号" or "小一" => "封面题名",
            _ => "封面其他文字"
        };
    }

    private static int NextBlockIndex(IDictionary<string, int> counters, string area, string blockType)
    {
        var key = $"{area}:{blockType}";
        counters.TryGetValue(key, out var value);
        value++;
        counters[key] = value;
        return value;
    }

    private List<RunFormatSnapshot> ExtractRuns(Paragraph paragraph, MainDocumentPart mainPart)
    {
        var runs = paragraph.Descendants<Run>().ToList();
        var result = new List<RunFormatSnapshot>();

        for (var i = 0; i < runs.Count; i++)
        {
            var run = runs[i];
            var text = run.InnerText ?? "";
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var format = _styleResolver.ResolveRun(run, paragraph, mainPart);
            result.Add(new RunFormatSnapshot
            {
                Index = i + 1,
                Text = text,
                FontNameRaw = format.FontNameRaw,
                FontName = format.FontName,
                FontSizeRaw = format.FontSizeRaw,
                FontSize = format.FontSize,
                Bold = format.Bold,
                CharacterCount = text.Length
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

    private PageSetupSnapshot ExtractPageSetup(Body body)
    {
        var section = body.Descendants<SectionProperties>().LastOrDefault();
        var margin = section?.GetFirstChild<PageMargin>();
        var size = section?.GetFirstChild<PageSize>();

        return new PageSetupSnapshot
        {
            TopMarginCmValue = TwipsToCm(margin?.Top?.Value),
            BottomMarginCmValue = TwipsToCm(margin?.Bottom?.Value),
            LeftMarginCmValue = TwipsToCm(margin?.Left?.Value),
            RightMarginCmValue = TwipsToCm(margin?.Right?.Value),
            HeaderMarginCmValue = TwipsToCm(margin?.Header?.Value),
            FooterMarginCmValue = TwipsToCm(margin?.Footer?.Value),
            PageWidthCmValue = TwipsToCm(size?.Width?.Value),
            PageHeightCmValue = TwipsToCm(size?.Height?.Value),
            TopMargin = ToCmText(TwipsToCm(margin?.Top?.Value)),
            BottomMargin = ToCmText(TwipsToCm(margin?.Bottom?.Value)),
            LeftMargin = ToCmText(TwipsToCm(margin?.Left?.Value)),
            RightMargin = ToCmText(TwipsToCm(margin?.Right?.Value)),
            HeaderMargin = ToCmText(TwipsToCm(margin?.Header?.Value)),
            FooterMargin = ToCmText(TwipsToCm(margin?.Footer?.Value)),
            PageWidth = ToCmText(TwipsToCm(size?.Width?.Value)),
            PageHeight = ToCmText(TwipsToCm(size?.Height?.Value)),
            OrientationRaw = size?.Orient?.Value.ToString() ?? "",
            Orientation = _terminology.PageOrientation(size?.Orient?.Value.ToString() ?? "")
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
            CommonFontNameRaw = MostCommon(nonEmpty.Select(p => p.FontNameRaw)),
            CommonFontSize = MostCommon(nonEmpty.Select(p => p.FontSize)),
            CommonFontSizeRaw = MostCommon(nonEmpty.Select(p => p.FontSizeRaw)),
            CommonJustification = MostCommon(nonEmpty.Select(p => p.Justification)),
            CommonJustificationRaw = MostCommon(nonEmpty.Select(p => p.JustificationRaw)),
            CommonSpacingBefore = MostCommon(nonEmpty.Select(p => p.SpacingBefore)),
            CommonSpacingBeforeRaw = MostCommon(nonEmpty.Select(p => p.SpacingBeforeRaw)),
            CommonSpacingAfter = MostCommon(nonEmpty.Select(p => p.SpacingAfter)),
            CommonSpacingAfterRaw = MostCommon(nonEmpty.Select(p => p.SpacingAfterRaw)),
            CommonLineSpacing = MostCommon(nonEmpty.Select(p => p.LineSpacing)),
            CommonLineSpacingRaw = MostCommon(nonEmpty.Select(p => p.LineSpacingRaw)),
            CommonFirstLineIndent = MostCommon(nonEmpty.Select(p => p.FirstLineIndent)),
            CommonFirstLineIndentRaw = MostCommon(nonEmpty.Select(p => p.FirstLineIndentRaw)),
            CommonTableColumnCount = result.Tables.Select(t => t.MaxColumnCount)
                .Where(value => value > 0)
                .GroupBy(value => value)
                .OrderByDescending(group => group.Count())
                .Select(group => group.Key)
                .FirstOrDefault(),
            PageSetup = result.PageSetup
        };
    }

    private static List<CoverageAreaSummary> BuildCoverage(DocumentAnalysisResult result)
    {
        var bodyParagraphs = result.Paragraphs;
        var objectCount = result.Tables.Count;
        return
        [
            new CoverageAreaSummary
            {
                Area = "全文",
                CharacterCount = bodyParagraphs.Sum(paragraph => paragraph.CharacterCount),
                ParagraphCount = bodyParagraphs.Count,
                ObjectCount = objectCount,
                CheckedCharacterCount = bodyParagraphs.Sum(paragraph => paragraph.CharacterCount),
                CheckedParagraphCount = bodyParagraphs.Count,
                CheckedObjectCount = objectCount,
                Notes = objectCount == 0 ? [] : ["表格结构已检查，表格内文字按段落文本参与字体字号检查。"]
            }
        ];
    }

    private static List<UncheckedItem> BuildUncheckedItems(DocumentAnalysisResult result)
    {
        var uncheckedItems = new List<UncheckedItem>
        {
            new()
            {
                Area = "页码和目录",
                Location = "目录区域附近",
                TargetElementId = "p:0",
                Reason = "真实页码和目录页码需要 Word 排版结果；当前阶段只做结构检查，不强行判断页码是否一致。"
            }
        };

        return uncheckedItems;
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

    private static string MostCommonWeighted(IEnumerable<(string Value, int Weight)> values)
    {
        return values
            .Where(item => !string.IsNullOrWhiteSpace(item.Value))
            .GroupBy(item => item.Value)
            .OrderByDescending(group => group.Sum(item => Math.Max(1, item.Weight)))
            .Select(group => group.Key)
            .FirstOrDefault() ?? "";
    }

    private static string Limit(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }

    private static double? TwipsToCm(long? twips)
    {
        if (twips is null)
        {
            return null;
        }

        return Math.Round(twips.Value / 567.0, 2);
    }

    private static string ToCmText(double? value)
    {
        return value is null ? "" : $"{value:0.##} 厘米";
    }
}
