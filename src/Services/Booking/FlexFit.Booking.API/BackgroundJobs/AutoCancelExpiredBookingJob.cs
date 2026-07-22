using FlexFit.Booking.Service.Service.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlexFit.Booking.API.BackgroundJobs
{
    public class AutoCancelExpiredBookingJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AutoCancelExpiredBookingJob> _logger;

        public AutoCancelExpiredBookingJob(IServiceScopeFactory scopeFactory, ILogger<AutoCancelExpiredBookingJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Flexfit Auto Cancel Expired Booking Job initialized.");

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1)); // Runs scan every 1 minute

            while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpirationsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in automatic Expired Booking Cancellation scan.");
                }
            }
        }

        private async Task ProcessExpirationsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var maintenanceService = scope.ServiceProvider.GetRequiredService<IBookingMaintenanceService>();
            await maintenanceService.ProcessExpirationsAsync();
        }
    }
}
