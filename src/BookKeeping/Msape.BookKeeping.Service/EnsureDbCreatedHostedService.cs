using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Msape.BookKeeping.Data.EF;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Service
{
    public class EnsureDbCreatedHostedService : BackgroundService
    {
        private readonly IServiceProvider _provider;

        public EnsureDbCreatedHostedService(IServiceProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            using var scope = _provider.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<BookKeepingContext>>();
            var context = scope.ServiceProvider.GetRequiredService<BookKeepingContext>();
            try
            {
                await context.Database.EnsureCreatedAsync(stoppingToken)
                    .ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                if(logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(ex, "Error creating database");
                }
            }
        }
    }
}
