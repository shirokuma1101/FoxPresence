using System.IO.Pipes;
using System.Security.Principal;
using FirefoxDiscordPresence.Shared;
namespace FirefoxDiscordPresence.Bridge.Services;
public sealed class PipeMessageClient
{
    public async Task<bool> TrySendAsync(ReadOnlyMemory<byte> payload, CancellationToken token)
    {
        for (var attempt = 0; attempt < 3; attempt++) try
        {
            await using var pipe = new NamedPipeClientStream(".", ProtocolConstants.PipeName, PipeDirection.Out, PipeOptions.Asynchronous, TokenImpersonationLevel.Identification);
            await pipe.ConnectAsync(750, token); await pipe.WriteAsync(payload, token); await pipe.FlushAsync(token); return true;
        }
        catch (Exception ex) when (ex is IOException or TimeoutException) { if (attempt < 2) await Task.Delay(200, token); }
        await Console.Error.WriteLineAsync("Tray pipe unavailable; message discarded."); return false;
    }
}
