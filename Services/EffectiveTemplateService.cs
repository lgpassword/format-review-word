using WordFormatAnalyzer.Models;

namespace WordFormatAnalyzer.Services;

public sealed class EffectiveTemplateService
{
    private readonly TemplateRuleConflictService _ruleConflicts;
    private readonly TemplateRuleService _templateRules;

    public EffectiveTemplateService(TemplateRuleConflictService ruleConflicts, TemplateRuleService templateRules)
    {
        _ruleConflicts = ruleConflicts;
        _templateRules = templateRules;
    }

    public DocumentAnalysisResult Build(AnalysisSession session)
    {
        var effective = Clone(session.ReferenceTemplates.LastOrDefault()?.Analysis ?? session.TemplateAnalysis);
        effective.Baseline = _ruleConflicts.ApplySelections(effective.Baseline, session.RuleConflicts);
        effective.FileName = session.ReferenceTemplates.LastOrDefault()?.FileName ?? session.TemplateFileName;
        effective.FormatRules = EffectiveRules(session, effective);
        return effective;
    }

    private List<TemplateFormatRule> EffectiveRules(AnalysisSession session, DocumentAnalysisResult effective)
    {
        if (session.EffectiveRules.Count > 0)
        {
            return session.EffectiveRules.Select(TemplateRuleService.Clone).ToList();
        }

        if (effective.FormatRules.Count > 0)
        {
            return effective.FormatRules.Select(TemplateRuleService.Clone).ToList();
        }

        return _templateRules.BuildRules(effective, "模板");
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
            FormatRules = source.FormatRules.Select(TemplateRuleService.Clone).ToList(),
            Baseline = new TemplateBaseline
            {
                CommonFontName = source.Baseline.CommonFontName,
                CommonFontNameRaw = source.Baseline.CommonFontNameRaw,
                CommonChineseFontName = source.Baseline.CommonChineseFontName,
                CommonChineseFontNameRaw = source.Baseline.CommonChineseFontNameRaw,
                CommonWesternFontName = source.Baseline.CommonWesternFontName,
                CommonWesternFontNameRaw = source.Baseline.CommonWesternFontNameRaw,
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
