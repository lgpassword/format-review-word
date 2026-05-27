using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WordFormatAnalyzer.Models;

namespace WordFormatAnalyzer.Services;

public sealed class NormalDocumentService
{
    private readonly WordAnalysisService _analysis;

    public NormalDocumentService(WordAnalysisService analysis)
    {
        _analysis = analysis;
    }

    public string CreateNormalizedCopy(string targetPath, string outputPath, DocumentAnalysisResult template, IReadOnlyList<FormatIssue> issues)
    {
        File.Copy(targetPath, outputPath, overwrite: true);
        var targetAnalysis = _analysis.Analyze(outputPath, Path.GetFileName(outputPath));

        using var document = WordprocessingDocument.Open(outputPath, true);
        var mainPart = document.MainDocumentPart ?? throw new InvalidOperationException("Word document has no main document part.");
        var body = mainPart.Document.Body ?? throw new InvalidOperationException("Word document has no body.");
        var paragraphs = body.Descendants<Paragraph>().ToList();

        ApplyPageSetup(body, template.Baseline.PageSetup);
        ApplyTemplateRules(paragraphs, targetAnalysis.Paragraphs, template.FormatRules, template.Baseline);
        NormalizePunctuation(paragraphs);
        mainPart.Document.Save();
        return outputPath;
    }

    private static void ApplyPageSetup(Body body, PageSetupSnapshot baseline)
    {
        var section = body.Descendants<SectionProperties>().LastOrDefault() ?? body.AppendChild(new SectionProperties());
        var margin = section.GetFirstChild<PageMargin>() ?? section.AppendChild(new PageMargin());
        var size = section.GetFirstChild<PageSize>() ?? section.AppendChild(new PageSize());

        if (baseline.TopMarginCmValue is not null) margin.Top = CmToTwips(baseline.TopMarginCmValue.Value);
        if (baseline.BottomMarginCmValue is not null) margin.Bottom = CmToTwips(baseline.BottomMarginCmValue.Value);
        if (baseline.LeftMarginCmValue is not null) margin.Left = CmToTwipsUInt(baseline.LeftMarginCmValue.Value);
        if (baseline.RightMarginCmValue is not null) margin.Right = CmToTwipsUInt(baseline.RightMarginCmValue.Value);
        if (baseline.HeaderMarginCmValue is not null) margin.Header = CmToTwipsUInt(baseline.HeaderMarginCmValue.Value);
        if (baseline.FooterMarginCmValue is not null) margin.Footer = CmToTwipsUInt(baseline.FooterMarginCmValue.Value);
        if (baseline.PageWidthCmValue is not null) size.Width = CmToTwipsUInt(baseline.PageWidthCmValue.Value);
        if (baseline.PageHeightCmValue is not null) size.Height = CmToTwipsUInt(baseline.PageHeightCmValue.Value);

        var orientation = ParsePageOrientation(string.IsNullOrWhiteSpace(baseline.OrientationRaw) ? baseline.Orientation : baseline.OrientationRaw);
        if (orientation is not null)
        {
            size.Orient = orientation.Value;
        }
    }

    private static void ApplyTemplateRules(
        IReadOnlyList<Paragraph> paragraphs,
        IReadOnlyList<ParagraphSnapshot> snapshots,
        IReadOnlyList<TemplateFormatRule> rules,
        TemplateBaseline baseline)
    {
        for (var i = 0; i < paragraphs.Count && i < snapshots.Count; i++)
        {
            var paragraph = paragraphs[i];
            var snapshot = snapshots[i];
            var rule = FindRule(rules, snapshot);
            var pPr = paragraph.ParagraphProperties ?? paragraph.PrependChild(new ParagraphProperties());
            var justificationRaw = FirstNonEmpty(rule?.JustificationRaw, baseline.CommonJustificationRaw, rule?.Justification, baseline.CommonJustification);
            var justification = ParseJustification(justificationRaw);
            if (justification is not null)
            {
                pPr.Justification = new Justification { Val = justification.Value };
            }

            pPr.SpacingBetweenLines ??= new SpacingBetweenLines();
            var spacingBefore = FirstNonEmpty(rule?.SpacingBeforeRaw, baseline.CommonSpacingBeforeRaw);
            if (!string.IsNullOrWhiteSpace(spacingBefore))
            {
                pPr.SpacingBetweenLines.Before = spacingBefore;
            }

            var spacingAfter = FirstNonEmpty(rule?.SpacingAfterRaw, baseline.CommonSpacingAfterRaw);
            if (!string.IsNullOrWhiteSpace(spacingAfter))
            {
                pPr.SpacingBetweenLines.After = spacingAfter;
            }

            var lineSpacing = FirstNonEmpty(rule?.LineSpacingRaw, baseline.CommonLineSpacingRaw);
            if (!string.IsNullOrWhiteSpace(lineSpacing))
            {
                pPr.SpacingBetweenLines.Line = lineSpacing;
            }

            var firstLineIndent = FirstNonEmpty(rule?.FirstLineIndentRaw, baseline.CommonFirstLineIndentRaw);
            if (!string.IsNullOrWhiteSpace(firstLineIndent))
            {
                pPr.Indentation ??= new Indentation();
                pPr.Indentation.FirstLine = firstLineIndent;
            }

            ApplyRunDefaults(paragraph, rule, baseline);
        }
    }

