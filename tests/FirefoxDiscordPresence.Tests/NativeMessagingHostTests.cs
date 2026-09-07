using FirefoxDiscordPresence.Bridge.Services;
namespace FirefoxDiscordPresence.Tests;
public sealed class NativeMessagingHostTests
{
    [Fact]
    public async Task TryReadExactlyAsync_HandlesPartialReads()
    {
        await using var stream = new ChunkedStream([1, 2, 3, 4], 1);
        var buffer = new byte[4];
        Assert.True(await NativeMessagingHost.TryReadExactlyAsync(stream, buffer, CancellationToken.None));
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, buffer);
    }
    private sealed class ChunkedStream(byte[] bytes, int chunkSize) : MemoryStream(bytes)
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => base.ReadAsync(buffer[..Math.Min(chunkSize, buffer.Length)], cancellationToken);
    }
}
