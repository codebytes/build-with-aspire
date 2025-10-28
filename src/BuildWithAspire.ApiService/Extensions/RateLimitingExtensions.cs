using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace BuildWithAspire.ApiService.Extensions;

/// <summary>
/// Extensions for configuring rate limiting.
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Adds rate limiting policies for API endpoints.
    /// </summary>
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter("chat", limiterOptions =>
            {
                limiterOptions.PermitLimit = 10;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 2;
            });

            options.AddFixedWindowLimiter("weather", limiterOptions =>
            {
                limiterOptions.PermitLimit = 20;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 5;
            });
        });
    }
}
