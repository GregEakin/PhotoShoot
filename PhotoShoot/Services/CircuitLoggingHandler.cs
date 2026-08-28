using Microsoft.AspNetCore.Components.Server.Circuits;

namespace PhotoShoot.Services;

public sealed class CircuitLoggingHandler : CircuitHandler
{
    private readonly ILogger<CircuitLoggingHandler> _logger;

    public CircuitLoggingHandler(ILogger<CircuitLoggingHandler> logger)
    {
        _logger = logger;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Circuit opened. CircuitId={CircuitId}, Instance={InstanceName}",
                circuit.Id,
                Environment.MachineName);
        }

        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Circuit closed. CircuitId={CircuitId}, Instance={InstanceName}",
                circuit.Id,
                Environment.MachineName);
        }

        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Circuit connection up. CircuitId={CircuitId}, Instance={InstanceName}",
                circuit.Id,
                Environment.MachineName);
        }

        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Circuit connection down. CircuitId={CircuitId}, Instance={InstanceName}",
                circuit.Id,
                Environment.MachineName);
        }

        return Task.CompletedTask;
    }
}
