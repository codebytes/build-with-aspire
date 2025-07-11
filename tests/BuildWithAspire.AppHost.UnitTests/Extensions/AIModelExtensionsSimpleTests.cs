namespace BuildWithAspire.AppHost.UnitTests.Extensions;

public class AIModelExtensionsSimpleTests
{
    [Fact]
    public void GitHubModelsResource_Constructor_SetsProperties()
    {
        // Arrange
        const string name = "test-github-models";
        const string model = "gpt-4o-mini";

        // Act
        var resource = new GitHubModelsResource(name, model);

        // Assert
        Assert.Equal(name, resource.Name);
        Assert.Equal(model, resource.Model);
        Assert.Null(resource.Key);
    }

    [Fact]
    public void FoundryLocalResource_Constructor_SetsProperties()
    {
        // Arrange
        const string name = "test-foundry-local";
        const string model = "phi-3.5-mini";

        // Act
        var resource = new FoundryLocalResource(name, model);

        // Assert
        Assert.Equal(name, resource.Name);
        Assert.Equal(model, resource.Model);
        Assert.Equal("http://localhost:8000", resource.Endpoint);
        Assert.Equal("/tmp/foundry-local-models", resource.ModelCachePath);
        Assert.True(resource.AutoStart);
    }

    [Fact]
    public void GitHubModelsResource_ConnectionString_ContainsExpectedValues()
    {
        // Arrange
        const string name = "test-resource";
        const string model = "test-model";
        const string testToken = "test-token";

        Environment.SetEnvironmentVariable("GITHUB_TOKEN", testToken);

        try
        {
            var resource = new GitHubModelsResource(name, model);

            // Act
            var connectionString = resource.ConnectionStringExpression.ValueExpression;

            // Assert
            Assert.Contains("Endpoint=https://models.inference.ai.azure.com", connectionString);
            Assert.Contains($"Model={model}", connectionString);
            Assert.Contains($"DeploymentId={model}", connectionString);
            Assert.Contains($"Key={testToken}", connectionString);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_TOKEN", null);
        }
    }

    [Fact]
    public void FoundryLocalResource_ConnectionString_ContainsExpectedValues()
    {
        // Arrange
        const string name = "test-resource";
        const string model = "test-model";
        var resource = new FoundryLocalResource(name, model);

        // Act
        var connectionString = resource.ConnectionStringExpression.ValueExpression;

        // Assert
        Assert.Contains("Provider=FoundryLocal", connectionString);
        Assert.Contains($"Model={model}", connectionString);
        Assert.Contains("Endpoint=http://localhost:8000", connectionString);
        Assert.Contains("AutoStart=true", connectionString);
        Assert.Contains("ModelCachePath=/tmp/foundry-local-models", connectionString);
    }

    [Fact]
    public void FoundryLocalResource_WithEndpoint_UpdatesEndpoint()
    {
        // Arrange
        const string name = "test-resource";
        const string model = "test-model";
        const string customEndpoint = "http://localhost:9000";
        var resource = new FoundryLocalResource(name, model);

        // Act
        resource.Endpoint = customEndpoint;

        // Assert
        Assert.Equal(customEndpoint, resource.Endpoint);
    }

    [Fact]
    public void FoundryLocalResource_WithAutoStart_UpdatesAutoStart()
    {
        // Arrange
        const string name = "test-resource";
        const string model = "test-model";
        var resource = new FoundryLocalResource(name, model);

        // Act
        resource.AutoStart = false;

        // Assert
        Assert.False(resource.AutoStart);
    }

    [Fact]
    public void FoundryLocalResource_WithModelCachePath_UpdatesCachePath()
    {
        // Arrange
        const string name = "test-resource";
        const string model = "test-model";
        const string customCachePath = "/custom/cache/path";
        var resource = new FoundryLocalResource(name, model);

        // Act
        resource.ModelCachePath = customCachePath;

        // Assert
        Assert.Equal(customCachePath, resource.ModelCachePath);
    }

}
