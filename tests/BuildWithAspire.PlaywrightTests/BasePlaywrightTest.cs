using Aspire.Hosting.Testing;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace BuildWithAspire.PlaywrightTests;

/// <summary>
/// Base class for Playwright tests that integrates with Aspire distributed application testing.
/// Provides setup and teardown for the Aspire app and Playwright browser instances.
/// </summary>
public abstract class BasePlaywrightTest : IAsyncLifetime
{
    private static IAsyncDisposable? _aspireApp;
    private static string _webAppBaseUrl = string.Empty;
    private static IBrowser? _browser;
    private static readonly object _lock = new();
    private static bool _isInitialized = false;

    protected IPage Page { get; private set; } = null!;
    protected string WebAppBaseUrl => _webAppBaseUrl;

    public virtual async Task InitializeAsync()
    {
        await InitializeSharedResourcesAsync();
        await InitializePageAsync();
    }

    private static async Task InitializeSharedResourcesAsync()
    {
        lock (_lock)
        {
            if (_isInitialized) return;
            _isInitialized = true;
        }

        // Start the Aspire distributed application
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.BuildWithAspire_AppHost>();
        var app = await builder.BuildAsync();
        await app.StartAsync();
        _aspireApp = app;

        // Get the web frontend URL
        var httpClient = app.CreateHttpClient("webfrontend");
        _webAppBaseUrl = httpClient.BaseAddress?.ToString() ?? throw new InvalidOperationException("Failed to get web app base URL");
        
        // Remove trailing slash
        _webAppBaseUrl = _webAppBaseUrl.TrimEnd('/');

        // Initialize Playwright browser
        var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    private async Task InitializePageAsync()
    {
        if (_browser == null)
            throw new InvalidOperationException("Browser not initialized");

        Page = await _browser.NewPageAsync();
        
        // Navigate to the app before each test
        await Page.GotoAsync(_webAppBaseUrl);
        
        // Wait for Blazor to initialize
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task DisposeAsync()
    {
        if (Page != null)
        {
            await Page.CloseAsync();
        }
    }

    /// <summary>
    /// Helper method to wait for Blazor SignalR connection to be established
    /// </summary>
    protected async Task WaitForBlazorConnectionAsync()
    {
        // Wait for the Blazor connection indicator or a specific element that appears when connected
        await Page.WaitForFunctionAsync("() => window.Blazor && window.Blazor.reconnect");
    }

    /// <summary>
    /// Helper method to navigate to a specific page and wait for it to load
    /// </summary>
    protected async Task NavigateToPageAsync(string path)
    {
        await Page.GotoAsync($"{WebAppBaseUrl}{path}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await WaitForBlazorConnectionAsync();
    }

    /// <summary>
    /// Static cleanup method to dispose shared resources
    /// </summary>
    public static async Task DisposeSharedResourcesAsync()
    {
        if (_browser != null)
        {
            await _browser.DisposeAsync();
        }
        
        if (_aspireApp != null)
        {
            await _aspireApp.DisposeAsync();
        }
    }
}