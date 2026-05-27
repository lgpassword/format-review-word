using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WordFormatAnalyzer.Models;
using WordFormatAnalyzer.Services;

namespace WordFormatAnalyzer.Pages;

public class IndexModel : PageModel
{
    private const long MaxFileBytes = 20 * 1024 * 1024;
    private readonly AppStorage _storage;
    private readonly AnalysisSessionRepository _sessions;
    private readonly WordAnalysisService _analysis;
    private readonly WordComparisonService _comparison;
    private readonly WordAnnotationService _annotation;
    private readonly ReportRenderService _reports;
    private readonly WordConversionService _converter;
    private readonly NormalDocumentService _normalDocument;
    private readonly IssueTextService _issueText;
    private readonly TemplateRuleConflictService _ruleConflicts;
    private readonly EffectiveTemplateService _effectiveTemplate;
    private readonly TemplateRuleService _templateRules;

    public IndexModel(
        AppStorage storage,
        AnalysisSessionRepository sessions,
        WordAnalysisService analysis,
        WordComparisonService comparison,
        WordAnnotationService annotation,
        ReportRenderService reports,
        WordConversionService converter,
        NormalDocumentService normalDocument,
        IssueTextService issueText,
        TemplateRuleConflictService ruleConflicts,
        EffectiveTemplateService effectiveTemplate,
        TemplateRuleService templateRules)
    {
        _storage = storage;
        _sessions = sessions;
        _analysis = analysis;
        _comparison = comparison;
        _annotation = annotation;
        _reports = reports;
        _converter = converter;
        _normalDocument = normalDocument;
        _issueText = issueText;
        _ruleConflicts = ruleConflicts;
        _effectiveTemplate = effectiveTemplate;
        _templateRules = templateRules;
    }

    [BindProperty]
    public IFormFile? TemplateFile { get; set; }

    [BindProperty]
    public IFormFile? TargetFile { get; set; }

