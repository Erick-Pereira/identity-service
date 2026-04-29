using Microsoft.EntityFrameworkCore;
using Simcag.IdentityService.Domain.Entities;
using Simcag.IdentityService.Infrastructure.Persistence.DbContext;

namespace Simcag.IdentityService.Api.Workers;

/// <summary>
/// Job em segundo plano que percorre <see cref="ConformityItem"/> não concluídos
/// com <see cref="ConformityItem.DueDate"/> vencido e registra log estruturado.
/// O <see cref="ConformityItem.Status"/> já é computado em runtime; este worker
/// existe para emitir alertas/log/observabilidade sobre o universo OVERDUE.
/// Executa a cada <c>OVERDUE_CONFORMITY_INTERVAL_HOURS</c> (default 6h).
/// </summary>
public sealed class OverdueConformityWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<OverdueConformityWorker> _logger;
    private readonly TimeSpan _interval;

    public OverdueConformityWorker(IServiceProvider services, ILogger<OverdueConformityWorker> logger)
    {
        _services = services;
        _logger = logger;

        var raw = Environment.GetEnvironmentVariable("OVERDUE_CONFORMITY_INTERVAL_HOURS");
        _interval = double.TryParse(raw, out var hours) && hours > 0
            ? TimeSpan.FromHours(hours)
            : TimeSpan.FromHours(6);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OverdueConformityWorker iniciado. Intervalo {Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IdentityServiceDbContext>();
                var now = DateTime.UtcNow;

                var overdue = await db.ConformityItems
                    .AsNoTracking()
                    .Where(c => c.CompletedAt == null && c.DueDate.HasValue && c.DueDate < now)
                    .ToListAsync(stoppingToken);

                if (overdue.Count > 0)
                {
                    _logger.LogWarning(
                        "{Count} conformidades em atraso detectadas",
                        overdue.Count);
                    foreach (var item in overdue)
                    {
                        _logger.LogWarning(
                            "Conformidade OVERDUE: condominio={CondominioId} type={Type} dueDate={DueDate}",
                            item.CondominioId, item.Type, item.DueDate);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha no OverdueConformityWorker");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (TaskCanceledException) { /* shutdown */ }
        }
    }
}
