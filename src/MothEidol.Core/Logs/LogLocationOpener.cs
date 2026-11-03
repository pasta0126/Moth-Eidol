using MothEidol.Core.Platform;

namespace MothEidol.Core.Logs;

/// <summary>What happened when a location was opened; a message is set when it did not work.</summary>
public sealed record OpenResult(bool Opened, string? Message)
{
    public static OpenResult Success { get; } = new(true, null);
}

/// <summary>
/// Opens the location of a log source in the file manager: the followed file when known (the newest match of a
/// folder source), otherwise the folder. Existence checks run off the caller's thread with a timeout so an
/// unreachable network share cannot freeze the interface.
/// </summary>
public sealed class LogLocationOpener(IFileManager fileManager, TimeSpan? timeout = null)
{
    private readonly TimeSpan _timeout = timeout ?? TimeSpan.FromSeconds(4);

    /// <summary>The folder a source lives in.</summary>
    public static string FolderOf(LogSource source) =>
        source.Kind == SourceKind.File ? Path.GetDirectoryName(source.Path) ?? source.Path : source.Path;

    /// <summary>The distinct folders of the sources, in order.</summary>
    public static IReadOnlyList<string> FoldersOf(IEnumerable<LogSource> sources) =>
        [.. sources.Select(FolderOf).Distinct(StringComparer.OrdinalIgnoreCase)];

    public Task<OpenResult> OpenAsync(LogSource source) => RunAsync(() => Resolve(source));

    public Task<OpenResult> OpenFolderAsync(string folder) => RunAsync(() => Resolve(folder, file: null));

    private async Task<OpenResult> RunAsync(Func<(string Path, bool IsFile, string Shown)?> resolve)
    {
        var work = Task.Run(resolve);
        var finished = await Task.WhenAny(work, Task.Delay(_timeout)) == work;
        if (!finished) return new OpenResult(false, "The location did not respond in time and is considered unavailable.");

        var target = await work;
        if (target is not { } t) return new OpenResult(false, "The location is unavailable: it does not exist or cannot be reached.");

        return fileManager.TryReveal(t.Path, t.IsFile, out var error)
            ? OpenResult.Success
            : new OpenResult(false, $"{error} The path is {t.Shown}");
    }

    private static (string Path, bool IsFile, string Shown)? Resolve(LogSource source)
    {
        try
        {
            if (source.Kind == SourceKind.File)
            {
                if (File.Exists(source.Path)) return (source.Path, true, source.Path);
                return Resolve(FolderOf(source), file: null);
            }

            string? newest = null;
            if (Directory.Exists(source.Path))
            {
                newest = new DirectoryInfo(source.Path).EnumerateFiles(source.Pattern ?? "*")
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .FirstOrDefault()?.FullName;
            }
            return Resolve(source.Path, newest);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static (string Path, bool IsFile, string Shown)? Resolve(string folder, string? file)
    {
        try
        {
            if (file is not null) return (file, true, file);
            return Directory.Exists(folder) ? (folder, false, folder) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
