using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WordFormatAnalyzer.Services;

public sealed class WordConversionService
{
    private readonly AppStorage _storage;

    public WordConversionService(AppStorage storage)
    {
        _storage = storage;
    }

    public Task<string> EnsureDocxAsync(string sourcePath, string fileName)
    {
        var extension = Path.GetExtension(fileName);
        if (extension.Equals(".docx", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(sourcePath);
        }

        if (!extension.Equals(".doc", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("仅支持 .doc 和 .docx 文件。");
        }

        if (!OperatingSystem.IsWindows())
        {
            throw new InvalidOperationException("当前环境不支持 .doc 转换，请改用 .docx 文件。");
        }

        var convertedPath = _storage.CreateWorkingPath($"{Path.GetFileNameWithoutExtension(fileName)}-converted.docx");
        return ConvertDocToDocxAsync(sourcePath, convertedPath);
    }

    [SupportedOSPlatform("windows")]
    private static Task<string> ConvertDocToDocxAsync(string sourcePath, string convertedPath)
    {
        var task = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            object? wordApp = null;
            object? documents = null;
            object? document = null;

            try
            {
                var wordType = Type.GetTypeFromProgID("Word.Application")
                    ?? throw new InvalidOperationException("当前环境未安装 Word，无法处理 .doc 文件。");

                wordApp = Activator.CreateInstance(wordType)
                    ?? throw new InvalidOperationException("无法启动 Word 自动化转换。");

                dynamic app = wordApp;
                app.Visible = false;
                app.DisplayAlerts = 0;

                documents = app.Documents;
                dynamic docs = documents;
                document = docs.Open(sourcePath, false, true);
                dynamic doc = document;
                doc.SaveAs2(convertedPath, 16);
                doc.Close(false);
                app.Quit(false);

                task.TrySetResult(convertedPath);
            }
            catch (Exception ex)
            {
                try
                {
                    if (document is not null)
                    {
                        dynamic doc = document;
                        doc.Close(false);
                    }
                }
                catch
                {
                    // ignore cleanup failures
                }

                try
                {
                    if (wordApp is not null)
                    {
                        dynamic app = wordApp;
                        app.Quit(false);
                    }
                }
                catch
                {
                    // ignore cleanup failures
                }

                task.TrySetException(new InvalidOperationException("无法将 .doc 转换为 .docx。请确认本机已安装 Microsoft Word。", ex));
            }
            finally
            {
                if (document is not null)
                {
                    Marshal.FinalReleaseComObject(document);
                }

                if (documents is not null)
                {
                    Marshal.FinalReleaseComObject(documents);
                }

                if (wordApp is not null)
                {
                    Marshal.FinalReleaseComObject(wordApp);
                }
            }
        })
        {
            IsBackground = true
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return task.Task;
    }
}
