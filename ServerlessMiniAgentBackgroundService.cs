using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Datadog.Trace.MiniAgent.Bootstrap;

public class ServerlessMiniAgentBackgroundService : BackgroundService
{
    private readonly ILogger<ServerlessMiniAgentBackgroundService> _logger;
    private readonly ServerlessMiniAgent _serverlessMiniAgent;

    public ServerlessMiniAgentBackgroundService(ILogger<ServerlessMiniAgentBackgroundService> logger)
    {
        _logger = logger;
        _serverlessMiniAgent = new ServerlessMiniAgent(logger);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var process = _serverlessMiniAgent.Start();
        _logger.LogInformation("[Mini-Agent] Process started with PID {ProcessId}.", process.Id);

        await process.WaitForExitAsync(stoppingToken);
        _logger.LogInformation("[Mini-Agent] Process {ProcessId} exited with code {ExitCode}.", process.Id, process.ExitCode);
    }
}
