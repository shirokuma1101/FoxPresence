using System.IO.Pipes;
using System.Text.Json;
using FirefoxDiscordPresence.Shared;
using FirefoxDiscordPresence.Shared.Models;
namespace FirefoxDiscordPresence.Tray.Services;
public sealed class NamedPipeServer(FileLogger logger) : IAsyncDisposable
{
    private readonly CancellationTokenSource _shutdown = new();
    private Task? _loop;
    public event Action<MediaPresence>? MessageReceived;
    public event Action<bool>? ConnectionChanged;
    public void Start() => _loop ??= AcceptLoopAsync(_shutdown.Token);
    private async Task AcceptLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await using var pipe = new NamedPipeServerStream(ProtocolConstants.PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync(token); ConnectionChanged?.Invoke(true); logger.Info("Named Pipe connection");
                using var buffer = new MemoryStream(); await pipe.CopyToAsync(buffer, token);
                if (buffer.Length is > 0 and <= ProtocolConstants.MaximumMessageBytes)
                {
                    var media = JsonSerializer.Deserialize<MediaPresence>(buffer.ToArray(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.Normalize();
                    if (media is not null && media.Site.Length > 0) MessageReceived?.Invoke(media);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.Error("Named Pipe error", ex); await Task.Delay(500, token).ConfigureAwait(false); }
            finally { ConnectionChanged?.Invoke(false); }
        }
    }
    public async ValueTask DisposeAsync() { _shutdown.Cancel(); if (_loop is not null) try { await _loop; } catch (OperationCanceledException) { } _shutdown.Dispose(); }
}
