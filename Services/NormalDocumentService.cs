using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WordFormatAnalyzer.Models;

namespace WordFormatAnalyzer.Services;

public sealed class NormalDocumentService
{
    public string CreateNormalizedCopy(string targetPath, string outputPath, DocumentAnalysisResult template, IReadOnlyList<FormatIssue> issues)
    {
        File.Copy(targetPath, outputPath, overwrite: true);

        using var document = WordprocessingDocument.Open(outputPath, true);
        var mainPart = document.MainDocumentPart ?? throw new InvalidOperationException("Word document has no main document part.");
        var body = mainPart.Document.Body ?? throw new InvalidOperationException("Word document has no body.");
        var paragraphs = body.Descendants<Paragraph>().ToList();

        ApplyPageSetup(body, template.Baseline.PageSetup);
        ApplyTemplateDefaults(paragraphs, template.Baseline);
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

    private static void ApplyTemplateDefaults(IEnumerable<Paragraph> paragraphs, TemplateBaseline baseline)
    {
        foreach (var paragraph in paragraphs)
        {
            var pPr = paragraph.ParagraphProperties ?? paragraph.PrependChild(new ParagraphProperties());
            var justification = ParseJustification(string.IsNullOrWhiteSpace(baseline.CommonJustificationRaw) ? baseline.CommonJustification : baseline.CommonJustificationRaw);
            if (justification is not null)
            {
                pPr.Justification = new Justification { Val = justification.Value };
            }

            pPr.SpacingBetweenLines ??= new SpacingBetweenLines();
            if (!string.IsNullOrWhiteSpace(baseline.CommonSpacingBeforeRaw))
            {
                pPr.SpacingBetweenLines.Before = baseline.CommonSpacingBeforeRaw;
            }

            if (!string.IsNullOrWhiteSpace(baseline.CommonSpacingAfterRaw))
            {
                pPr.SpacingBetweenLines.After = baseline.CommonSpacingAfterRaw;
            }

            if (!string.IsNullOrWhiteSpace(baseline.CommonLineSpacingRaw))
            {
                pPr.SpacingBetweenLines.Line = baseline.CommonLineSpacingRaw;
            }

            if (!string.IsNullOrWhiteSpace(baseline.CommonFirstLineIndentRaw))
            {
                pPr.Indentation ??= new Indentation();
                pPr.Indentation.FirstLine = baseline.CommonFirstLineIndentRaw;
            }

            ApplyRunDefaults(paragraph, baseline);
        }
    }

    private static void ApplyRunDefaults(Paragraph paragraph, TemplateBaseline baseline)
    {
        foreach (var run in paragraph.Descendants<Run>())
        {
            var runProperties = run.RunProperties ?? run.PrependChild(new RunProperties());
            if (!string.IsNullOrWhiteSpace(baseline.CommonFontNameRaw))
            {
                runProperties.RunFonts ??= new RunFonts();
                runProperties.RunFonts.EastAsia = baseline.CommonFontNameRaw;
                runProperties.RunFonts.Ascii = baseline.CommonFontNameRaw;
                runProperties.RunFonts.HighAnsi = baseline.CommonFontNameRaw;
            }

            if (!string.IsNullOrWhiteSpace(baseline.CommonChineseFontNameRaw))
            {
                runProperties.RunFonts ??= new RunFonts();
                runProperties.RunFonts.EastAsia = baseline.CommonChineseFontNameRaw;
            }

            if (!string.IsNullOrWhiteSpace(baseline.CommonWesternFontNameRaw))
            {
                runProperties.RunFonts ??= new RunFonts();
                runProperties.RunFonts.Ascii = baseline.CommonWesternFontNameRaw;
                runProperties.RunFonts.HighAnsi = baseline.CommonWesternFontNameRaw;
            }

            if (!string.IsNullOrWhiteSpace(baseline.CommonFontSizeRaw))
            {
                runProperties.FontSize = new FontSize { Val = baseline.CommonFontSizeRaw };
                runProperties.FontSizeComplexScript = new FontSizeComplexScript { Val = baseline.CommonFontSizeRaw };
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
}
