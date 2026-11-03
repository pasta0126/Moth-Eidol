using MothEidol.Core.Logs;
using MothEidol.Core.Platform;

namespace MothEidol.Core.Tests.Logs;

public sealed class LogLocationOpenerTests : IDisposable
{
    private sealed class FakeFileManager : IFileManager
    {
        public List<(string Path, bool IsFile)> Calls { get; } = [];
        public string? Failure { get; set; }

        public bool TryReveal(string path, bool isFile, out string? error)
        {
            Calls.Add((path, isFile));
            error = Failure;
            return Failure is null;
        }
    }

    private readonly string _dir = Directory.CreateTempSubdirectory("moth-open-").FullName;
    private readonly FakeFileManager _fm = new();

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private LogLocationOpener Opener => new(_fm);

    [Fact]
    public async Task FileSource_RevealsTheFile()
    {
        var file = Path.Combine(_dir, "a.log");
        File.WriteAllText(file, "x");

        var result = await Opener.OpenAsync(new LogSource(1, SourceKind.File, file, null, true));

        Assert.True(result.Opened);
        Assert.Equal([(file, true)], _fm.Calls);
    }

    [Fact]
    public async Task FolderSource_RevealsTheNewestMatch()
    {
        var old = Path.Combine(_dir, "svc1.log");
        var newest = Path.Combine(_dir, "svc2.log");
        File.WriteAllText(old, "x");
        File.WriteAllText(newest, "x");
        File.SetLastWriteTimeUtc(old, DateTime.UtcNow.AddDays(-1));
        File.WriteAllText(Path.Combine(_dir, "other.txt"), "x");

        await Opener.OpenAsync(new LogSource(1, SourceKind.Folder, _dir, "svc*.log", true));

        Assert.Equal([(newest, true)], _fm.Calls);
    }

    [Fact]
    public async Task FolderSource_WithoutMatches_OpensTheFolder()
    {
        await Opener.OpenAsync(new LogSource(1, SourceKind.Folder, _dir, "*.log", true));

        Assert.Equal([(_dir, false)], _fm.Calls);
    }

    [Fact]
    public async Task MissingFile_OpensItsFolder()
    {
        await Opener.OpenAsync(new LogSource(1, SourceKind.File, Path.Combine(_dir, "gone.log"), null, true));

        Assert.Equal([(_dir, false)], _fm.Calls);
    }

    [Fact]
    public async Task MissingFolder_ReportsUnavailable()
    {
        var result = await Opener.OpenAsync(new LogSource(1, SourceKind.Folder, Path.Combine(_dir, "nope"), "*", true));

        Assert.False(result.Opened);
        Assert.Contains("unavailable", result.Message);
        Assert.Empty(_fm.Calls);
    }

    [Fact]
    public async Task NoFileManager_ShowsThePath()
    {
        _fm.Failure = "No file manager is available.";

        var result = await Opener.OpenFolderAsync(_dir);

        Assert.False(result.Opened);
        Assert.Contains(_dir, result.Message);
    }

    [Fact]
    public void FoldersOf_AreDistinct()
    {
        var sources = new[]
        {
            new LogSource(1, SourceKind.File, Path.Combine(_dir, "a.log"), null, true),
            new LogSource(2, SourceKind.Folder, _dir, "*.log", true),
            new LogSource(3, SourceKind.Folder, Path.Combine(_dir, "sub"), "*", true),
        };

        Assert.Equal([_dir, Path.Combine(_dir, "sub")], LogLocationOpener.FoldersOf(sources));
    }
}
