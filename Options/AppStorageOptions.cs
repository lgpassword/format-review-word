namespace WordFormatAnalyzer.Options;

public sealed class AppStorageOptions
{
    public string DataDirectory { get; set; } = "App_Data";
    public string UploadDirectory { get; set; } = "Uploads";
    public string GeneratedDirectory { get; set; } = "Generated";
    public string DatabaseFileName { get; set; } = "word-analyzer.db";
}