    [BindProperty]
    public IFormFile? ReferenceTemplateFile { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SessionId { get; set; }

    [BindProperty]
    public Dictionary<string, string> ConflictSelections { get; set; } = [];

    [BindProperty]
    public Dictionary<string, string> ConflictCustomValues { get; set; } = [];

    [BindProperty]
    public Dictionary<string, TemplateRuleEditInput> RuleEdits { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Severity { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    public AnalysisSession? Session { get; private set; }
    public IReadOnlyList<FormatIssue> VisibleIssues { get; private set; } = [];

    private static bool IsWindows => OperatingSystem.IsWindows();

    public string IssueTitle(FormatIssue issue) => _issueText.BuildReportTitle(issue);

    public string IssueExpected(FormatIssue issue) => _issueText.BuildExpectedText(issue);

    public string IssueActual(FormatIssue issue) => _issueText.BuildActualText(issue);

    public async Task OnGetAsync()
    {
        await LoadSessionAsync();
    }

    public async Task<IActionResult> OnPostTemplateAsync()
    {
        var validation = ValidateWordFile(TemplateFile);
        if (validation is not null)
        {
            ModelState.AddModelError(string.Empty, validation);
            return Page();
        }

        if (Path.GetExtension(TemplateFile!.FileName).Equals(".doc", StringComparison.OrdinalIgnoreCase) && !IsWindows)
        {
            ModelState.AddModelError(string.Empty, "当前环境不支持 .doc 转换，请在 Windows 且安装 Microsoft Word 的环境中使用 .doc。");
            return Page();
        }

        var path = _storage.CreateUploadPath(TemplateFile!.FileName);
        await SaveUploadAsync(TemplateFile, path);
        var analysisPath = await _converter.EnsureDocxAsync(path, TemplateFile.FileName);
        var analysis = _analysis.Analyze(analysisPath, TemplateFile.FileName);

        var session = new AnalysisSession
        {
            Id = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.Now,
            TemplateFileName = TemplateFile.FileName,
            TemplatePath = path,
            TemplateAnalysis = analysis,
            EffectiveRules = analysis.FormatRules.Select(TemplateRuleService.Clone).ToList()
        };

        await _sessions.SaveTemplateAsync(session);
        return RedirectToPage(new { sessionId = session.Id });
    }

    public async Task<IActionResult> OnPostTargetAsync()
    {
        if (string.IsNullOrWhiteSpace(SessionId))
        {
            ModelState.AddModelError(string.Empty, "请先上传并分析模板文档。");
            return Page();
        }

        var session = await _sessions.GetAsync(SessionId);
        if (session is null)
        {
            ModelState.AddModelError(string.Empty, "模板分析会话不存在，请重新上传模板。");
            return Page();
        }

        var validation = ValidateWordFile(TargetFile);
        if (validation is not null)
        {
            Session = session;
            ModelState.AddModelError(string.Empty, validation);
            return Page();
        }

        if (Path.GetExtension(TargetFile!.FileName).Equals(".doc", StringComparison.OrdinalIgnoreCase) && !IsWindows)
        {
            Session = session;
            ModelState.AddModelError(string.Empty, "当前环境不支持 .doc 转换，请在 Windows 且安装 Microsoft Word 的环境中使用 .doc。");
            return Page();
        }

        var targetPath = _storage.CreateUploadPath(TargetFile!.FileName);
        await SaveUploadAsync(TargetFile, targetPath);
        var targetAnalysisPath = await _converter.EnsureDocxAsync(targetPath, TargetFile.FileName);
        var targetAnalysis = _analysis.Analyze(targetAnalysisPath, TargetFile.FileName);
        var effectiveTemplate = _effectiveTemplate.Build(session);
        var report = _comparison.Compare(effectiveTemplate, targetAnalysis);

        var annotatedPath = _storage.CreateGeneratedPath($"{Path.GetFileNameWithoutExtension(TargetFile.FileName)}-格式检查批注.docx");
        _annotation.CreateAnnotatedCopy(targetAnalysisPath, annotatedPath, report.Issues);
        report.AnnotatedWordDownloadName = Path.GetFileName(annotatedPath);

        var htmlPath = _storage.CreateGeneratedPath($"{Path.GetFileNameWithoutExtension(TargetFile.FileName)}-格式检测报告.html");
        _reports.WriteHtmlReport(report, htmlPath);
        report.HtmlReportDownloadName = Path.GetFileName(htmlPath);

        var normalPath = _storage.CreateGeneratedPath($"{Path.GetFileNameWithoutExtension(TargetFile.FileName)}-正常文档.docx");
        _normalDocument.CreateNormalizedCopy(targetAnalysisPath, normalPath, effectiveTemplate, report.Issues);
        report.NormalDocumentDownloadName = Path.GetFileName(normalPath);

        session.TargetFileName = TargetFile.FileName;
        session.TargetPath = targetPath;
        session.Report = report;
        session.AnnotatedPath = annotatedPath;
        session.HtmlReportPath = htmlPath;
        await _sessions.UpdateReportAsync(session);

        return RedirectToPage(new { sessionId = session.Id });
    }

    public async Task<IActionResult> OnPostReferenceTemplateAsync()
    {
        if (string.IsNullOrWhiteSpace(SessionId))
        {
            ModelState.AddModelError(string.Empty, "请先上传总模板。");
            return Page();
        }

        var session = await _sessions.GetAsync(SessionId);
        if (session is null)
        {
            ModelState.AddModelError(string.Empty, "模板分析会话不存在，请重新上传模板。");
            return Page();
        }

        var validation = ValidateWordFile(ReferenceTemplateFile);
        if (validation is not null)
        {
            Session = session;
            ModelState.AddModelError(string.Empty, validation);
            return Page();
        }

        if (Path.GetExtension(ReferenceTemplateFile!.FileName).Equals(".doc", StringComparison.OrdinalIgnoreCase) && !IsWindows)
        {
            Session = session;
            ModelState.AddModelError(string.Empty, "当前环境不支持 .doc 转换，请在 Windows 且安装 Microsoft Word 的环境中使用 .doc。");
            return Page();
        }

        var path = _storage.CreateUploadPath(ReferenceTemplateFile!.FileName);
        await SaveUploadAsync(ReferenceTemplateFile, path);
        var analysisPath = await _converter.EnsureDocxAsync(path, ReferenceTemplateFile.FileName);
        var analysis = _analysis.Analyze(analysisPath, ReferenceTemplateFile.FileName);

        session.ReferenceTemplates.Add(new TemplateReferenceAnalysis
        {
            FileName = ReferenceTemplateFile.FileName,
            Path = path,
            Analysis = analysis
        });

        _ruleConflicts.MergeConflicts(session.RuleConflicts, _ruleConflicts.FindConflicts(session.TemplateAnalysis, analysis));
        session.EffectiveRules = _templateRules.MergeRules(session.EffectiveRules, analysis.FormatRules);

        await _sessions.SaveTemplateAsync(session);
        return RedirectToPage(new { sessionId = session.Id });
    }

    public async Task<IActionResult> OnPostResolveConflictsAsync()
    {
        if (string.IsNullOrWhiteSpace(SessionId))
        {
            ModelState.AddModelError(string.Empty, "请先上传总模板。");
            return Page();
        }

        var session = await _sessions.GetAsync(SessionId);
        if (session is null)
        {
            ModelState.AddModelError(string.Empty, "模板分析会话不存在，请重新上传模板。");
            return Page();
        }

        foreach (var conflict in session.RuleConflicts)
        {
            if (!ConflictSelections.TryGetValue(conflict.ConflictId, out var selected))
            {
                continue;
            }

            ConflictCustomValues.TryGetValue(conflict.ConflictId, out var customValue);
            _ruleConflicts.ResolveConflict(conflict, selected, customValue);
        }

        var currentRules = session.EffectiveRules.Count > 0
            ? session.EffectiveRules
            : session.TemplateAnalysis.FormatRules;
        session.EffectiveRules = _templateRules.ApplyResolvedConflicts(currentRules, session.RuleConflicts);
        await _sessions.SaveTemplateAsync(session);
        return RedirectToPage(new { sessionId = session.Id });
    }

    public async Task<IActionResult> OnPostSaveRulesAsync()
    {
        if (string.IsNullOrWhiteSpace(SessionId))
        {
            ModelState.AddModelError(string.Empty, "请先上传总模板。");
            return Page();
        }

        var session = await _sessions.GetAsync(SessionId);
        if (session is null)
        {
            ModelState.AddModelError(string.Empty, "模板分析会话不存在，请重新上传模板。");
            return Page();
        }

        var currentRules = session.EffectiveRules.Count > 0
            ? session.EffectiveRules
            : session.TemplateAnalysis.FormatRules;
        session.EffectiveRules = _templateRules.ApplyEdits(currentRules, RuleEdits);
        await _sessions.SaveTemplateAsync(session);
        return RedirectToPage(new { sessionId = session.Id });
    }

    public async Task<IActionResult> OnGetDownloadAsync(string sessionId, string kind)
    {
        var session = await _sessions.GetAsync(sessionId);
        if (session is null)
        {
            return NotFound();
        }

        var path = kind.Equals("html", StringComparison.OrdinalIgnoreCase)
            ? session.HtmlReportPath
            : kind.Equals("normal", StringComparison.OrdinalIgnoreCase)
                ? session.Report is null
                    ? null
                    : Path.Combine(_storage.GeneratedDirectory, session.Report.NormalDocumentDownloadName)
                : session.AnnotatedPath;

        if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
        {
            return NotFound();
        }

        var contentType = kind.Equals("html", StringComparison.OrdinalIgnoreCase)
            ? "text/html; charset=utf-8"
            : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

        return PhysicalFile(path, contentType, Path.GetFileName(path));
    }

    private async Task LoadSessionAsync()
    {
        if (string.IsNullOrWhiteSpace(SessionId))
        {
            return;
        }

        Session = await _sessions.GetAsync(SessionId);
        if (Session is not null && Session.EffectiveRules.Count == 0)
        {
            Session.EffectiveRules = Session.TemplateAnalysis.FormatRules.Select(TemplateRuleService.Clone).ToList();
        }

        VisibleIssues = ApplyFilters(Session?.Report?.Issues ?? []);
    }

    private IReadOnlyList<FormatIssue> ApplyFilters(IReadOnlyList<FormatIssue> issues)
    {
        return issues
            .Where(issue => string.IsNullOrWhiteSpace(Severity) || issue.Severity == Severity)
            .Where(issue => string.IsNullOrWhiteSpace(Category) || issue.Category == Category)
            .ToList();
    }

    private static string? ValidateWordFile(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return "请选择 .docx 文件。";
        }

        if (file.Length > MaxFileBytes)
        {
            return "文件不能超过 20 MB。";
        }

        var extension = Path.GetExtension(file.FileName);
        return extension.Equals(".docx", StringComparison.OrdinalIgnoreCase) || extension.Equals(".doc", StringComparison.OrdinalIgnoreCase)
            ? null
            : "目前只支持 .doc 和 .docx 文件。";
    }

    private static async Task SaveUploadAsync(IFormFile file, string path)
    {
        await using var stream = System.IO.File.Create(path);
        await file.CopyToAsync(stream);
    }
}
