using Lianxin.Sms.Api.Persistence;

namespace Lianxin.Sms.Api.Hosting;

internal sealed class SmsTicker(SmsStore store) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(store.Demo ? 700 : 10_000));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                try
                {
                    store.Use(() =>
                    {
                        store.Tick();
                        return true;
                    });
                }
                catch
                {
                    // Retry on the next tick; never log SMS data.
                }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
