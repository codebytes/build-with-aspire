using BuildWithAspire.ApiService.Data;
using Microsoft.EntityFrameworkCore;

namespace BuildWithAspire.ApiService.Extensions;

/// <summary>
/// Extensions for database configuration and migrations.
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Applies pending database migrations on startup.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        try
        {
            app.Logger.LogInformation("Applying database migrations");
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
            var startTime = DateTime.UtcNow;

            var canConnect = await dbContext.Database.CanConnectAsync().ConfigureAwait(false);
            if (!canConnect)
            {
                app.Logger.LogWarning("Cannot connect to database. Skipping migrations");
                return;
            }

            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync().ConfigureAwait(false);
            var pendingCount = pendingMigrations.Count();

            if (pendingCount > 0)
            {
                app.Logger.LogInformation("Applying {Count} pending migrations", pendingCount);
                await dbContext.Database.MigrateAsync().ConfigureAwait(false);
                var duration = DateTime.UtcNow - startTime;
                app.Logger.LogInformation("Migrations applied. Duration: {Duration}ms", duration.TotalMilliseconds);
            }
            else
            {
                var duration = DateTime.UtcNow - startTime;
                app.Logger.LogInformation("Database up to date. Duration: {Duration}ms", duration.TotalMilliseconds);
            }
        }
        catch (Npgsql.PostgresException pgEx) when (pgEx.SqlState == "42P07")
        {
            app.Logger.LogInformation("Database tables already exist");
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Database migration encountered an issue. Service will continue");
        }
    }
}
