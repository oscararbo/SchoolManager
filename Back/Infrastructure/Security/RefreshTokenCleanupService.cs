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

            var expiredTokens = await context.RefreshTokens
                .Where(t => t.ExpiresAtUtc <= nowUtc)
                .ToListAsync(cancellationToken);

            if (expiredTokens.Count == 0)
                return;

            foreach (var expiredToken in expiredTokens)
                expiredToken.IsDeleted = true;

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Se eliminaron {Count} refresh tokens expirados.", expiredTokens.Count);
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