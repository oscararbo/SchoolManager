using Back.Api.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Infrastructure.Security;

public sealed class RefreshTokenCleanupService(IServiceScopeFactory scopeFactory, ILogger<RefreshTokenCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CleanupExpiredTokensAsync(stoppingToken);

        using var timer = new PeriodicTimer(CleanupInterval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CleanupExpiredTokensAsync(stoppingToken);
        }
    }

    private async Task CleanupExpiredTokensAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var nowUtc = DateTime.UtcNow;

            var query = context.RefreshTokens
                .IgnoreQueryFilters()
                .Where(t => t.ExpiresAtUtc <= nowUtc);

            int deleted;
            try
            {
                deleted = await query.ExecuteDeleteAsync(cancellationToken);
            }
            catch (InvalidOperationException)
            {
                // Some providers (e.g. InMemory in tests) do not support ExecuteDelete.
                var expiredTokens = await query.ToListAsync(cancellationToken);
                if (expiredTokens.Count == 0)
                    return;

                context.RefreshTokens.RemoveRange(expiredTokens);
                deleted = await context.SaveChangesAsync(cancellationToken);
            }

            if (deleted > 0)
                logger.LogInformation("Se eliminaron {Count} refresh tokens expirados.", deleted);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error eliminando refresh tokens expirados.");
        }
    }
}