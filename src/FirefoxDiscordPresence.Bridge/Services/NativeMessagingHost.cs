using System.Buffers.Binary;
using System.Text.Json;
using FirefoxDiscordPresence.Shared;
using FirefoxDiscordPresence.Shared.Models;
namespace FirefoxDiscordPresence.Bridge.Services;
public sealed class NativeMessagingHost(Stream input, PipeMessageClient pipeClient)
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };
    public async Task RunAsync(CancellationToken token)
    {
        var header = new byte[4];
        while (await TryReadExactlyAsync(input, header, token))
        {
            var length = BinaryPrimitives.ReadInt32LittleEndian(header);
            if (length is <= 0 or > ProtocolConstants.MaximumMessageBytes) throw new InvalidDataException($"Invalid message length: {length}");
            var payload = new byte[length];
            if (!await TryReadExactlyAsync(input, payload, token)) throw new EndOfStreamException();
            try { var message = JsonSerializer.Deserialize<MediaPresence>(payload, Options)?.Normalize(); if (message is not null && message.Site.Length > 0) await pipeClient.TrySendAsync(payload, token); }
            catch (JsonException ex) { await Console.Error.WriteLineAsync($"Ignored invalid JSON: {ex.Message}"); }
        }
    }
    internal static async Task<bool> TryReadExactlyAsync(Stream stream, Memory<byte> buffer, CancellationToken token)
    {
        var offset = 0;
        while (offset < buffer.Length) { var read = await stream.ReadAsync(buffer[offset..], token); if (read == 0) return offset == 0 ? false : throw new EndOfStreamException(); offset += read; }
        return true;
    }
}
