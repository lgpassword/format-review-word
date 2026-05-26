using System.Text.Json;
using Microsoft.Data.Sqlite;
using WordFormatAnalyzer.Models;

namespace WordFormatAnalyzer.Services;

public sealed class AnalysisSessionRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppStorage _storage;

    public AnalysisSessionRepository(AppStorage storage)
    {
        _storage = storage;
    }

    public async Task SaveTemplateAsync(AnalysisSession session)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR REPLACE INTO AnalysisSessions
                (Id, CreatedAt, TemplateFileName, TemplatePath, TemplateAnalysisJson,
                 ReferenceTemplatesJson, RuleConflictsJson,
                 TargetFileName, TargetPath, ReportJson, AnnotatedPath, HtmlReportPath)
            VALUES
                ($id, $createdAt, $templateFileName, $templatePath, $templateAnalysisJson,
                 $referenceTemplatesJson, $ruleConflictsJson,
                 $targetFileName, $targetPath, $reportJson, $annotatedPath, $htmlReportPath);
            """;
        command.Parameters.AddWithValue("$id", session.Id);
        command.Parameters.AddWithValue("$createdAt", session.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$templateFileName", session.TemplateFileName);
        command.Parameters.AddWithValue("$templatePath", session.TemplatePath);
        command.Parameters.AddWithValue("$templateAnalysisJson", JsonSerializer.Serialize(session.TemplateAnalysis, JsonOptions));
        command.Parameters.AddWithValue("$referenceTemplatesJson", JsonSerializer.Serialize(session.ReferenceTemplates, JsonOptions));
        command.Parameters.AddWithValue("$ruleConflictsJson", JsonSerializer.Serialize(session.RuleConflicts, JsonOptions));
        command.Parameters.AddWithValue("$targetFileName", (object?)session.TargetFileName ?? DBNull.Value);
        command.Parameters.AddWithValue("$targetPath", (object?)session.TargetPath ?? DBNull.Value);
        command.Parameters.AddWithValue("$reportJson", session.Report is null ? DBNull.Value : JsonSerializer.Serialize(session.Report, JsonOptions));
        command.Parameters.AddWithValue("$annotatedPath", (object?)session.AnnotatedPath ?? DBNull.Value);
        command.Parameters.AddWithValue("$htmlReportPath", (object?)session.HtmlReportPath ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<AnalysisSession?> GetAsync(string id)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, CreatedAt, TemplateFileName, TemplatePath, TemplateAnalysisJson,
                   ReferenceTemplatesJson, RuleConflictsJson,
                   TargetFileName, TargetPath, ReportJson, AnnotatedPath, HtmlReportPath
            FROM AnalysisSessions
            WHERE Id = $id;
            """;
        command.Parameters.AddWithValue("$id", id);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new AnalysisSession
        {
            Id = reader.GetString(0),
            CreatedAt = DateTime.Parse(reader.GetString(1)),
            TemplateFileName = reader.GetString(2),
            TemplatePath = reader.GetString(3),
            TemplateAnalysis = JsonSerializer.Deserialize<DocumentAnalysisResult>(reader.GetString(4), JsonOptions) ?? new(),
            ReferenceTemplates = reader.IsDBNull(5) ? [] : JsonSerializer.Deserialize<List<TemplateReferenceAnalysis>>(reader.GetString(5), JsonOptions) ?? [],
            RuleConflicts = reader.IsDBNull(6) ? [] : JsonSerializer.Deserialize<List<TemplateRuleConflict>>(reader.GetString(6), JsonOptions) ?? [],
            TargetFileName = reader.IsDBNull(7) ? null : reader.GetString(7),
            TargetPath = reader.IsDBNull(8) ? null : reader.GetString(8),
            Report = reader.IsDBNull(9) ? null : JsonSerializer.Deserialize<AnalysisReport>(reader.GetString(9), JsonOptions),
            AnnotatedPath = reader.IsDBNull(10) ? null : reader.GetString(10),
            HtmlReportPath = reader.IsDBNull(11) ? null : reader.GetString(11)
        };
    }

    public async Task UpdateReportAsync(AnalysisSession session)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE AnalysisSessions
            SET TargetFileName = $targetFileName,
                TargetPath = $targetPath,
                ReportJson = $reportJson,
                AnnotatedPath = $annotatedPath,
                ReferenceTemplatesJson = $referenceTemplatesJson,
                RuleConflictsJson = $ruleConflictsJson,
                HtmlReportPath = $htmlReportPath
            WHERE Id = $id;
            """;
        command.Parameters.AddWithValue("$id", session.Id);
        command.Parameters.AddWithValue("$targetFileName", (object?)session.TargetFileName ?? DBNull.Value);
        command.Parameters.AddWithValue("$targetPath", (object?)session.TargetPath ?? DBNull.Value);
        command.Parameters.AddWithValue("$reportJson", session.Report is null ? DBNull.Value : JsonSerializer.Serialize(session.Report, JsonOptions));
        command.Parameters.AddWithValue("$annotatedPath", (object?)session.AnnotatedPath ?? DBNull.Value);
        command.Parameters.AddWithValue("$referenceTemplatesJson", JsonSerializer.Serialize(session.ReferenceTemplates, JsonOptions));
        command.Parameters.AddWithValue("$ruleConflictsJson", JsonSerializer.Serialize(session.RuleConflicts, JsonOptions));
        command.Parameters.AddWithValue("$htmlReportPath", (object?)session.HtmlReportPath ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    private SqliteConnection CreateConnection()
    {
        return new SqliteConnection($"Data Source={_storage.DatabasePath}");
    }
}
