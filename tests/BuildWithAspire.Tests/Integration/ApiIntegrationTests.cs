using Aspire.Hosting.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BuildWithAspire.Tests.Integration;

public class ApiIntegrationTests : IClassFixture<AspireAppFixture>
{
    private readonly AspireAppFixture _fixture;

    public ApiIntegrationTests(AspireAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task WeatherForecast_ReturnsSuccessAndCorrectContentType()
    {
        // Act
        var response = await _fixture.ApiClient.GetAsync("/weatherforecast");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
        
        // Verify it's valid JSON array
        var forecasts = JsonSerializer.Deserialize<JsonElement[]>(content);
        Assert.NotNull(forecasts);
        Assert.NotEmpty(forecasts);
    }

    [Fact]
    public async Task Conversations_Get_ReturnsListSuccessfully()
    {
        // Act
        var response = await _fixture.ApiClient.GetAsync("/conversations");

        // Assert
        response.EnsureSuccessStatusCode();
        var conversations = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.NotNull(conversations);
        // Just verify we can get conversations - the list may or may not be empty depending on test isolation
    }

    [Fact]
    public async Task Conversations_CreateAndRetrieve_WorksCorrectly()
    {
        // Act - Create conversation with unique name to avoid test conflicts
        var uniqueName = $"Test Conversation {Guid.NewGuid()}";
        var createRequest = new { name = uniqueName };
        var createResponse = await _fixture.ApiClient.PostAsJsonAsync("/conversations", createRequest);
        
        // Assert - Creation successful
        createResponse.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        
        var createdConversation = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(createdConversation.TryGetProperty("id", out var idProperty));
        var conversationId = idProperty.GetGuid();
        Assert.NotEqual(Guid.Empty, conversationId);
        
        // Act - Retrieve conversation
        var getResponse = await _fixture.ApiClient.GetAsync($"/conversations/{conversationId}");
        
        // Assert - Retrieval successful
        getResponse.EnsureSuccessStatusCode();
        var retrievedConversation = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(retrievedConversation.TryGetProperty("name", out var nameProperty));
        Assert.Equal(uniqueName, nameProperty.GetString());
    }

    [Fact]
    public async Task Conversations_GetNonExistent_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _fixture.ApiClient.GetAsync($"/conversations/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Conversations_Delete_RemovesConversation()
    {
        // Create a conversation first with unique name
        var uniqueName = $"Test Conversation to Delete {Guid.NewGuid()}";
        var createRequest = new { name = uniqueName };
        var createResponse = await _fixture.ApiClient.PostAsJsonAsync("/conversations", createRequest);
        createResponse.EnsureSuccessStatusCode();
        
        var createdConversation = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var conversationId = createdConversation.GetProperty("id").GetGuid();

        // Act - Delete conversation
        var deleteResponse = await _fixture.ApiClient.DeleteAsync($"/conversations/{conversationId}");

        // Assert - Deletion successful
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        
        // Verify conversation no longer exists
        var getResponse = await _fixture.ApiClient.GetAsync($"/conversations/{conversationId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Conversations_CreateWithInvalidData_ReturnsBadRequest()
    {
        // Act - Try to create conversation with empty name
        var createRequest = new { name = "" };
        var response = await _fixture.ApiClient.PostAsJsonAsync("/conversations", createRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task HealthChecks_ReturnsHealthy()
    {
        // Act
        var response = await _fixture.ApiClient.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    // Note: Metrics endpoint testing is skipped as it may not be exposed in test environment
    // and depends on the specific OpenTelemetry configuration used in testing

    [Fact]
    public async Task Swagger_IsAvailableInDevelopment()
    {
        // Act
        var response = await _fixture.ApiClient.GetAsync("/swagger/index.html");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Swagger", content);
    }
}