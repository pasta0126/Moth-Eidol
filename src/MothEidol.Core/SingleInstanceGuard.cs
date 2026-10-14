using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;

namespace MothEidol.Core;

/// <summary>
/// Allows one running instance per user session. A second instance signals the
/// first one (so it can restore its window) and then reports that it is not primary.
/// </summary>
public sealed class SingleInstanceGuard : IDisposable
{
    private readonly string _name;
    private readonly string _pipeName;
    private Mutex? _mutex;
    private CancellationTokenSource? _listenerCts;

    public SingleInstanceGuard(string name)
    {
        _name = name;
        // Unix domain socket paths are length-limited (~104 chars on macOS), so use a short stable name.
        _pipeName = "me-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(name)))[..16].ToLowerInvariant();
    }

    /// <summary>Raised on the primary instance (on a background thread) when another instance was launched.</summary>
    public event Action? ActivationRequested;

    /// <summary>Returns true if this process is the primary instance.</summary>
    public bool TryAcquire()
    {
        _mutex = new Mutex(initiallyOwned: true, _name, out var createdNew);
        if (!createdNew)
        {
            _mutex.Dispose();
            _mutex = null;
            SignalPrimary();
            return false;
        }

        _listenerCts = new CancellationTokenSource();
        _ = Task.Run(() => ListenAsync(_listenerCts.Token));
        return true;
    }

    private void SignalPrimary()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out);
            client.Connect(timeout: 2000);
            client.WriteByte(1);
        }
        catch (Exception ex) when (ex is IOException or TimeoutException or UnauthorizedAccessException)
        {
            // The primary instance could not be reached; nothing more to do.
        }
    }

    private async Task ListenAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(
                    _pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(token);
                var buffer = new byte[1];
                _ = await server.ReadAsync(buffer, token);
                ActivationRequested?.Invoke();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (IOException)
            {
                await Task.Delay(200, CancellationToken.None);
            }
        }
    }

    public void Dispose()
    {
        _listenerCts?.Cancel();
        _listenerCts?.Dispose();
        if (_mutex is not null)
        {
            try { _mutex.ReleaseMutex(); } catch (ApplicationException) { }
            _mutex.Dispose();
        }
    }
}
