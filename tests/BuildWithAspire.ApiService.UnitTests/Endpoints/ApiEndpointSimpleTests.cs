using System.Net;
using System.Text;
using System.Text.Json;
using BuildWithAspire.ApiService.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace BuildWithAspire.ApiService.UnitTests.Endpoints;

public class ApiEndpointSimpleTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _databaseName = $"TestDb_{Guid.NewGuid()}";

    public ApiEndpointSimpleTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            // Clear configuration to avoid Aspire connection string requirements
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.Sources.Clear();
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:chatdb"] = "Host=localhost;Database=test;Username=test;Password=test",
                    ["AI:Provider"] = "Ollama",
                    ["AI:DeploymentName"] = "test",
                    ["AI:Model"] = "test-model"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Remove all DbContext and related registrations
                var descriptorsToRemove = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ChatDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(ChatDbContext) ||
                    d.ImplementationType?.Name.Contains("ChatDbContext") == true ||
                    d.ServiceType.Name.Contains("ChatDbContext") ||
                    d.ServiceType.Name.Contains("DbContext") ||
                    d.ImplementationType?.Name.Contains("PostgreSQL") == true ||
                    d.ImplementationType?.Name.Contains("Npgsql") == true).ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                // Add standard EF Core in-memory database with shared name
                services.AddDbContext<ChatDbContext>(options =>
                {
                    options.UseInMemoryDatabase(databaseName: _databaseName);
                });

                // Mock IChatClient - simplified
                var mockChatClient = Substitute.For<IChatClient>();

                // Mock AIConfiguration.AISettings
                var aiSettings = new AIConfiguration.AISettings(AIConfiguration.AIProvider.Ollama, "test", "test-model", 120);
                services.AddSingleton(aiSettings);

                services.AddSingleton(mockChatClient);
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetConversations_EmptyDatabase_ReturnsEmptyArray()
    {
        // Act
        var response = await _client.GetAsync("/conversations");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var conversations = JsonSerializer.Deserialize<object[]>(content);
        Assert.NotNull(conversations);
        Assert.Empty(conversations);
    }

    [Fact]
    public async Task CreateConversation_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new { Name = "Test Conversation" };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/conversations", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var conversation = JsonSerializer.Deserialize<JsonElement>(responseContent);
        Assert.Equal("Test Conversation", conversation.GetProperty("name").GetString());
    }

    [Fact]
    public async Task CreateConversation_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new { Name = "" };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/conversations", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetConversation_ValidId_ReturnsConversation()
    {
        // Arrange - Create a conversation first
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Name = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/conversations/{conversation.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.Equal("Test Conversation", result.GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetConversation_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/conversations/{invalidId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteConversation_ValidId_ReturnsNoContent()
    {
        // Arrange - Create a conversation first
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Name = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await _client.DeleteAsync($"/conversations/{conversation.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteConversation_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/conversations/{invalidId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetConversations_WithData_ReturnsConversationsOrderedByUpdatedAt()
    {
        // Arrange - Create multiple conversations
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();

        var conversation1 = new Conversation
        {
            Id = Guid.NewGuid(),
            Name = "First Conversation",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var conversation2 = new Conversation
        {
            Id = Guid.NewGuid(),
            Name = "Second Conversation",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        dbContext.Conversations.AddRange(conversation1, conversation2);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/conversations");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var conversations = JsonSerializer.Deserialize<JsonElement[]>(content);
        Assert.NotNull(conversations);
        Assert.Equal(2, conversations.Length);

        // Should be ordered by UpdatedAt descending
        Assert.Equal("Second Conversation", conversations[0].GetProperty("name").GetString());
        Assert.Equal("First Conversation", conversations[1].GetProperty("name").GetString());
    }
}
