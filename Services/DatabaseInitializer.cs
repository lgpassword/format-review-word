using Microsoft.Data.Sqlite;

namespace WordFormatAnalyzer.Services;

public sealed class DatabaseInitializer
{
    private readonly AppStorage _storage;

    public DatabaseInitializer(AppStorage storage)
    {
        _storage = storage;
    }

    public async Task InitializeAsync()
    {
        await using var connection = new SqliteConnection($"Data Source={_storage.DatabasePath}");
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS AnalysisSessions (
                Id TEXT PRIMARY KEY,
                CreatedAt TEXT NOT NULL,
                TemplateFileName TEXT NOT NULL,
                TemplatePath TEXT NOT NULL,
                TemplateAnalysisJson TEXT NOT NULL,
                ReferenceTemplatesJson TEXT NULL,
                RuleConflictsJson TEXT NULL,
                EffectiveRulesJson TEXT NULL,
                TargetFileName TEXT NULL,
                TargetPath TEXT NULL,
                ReportJson TEXT NULL,
                AnnotatedPath TEXT NULL,
                HtmlReportPath TEXT NULL
            );
            """;

        await command.ExecuteNonQueryAsync();
        await AddColumnIfMissingAsync(connection, "ReferenceTemplatesJson", "TEXT NULL");
        await AddColumnIfMissingAsync(connection, "RuleConflictsJson", "TEXT NULL");
        await AddColumnIfMissingAsync(connection, "EffectiveRulesJson", "TEXT NULL");
        await AddColumnIfMissingAsync(connection, "HtmlReportPath", "TEXT NULL");
    }

    private static async Task AddColumnIfMissingAsync(SqliteConnection connection, string columnName, string definition)
    {
        var existsCommand = connection.CreateCommand();
        existsCommand.CommandText = "PRAGMA table_info(AnalysisSessions);";

        await using var reader = await existsCommand.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (reader.GetString(1).Equals(columnName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        var addCommand = connection.CreateCommand();
        addCommand.CommandText = $"ALTER TABLE AnalysisSessions ADD COLUMN {columnName} {definition};";
        await addCommand.ExecuteNonQueryAsync();
    }
}
