using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WordFormatAnalyzer.Services;

public sealed class WordStyleResolver
{
    private readonly WordTerminologyService _terminology;

    public WordStyleResolver(WordTerminologyService terminology)
    {
        _terminology = terminology;
    }

    public ResolvedParagraphFormat ResolveParagraph(Paragraph paragraph, MainDocumentPart mainPart)
    {
        var direct = paragraph.ParagraphProperties;
        var style = ResolveParagraphStyle(direct?.ParagraphStyleId?.Val?.Value, mainPart);
        var defaults = mainPart.StyleDefinitionsPart?.Styles?.DocDefaults?.ParagraphPropertiesDefault?.ParagraphPropertiesBaseStyle;

        var rawJustification = FirstNonEmpty(
            direct?.Justification?.Val?.Value.ToString(),
            style?.StyleParagraphProperties?.Justification?.Val?.Value.ToString(),
            defaults?.Justification?.Val?.Value.ToString());

        var rawBefore = FirstNonEmpty(
            direct?.SpacingBetweenLines?.Before?.Value,
            style?.StyleParagraphProperties?.SpacingBetweenLines?.Before?.Value,
            defaults?.SpacingBetweenLines?.Before?.Value);

        var rawAfter = FirstNonEmpty(
            direct?.SpacingBetweenLines?.After?.Value,
            style?.StyleParagraphProperties?.SpacingBetweenLines?.After?.Value,
            defaults?.SpacingBetweenLines?.After?.Value);

        var rawLine = FirstNonEmpty(
            direct?.SpacingBetweenLines?.Line?.Value,
            style?.StyleParagraphProperties?.SpacingBetweenLines?.Line?.Value,
            defaults?.SpacingBetweenLines?.Line?.Value);

        var rawFirstLine = FirstNonEmpty(
            direct?.Indentation?.FirstLine?.Value,
            style?.StyleParagraphProperties?.Indentation?.FirstLine?.Value,
            defaults?.Indentation?.FirstLine?.Value);

        return new ResolvedParagraphFormat(
            string.IsNullOrWhiteSpace(rawJustification) ? "left" : rawJustification,
            string.IsNullOrWhiteSpace(rawJustification) ? "左对齐" : _terminology.Justification(rawJustification),
            rawBefore,
            _terminology.Spacing(rawBefore),
            rawAfter,
            _terminology.Spacing(rawAfter),
            rawLine,
            _terminology.LineSpacing(rawLine),
            rawFirstLine,
            _terminology.FirstLineIndent(rawFirstLine));
    }

    public ResolvedRunFormat ResolveRun(Run run, Paragraph paragraph, MainDocumentPart mainPart)
    {
        var direct = run.RunProperties;
        var paragraphStyle = ResolveParagraphStyle(paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value, mainPart);
        var characterStyle = ResolveCharacterStyle(direct?.RunStyle?.Val?.Value, mainPart);
        var defaults = mainPart.StyleDefinitionsPart?.Styles?.DocDefaults?.RunPropertiesDefault?.RunPropertiesBaseStyle;

        var rawFont = FirstNonEmpty(
            direct?.RunFonts?.EastAsia?.Value,
            direct?.RunFonts?.Ascii?.Value,
            direct?.RunFonts?.HighAnsi?.Value,
            characterStyle?.StyleRunProperties?.RunFonts?.EastAsia?.Value,
            characterStyle?.StyleRunProperties?.RunFonts?.Ascii?.Value,
            characterStyle?.StyleRunProperties?.RunFonts?.HighAnsi?.Value,
            paragraphStyle?.StyleRunProperties?.RunFonts?.EastAsia?.Value,
            paragraphStyle?.StyleRunProperties?.RunFonts?.Ascii?.Value,
            paragraphStyle?.StyleRunProperties?.RunFonts?.HighAnsi?.Value,
            defaults?.RunFonts?.EastAsia?.Value,
            defaults?.RunFonts?.Ascii?.Value,
            defaults?.RunFonts?.HighAnsi?.Value);

        var rawSize = FirstNonEmpty(
            direct?.FontSize?.Val?.Value,
            direct?.FontSizeComplexScript?.Val?.Value,
            characterStyle?.StyleRunProperties?.FontSize?.Val?.Value,
            characterStyle?.StyleRunProperties?.FontSizeComplexScript?.Val?.Value,
            paragraphStyle?.StyleRunProperties?.FontSize?.Val?.Value,
            paragraphStyle?.StyleRunProperties?.FontSizeComplexScript?.Val?.Value,
            defaults?.FontSize?.Val?.Value,
            defaults?.FontSizeComplexScript?.Val?.Value);

        var bold = ResolveBold(direct?.Bold, characterStyle?.StyleRunProperties?.Bold, paragraphStyle?.StyleRunProperties?.Bold, defaults?.Bold);

        return new ResolvedRunFormat(rawFont, _terminology.FontName(rawFont), rawSize, _terminology.FontSize(rawSize), bold);
    }

    private static Style? ResolveParagraphStyle(string? styleId, MainDocumentPart mainPart)
    {
        return ResolveStyle(styleId, mainPart, StyleValues.Paragraph);
    }

    private static Style? ResolveCharacterStyle(string? styleId, MainDocumentPart mainPart)
    {
        return ResolveStyle(styleId, mainPart, StyleValues.Character);
    }

    private static Style? ResolveStyle(string? styleId, MainDocumentPart mainPart, StyleValues type)
    {
        if (string.IsNullOrWhiteSpace(styleId))
        {
            return null;
        }

        return mainPart.StyleDefinitionsPart?.Styles?.Elements<Style>()
            .FirstOrDefault(style => style.Type?.Value == type && style.StyleId?.Value == styleId);
    }

    private static bool? ResolveBold(params Bold?[] values)
    {
        foreach (var value in values)
        {
            if (value is not null)
            {
                return value.Val?.Value ?? true;
            }
        }

        return null;
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
    }
}

public sealed record ResolvedParagraphFormat(
    string JustificationRaw,
    string Justification,
    string SpacingBeforeRaw,
    string SpacingBefore,
    string SpacingAfterRaw,
    string SpacingAfter,
    string LineSpacingRaw,
    string LineSpacing,
    string FirstLineIndentRaw,
    string FirstLineIndent);

public sealed record ResolvedRunFormat(
    string FontNameRaw,
    string FontName,
    string FontSizeRaw,
    string FontSize,
    bool? Bold);
