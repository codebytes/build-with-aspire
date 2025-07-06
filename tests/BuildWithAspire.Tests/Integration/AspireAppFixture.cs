using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace BuildWithAspire.Tests.Integration;

/// <summary>
/// Shared fixture for Aspire app testing that starts the application once and reuses it across all tests.
/// This significantly improves test performance by avoiding repeated app startup costs.
/// Uses the same configuration as the AppHost but in a testing context.
/// </summary>
public class AspireAppFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    public HttpClient ApiClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Create the builder using the same AppHost configuration
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.BuildWithAspire_AppHost>();
        
        // The DistributedApplicationTestingBuilder automatically configures the app for testing
        // It will use test-friendly versions of the resources (e.g., containers without persistent volumes)
        
        _app = await builder.BuildAsync();
        await _app.StartAsync();
        
        ApiClient = _app.CreateHttpClient("apiservice");
    }

    public async Task DisposeAsync()
    {
        ApiClient?.Dispose();
        if (_app != null)
        {
            await _app.DisposeAsync();
        }
    }
}
