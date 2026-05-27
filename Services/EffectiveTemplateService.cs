using WordFormatAnalyzer.Models;

namespace WordFormatAnalyzer.Services;

public sealed class EffectiveTemplateService
{
    private readonly TemplateRuleConflictService _ruleConflicts;

    public EffectiveTemplateService(TemplateRuleConflictService ruleConflicts)
    {
        _ruleConflicts = ruleConflicts;
    }

    public DocumentAnalysisResult Build(AnalysisSession session)
    {
        var effective = Clone(session.ReferenceTemplates.LastOrDefault()?.Analysis ?? session.TemplateAnalysis);
        effective.Baseline = _ruleConflicts.ApplySelections(effective.Baseline, session.RuleConflicts);
        effective.FileName = session.ReferenceTemplates.LastOrDefault()?.FileName ?? session.TemplateFileName;
        return effective;
    }

    private static DocumentAnalysisResult Clone(DocumentAnalysisResult source)
    {
        return new DocumentAnalysisResult
        {
            FileName = source.FileName,
            Comments = source.Comments.ToList(),
            Paragraphs = source.Paragraphs.ToList(),
            Tables = source.Tables.ToList(),
            BlankAreas = source.BlankAreas.ToList(),
            CoverageAreas = source.CoverageAreas.ToList(),
            UncheckedItems = source.UncheckedItems.ToList(),
            PageSetup = source.PageSetup,
            Baseline = new TemplateBaseline
            {
                CommonFontName = source.Baseline.CommonFontName,
                CommonFontNameRaw = source.Baseline.CommonFontNameRaw,
                CommonFontSize = source.Baseline.CommonFontSize,
                CommonFontSizeRaw = source.Baseline.CommonFontSizeRaw,
                CommonJustification = source.Baseline.CommonJustification,
                CommonJustificationRaw = source.Baseline.CommonJustificationRaw,
                CommonSpacingBefore = source.Baseline.CommonSpacingBefore,
                CommonSpacingBeforeRaw = source.Baseline.CommonSpacingBeforeRaw,
                CommonSpacingAfter = source.Baseline.CommonSpacingAfter,
                CommonSpacingAfterRaw = source.Baseline.CommonSpacingAfterRaw,
                CommonLineSpacing = source.Baseline.CommonLineSpacing,
                CommonLineSpacingRaw = source.Baseline.CommonLineSpacingRaw,
                CommonFirstLineIndent = source.Baseline.CommonFirstLineIndent,
                CommonFirstLineIndentRaw = source.Baseline.CommonFirstLineIndentRaw,
                CommonTableColumnCount = source.Baseline.CommonTableColumnCount,
                PageSetup = source.Baseline.PageSetup
            }
        };
    }
}
