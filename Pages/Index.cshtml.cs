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

    public IndexModel(
        AppStorage storage,
        AnalysisSessionRepository sessions,
        WordAnalysisService analysis,
        WordComparisonService comparison,
        WordAnnotationService annotation,
        ReportRenderService reports)
    {
        _storage = storage;
        _sessions = sessions;
        _analysis = analysis;
        _comparison = comparison;
        _annotation = annotation;
        _reports = reports;
    }

    [BindProperty]
    public IFormFile? TemplateFile { get; set; }

    [BindProperty]
    public IFormFile? TargetFile { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SessionId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Severity { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    public AnalysisSession? Session { get; private set; }
    public IReadOnlyList<FormatIssue> VisibleIssues { get; private set; } = [];

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

        var path = _storage.CreateUploadPath(TemplateFile!.FileName);
        await SaveUploadAsync(TemplateFile, path);
        var analysis = _analysis.Analyze(path, TemplateFile.FileName);

        var session = new AnalysisSession
        {
            Id = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.Now,
            TemplateFileName = TemplateFile.FileName,
            TemplatePath = path,
            TemplateAnalysis = analysis
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

        var targetPath = _storage.CreateUploadPath(TargetFile!.FileName);
        await SaveUploadAsync(TargetFile, targetPath);
        var targetAnalysis = _analysis.Analyze(targetPath, TargetFile.FileName);
        var report = _comparison.Compare(session.TemplateAnalysis, targetAnalysis);

        var annotatedPath = _storage.CreateGeneratedPath($"{Path.GetFileNameWithoutExtension(TargetFile.FileName)}-格式检查批注.docx");
        _annotation.CreateAnnotatedCopy(targetPath, annotatedPath, report.Issues);
        report.AnnotatedWordDownloadName = Path.GetFileName(annotatedPath);

        var htmlPath = _storage.CreateGeneratedPath($"{Path.GetFileNameWithoutExtension(TargetFile.FileName)}-格式检测报告.html");
        _reports.WriteHtmlReport(report, htmlPath);
        report.HtmlReportDownloadName = Path.GetFileName(htmlPath);

        session.TargetFileName = TargetFile.FileName;
        session.TargetPath = targetPath;
        session.Report = report;
        session.AnnotatedPath = annotatedPath;
        session.HtmlReportPath = htmlPath;
        await _sessions.UpdateReportAsync(session);

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

        return Path.GetExtension(file.FileName).Equals(".docx", StringComparison.OrdinalIgnoreCase)
            ? null
            : "目前只支持 .docx 文件。";
    }

    private static async Task SaveUploadAsync(IFormFile file, string path)
    {
        await using var stream = System.IO.File.Create(path);
        await file.CopyToAsync(stream);
    }
}