    private static void ApplyRunDefaults(Paragraph paragraph, TemplateFormatRule? rule, TemplateBaseline baseline)
    {
        foreach (var run in paragraph.Descendants<Run>())
        {
            var runProperties = run.RunProperties ?? run.PrependChild(new RunProperties());
            var chineseFont = FirstNonEmpty(rule?.ChineseFontNameRaw, baseline.CommonChineseFontNameRaw);
            if (!string.IsNullOrWhiteSpace(chineseFont))
            {
                runProperties.RunFonts ??= new RunFonts();
                runProperties.RunFonts.EastAsia = chineseFont;
            }

            var westernFont = FirstNonEmpty(rule?.WesternFontNameRaw, baseline.CommonWesternFontNameRaw);
            if (!string.IsNullOrWhiteSpace(westernFont))
            {
                runProperties.RunFonts ??= new RunFonts();
                runProperties.RunFonts.Ascii = westernFont;
                runProperties.RunFonts.HighAnsi = westernFont;
            }

            var fontSize = FirstNonEmpty(rule?.FontSizeRaw, baseline.CommonFontSizeRaw);
            if (!string.IsNullOrWhiteSpace(fontSize))
            {
                runProperties.FontSize = new FontSize { Val = fontSize };
                runProperties.FontSizeComplexScript = new FontSizeComplexScript { Val = fontSize };
            }
        }
    }

    private static void NormalizePunctuation(IEnumerable<Paragraph> paragraphs)
    {
        foreach (var paragraph in paragraphs)
        {
            foreach (var run in paragraph.Elements<Run>())
            {
                foreach (var text in run.Elements<Text>())
                {
                    text.Text = text.Text
                        .Replace(" ,", "，", StringComparison.Ordinal)
                        .Replace(" .", "。", StringComparison.Ordinal)
                        .Replace(" ;", "；", StringComparison.Ordinal)
                        .Replace(" :", "：", StringComparison.Ordinal)
                        .Replace(" !", "！", StringComparison.Ordinal)
                        .Replace(" ?", "？", StringComparison.Ordinal)
                        .Replace("，,", "，", StringComparison.Ordinal)
                        .Replace("。。", "。", StringComparison.Ordinal)
                        .Replace("，，", "，", StringComparison.Ordinal)
                        .Replace("！！", "！", StringComparison.Ordinal)
                        .Replace("？？", "？", StringComparison.Ordinal);
                }
            }
        }
    }

    private static int CmToTwips(double value)
    {
        return Convert.ToInt32(Math.Round(value * 567.0));
    }

    private static uint CmToTwipsUInt(double value)
    {
        return Convert.ToUInt32(Math.Max(0, CmToTwips(value)));
    }

    private static PageOrientationValues? ParsePageOrientation(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "portrait" => PageOrientationValues.Portrait,
            "landscape" => PageOrientationValues.Landscape,
            _ => null
        };
    }

    private static JustificationValues? ParseJustification(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "left" or "start" => JustificationValues.Left,
            "center" => JustificationValues.Center,
            "right" or "end" => JustificationValues.Right,
            "both" => JustificationValues.Both,
            "distribute" => JustificationValues.Distribute,
            _ => null
        };
    }

    private static TemplateFormatRule? FindRule(IEnumerable<TemplateFormatRule> rules, ParagraphSnapshot paragraph)
    {
        return rules.FirstOrDefault(rule =>
            string.Equals(rule.Area, paragraph.Area, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(rule.BlockType, paragraph.BlockType, StringComparison.OrdinalIgnoreCase));
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
    }
}
