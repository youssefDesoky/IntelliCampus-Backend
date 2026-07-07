using IntelliCampus.Service_Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IntelliCampus.Service;

public class DepartmentAllocationHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DepartmentAllocationHostedService> _logger;

    public DepartmentAllocationHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<DepartmentAllocationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Department allocation service starting");

        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var allocationService = scope.ServiceProvider
                    .GetRequiredService<IDepartmentAllocationService>();

                var repo = scope.ServiceProvider
                    .GetRequiredService<IntelliCampus.Domain.Interfaces.IUnitOfWork>()
                    .GetRepository<IntelliCampus.Domain.Entities.Student, int>();

                var hasUnallocated = await repo.AnyAsync(s =>
                    s.DepartmentId == null);

                if (hasUnallocated)
                {
                    _logger.LogInformation("Unallocated students found, running allocation");
                    var result = await allocationService.RunAllocationAsync();
                    _logger.LogInformation(
                        "Allocation complete: {Allocated} assigned, {Unallocated} unallocated",
                        result.Allocations.Count, result.Unallocated.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running department allocation");
                throw;
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
