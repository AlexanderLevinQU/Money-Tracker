using System;
using System.IO;
using System.Threading;

namespace MoneyTracker.UI.Services;

public static class Logger
{
    private static readonly object _lock = new object();
    private static string? _logFilePath;
    private static long _maxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private static int _maxRetainedFiles = 5;

    public static void Init(string? logFilePath = null, long? maxFileSizeBytes = null, int? maxRetainedFiles = null)
    {
        if (!string.IsNullOrEmpty(_logFilePath)) return;
        _logFilePath = logFilePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MoneyTracker", "debug.log");
        if (maxFileSizeBytes.HasValue) _maxFileSizeBytes = maxFileSizeBytes.Value;
        if (maxRetainedFiles.HasValue) _maxRetainedFiles = maxRetainedFiles.Value;
        try
        {
            var dir = Path.GetDirectoryName(_logFilePath) ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MoneyTracker");
            Directory.CreateDirectory(dir);
        }
        catch
        {
            // ignore directory create failures; we'll still try console output
        }
    }

    public static void Log(string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        try
        {
            Console.WriteLine(line);
        }
        catch { }

        if (string.IsNullOrEmpty(_logFilePath)) Init();

        try
        {
            lock (_lock)
            {
                RotateIfNeeded();
                File.AppendAllText(_logFilePath!, line + Environment.NewLine);
            }
        }
        catch
        {
            // swallow file write errors
        }
    }

    private static void RotateIfNeeded()
    {
        try
        {
            if (string.IsNullOrEmpty(_logFilePath)) return;
            if (!File.Exists(_logFilePath)) return;

            var fi = new FileInfo(_logFilePath);
            if (fi.Length < _maxFileSizeBytes) return;

            var dir = fi.DirectoryName ?? Path.GetDirectoryName(_logFilePath)!;
            var baseName = Path.GetFileNameWithoutExtension(_logFilePath);
            var ext = Path.GetExtension(_logFilePath);
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var rotatedName = Path.Combine(dir, $"{baseName}-{timestamp}{ext}");

            // Move current log to rotated name
            File.Move(_logFilePath, rotatedName);

            // Clean up old rotated files, keep newest _maxRetainedFiles
            try
            {
                var pattern = $"{baseName}-*{ext}";
                var files = Directory.GetFiles(dir, pattern)
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTimeUtc)
                    .ToList();

                for (int i = _maxRetainedFiles; i < files.Count; i++)
                {
                    try { files[i].Delete(); } catch { }
                }
            }
            catch { }
        }
        catch
        {
            // swallow rotation errors to avoid breaking logging
        }
    }

    public static void LogException(Exception ex, string? context = null)
    {
        Log((context is null ? "Exception" : context) + ": " + ex.ToString());
    }
}
