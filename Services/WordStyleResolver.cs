using DocumentFormat.OpenXml;
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
        style ??= ResolveDefaultStyle(mainPart, StyleValues.Paragraph);
        var styleChain = StyleChain(style, mainPart, StyleValues.Paragraph).ToList();
        var defaults = mainPart.StyleDefinitionsPart?.Styles?.DocDefaults?.ParagraphPropertiesDefault?.ParagraphPropertiesBaseStyle;

        var rawJustification = FirstNonEmpty(
            direct?.Justification?.Val?.InnerText,
            styleChain.Select(item => item.StyleParagraphProperties?.Justification?.Val?.InnerText),
            defaults?.Justification?.Val?.InnerText);
        rawJustification = _terminology.JustificationRaw(rawJustification);

        var rawBefore = FirstNonEmpty(
            direct?.SpacingBetweenLines?.Before?.Value,
            styleChain.Select(item => item.StyleParagraphProperties?.SpacingBetweenLines?.Before?.Value),
            defaults?.SpacingBetweenLines?.Before?.Value);

        var rawAfter = FirstNonEmpty(
            direct?.SpacingBetweenLines?.After?.Value,
            styleChain.Select(item => item.StyleParagraphProperties?.SpacingBetweenLines?.After?.Value),
            defaults?.SpacingBetweenLines?.After?.Value);

        var rawLine = FirstNonEmpty(
            direct?.SpacingBetweenLines?.Line?.Value,
            styleChain.Select(item => item.StyleParagraphProperties?.SpacingBetweenLines?.Line?.Value),
            defaults?.SpacingBetweenLines?.Line?.Value);

        var rawFirstLine = FirstNonEmpty(
            direct?.Indentation?.FirstLine?.Value,
            styleChain.Select(item => item.StyleParagraphProperties?.Indentation?.FirstLine?.Value),
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
        paragraphStyle ??= ResolveDefaultStyle(mainPart, StyleValues.Paragraph);
        var characterStyle = ResolveCharacterStyle(direct?.RunStyle?.Val?.Value, mainPart);
        characterStyle ??= ResolveDefaultStyle(mainPart, StyleValues.Character);
        var paragraphStyleChain = StyleChain(paragraphStyle, mainPart, StyleValues.Paragraph).ToList();
        var characterStyleChain = StyleChain(characterStyle, mainPart, StyleValues.Character).ToList();
        var defaults = mainPart.StyleDefinitionsPart?.Styles?.DocDefaults?.RunPropertiesDefault?.RunPropertiesBaseStyle;

        var rawFont = ResolveFont(run.InnerText ?? "", direct, characterStyleChain, paragraphStyleChain, defaults);

        var rawSize = FirstNonEmpty(
            direct?.FontSize?.Val?.Value,
            direct?.FontSizeComplexScript?.Val?.Value,
            characterStyleChain.Select(item => item.StyleRunProperties?.FontSize?.Val?.Value),
            characterStyleChain.Select(item => item.StyleRunProperties?.FontSizeComplexScript?.Val?.Value),
            paragraphStyleChain.Select(item => item.StyleRunProperties?.FontSize?.Val?.Value),
            paragraphStyleChain.Select(item => item.StyleRunProperties?.FontSizeComplexScript?.Val?.Value),
            defaults?.FontSize?.Val?.Value,
            defaults?.FontSizeComplexScript?.Val?.Value);

        var bold = ResolveBold(
            [direct?.Bold],
            characterStyleChain.Select(item => item.StyleRunProperties?.Bold),
            paragraphStyleChain.Select(item => item.StyleRunProperties?.Bold),
            [defaults?.Bold]);

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

    private static Style? ResolveDefaultStyle(MainDocumentPart mainPart, StyleValues type)
    {
        return mainPart.StyleDefinitionsPart?.Styles?.Elements<Style>()
            .FirstOrDefault(style => style.Type?.Value == type && style.Default?.Value == true);
    }

    private static IEnumerable<Style> StyleChain(Style? style, MainDocumentPart mainPart, StyleValues type)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (style is not null)
        {
            var styleId = style.StyleId?.Value ?? "";
            if (!visited.Add(styleId))
            {
                yield break;
            }

            yield return style;
            style = ResolveStyle(style.BasedOn?.Val?.Value, mainPart, type);
        }
    }

    private static string ResolveFont(
        string text,
        RunProperties? direct,
        IReadOnlyList<Style> characterStyles,
        IReadOnlyList<Style> paragraphStyles,
        RunPropertiesBaseStyle? defaults)
    {
        return ContainsCjk(text)
            ? FirstNonEmpty(
                EastAsiaFonts(direct),
                characterStyles.SelectMany(style => EastAsiaFonts(style.StyleRunProperties)),
                paragraphStyles.SelectMany(style => EastAsiaFonts(style.StyleRunProperties)),
                EastAsiaFonts(defaults),
                WesternFonts(direct),
                characterStyles.SelectMany(style => WesternFonts(style.StyleRunProperties)),
                paragraphStyles.SelectMany(style => WesternFonts(style.StyleRunProperties)),
                WesternFonts(defaults))
            : FirstNonEmpty(
                WesternFonts(direct),
                characterStyles.SelectMany(style => WesternFonts(style.StyleRunProperties)),
                paragraphStyles.SelectMany(style => WesternFonts(style.StyleRunProperties)),
                WesternFonts(defaults),
                EastAsiaFonts(direct),
                characterStyles.SelectMany(style => EastAsiaFonts(style.StyleRunProperties)),
                paragraphStyles.SelectMany(style => EastAsiaFonts(style.StyleRunProperties)),
                EastAsiaFonts(defaults));
    }

    private static IEnumerable<string?> EastAsiaFonts(OpenXmlCompositeElement? properties)
    {
        yield return properties?.GetFirstChild<RunFonts>()?.EastAsia?.Value;
    }

    private static IEnumerable<string?> WesternFonts(OpenXmlCompositeElement? properties)
    {
        var fonts = properties?.GetFirstChild<RunFonts>();
        yield return fonts?.Ascii?.Value;
        yield return fonts?.HighAnsi?.Value;
    }

    private static bool ContainsCjk(string text)
    {
        return text.Any(character => character is >= '\u4e00' and <= '\u9fff');
    }

    private static bool? ResolveBold(params IEnumerable<Bold?>[] valueGroups)
    {
        foreach (var value in valueGroups.SelectMany(group => group))
        {
            if (value is not null)
            {
                return value.Val?.Value ?? true;
            }
        }

        return null;
    }

    private static string FirstNonEmpty(params object?[] valueGroups)
    {
        foreach (var group in valueGroups)
        {
            if (group is string value && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            if (group is IEnumerable<string?> values)
            {
                var first = values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
                if (!string.IsNullOrWhiteSpace(first))
                {
                    return first;
                }
            }
        }

        return "";
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
