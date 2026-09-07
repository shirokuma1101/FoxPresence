using FirefoxDiscordPresence.Bridge.Services;
try { await new NativeMessagingHost(Console.OpenStandardInput(), new PipeMessageClient()).RunAsync(CancellationToken.None); }
catch (Exception ex) { await Console.Error.WriteLineAsync($"Native messaging host stopped: {ex.Message}"); }
return 0;
