using Eshhanly.Application.Bookings;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Eshhanly.Infrastructure.BackgroundJobs;

/// <summary>Every minute: expire overdue bookings and mark no-shows. Simple in-process timer (no Hangfire needed yet).</summary>
internal sealed partial class BookingTimeoutsWorker(IServiceScopeFactory scopeFactory, ILogger<BookingTimeoutsWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var changed = await scope.ServiceProvider.GetRequiredService<ISender>()
                    .Send(new ProcessBookingTimeoutsCommand(), stoppingToken);

                if (changed > 0)
                {
                    LogProcessed(changed);
                }
            }
#pragma warning disable CA1031 // A failing tick must not kill the worker; it retries next minute.
            catch (Exception ex) when (ex is not OperationCanceledException)
#pragma warning restore CA1031
            {
                LogFailed(ex);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Booking timeouts processed: {Count} booking(s) updated")]
    private partial void LogProcessed(int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Booking timeouts tick failed")]
    private partial void LogFailed(Exception exception);
}
