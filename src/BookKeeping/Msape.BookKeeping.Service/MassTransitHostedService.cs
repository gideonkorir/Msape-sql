using MassTransit;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Service
{
    public class MassTransitHostedService : BackgroundService
    {
        private readonly IBusControl _bus;

        public MassTransitHostedService(IBusControl bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var handle = await _bus.StartAsync(stoppingToken).ConfigureAwait(false);
            var tcs = new TaskCompletionSource<object>();
            using(stoppingToken.Register(() => tcs.TrySetResult(new object())))
            {
                await tcs.Task.ConfigureAwait(false);
            }
            await handle.StopAsync(stoppingToken).ConfigureAwait(false);
        }
    }
}
