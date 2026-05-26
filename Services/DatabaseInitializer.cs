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
                TargetFileName TEXT NULL,
                TargetPath TEXT NULL,
                ReportJson TEXT NULL,
                AnnotatedPath TEXT NULL,
                HtmlReportPath TEXT NULL
            );
            """;

        await command.ExecuteNonQueryAsync();
    }
}
