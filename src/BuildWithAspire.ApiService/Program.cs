using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var aiType = builder.Configuration["AI:Type"] ?? "ollama";
var chatDeploymentName = builder.Configuration["AI:ChatDeploymentName"] ?? "chat";

switch (aiType.ToLower())
{
    case "ollama":
        builder.AddOllamaApiClient(chatDeploymentName)
            .AddChatClient();
        break;
    case "azureopenai":
        builder.AddAzureOpenAIClient(chatDeploymentName)
            .AddChatClient(chatDeploymentName);
        break;
    case "github":
        builder.Services.AddGitHubAIClient(options => {
            options.Endpoint = new Uri(builder.Configuration["AI:GitHub:BaseUrl"] ?? "https://api.github.com");
            options.ModelId = builder.Configuration["AI:GitHub:ModelId"] ?? "codify";
            // Add auth if needed
            if (!string.IsNullOrEmpty(builder.Configuration["AI:GitHub:ApiKey"])) {
                options.AuthToken = builder.Configuration["AI:GitHub:ApiKey"];
            }
        }).AddChatClient();
        break;
    case "foundry":
        builder.Services.AddFoundryAIClient(options => {
            options.Endpoint = new Uri(builder.Configuration["AI:Foundry:BaseUrl"] ?? "http://localhost:8080");
            options.ModelId = builder.Configuration["AI:Foundry:ModelId"] ?? "llama3";
            // Add auth if needed
            if (!string.IsNullOrEmpty(builder.Configuration["AI:Foundry:ApiKey"])) {
                options.ApiKey = builder.Configuration["AI:Foundry:ApiKey"];
            }
        }).AddChatClient();
        break;
    default:
        throw new InvalidOperationException($"Unsupported AI type: {aiType}");
}

builder.Services.AddKernel();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<ChatService>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/weatherforecast", (IChatClient client) =>
{
    async IAsyncEnumerable<WeatherForecast> GetForecasts()
    {
        for (int index = 1; index <= 5; index++)
        {
            var temperature = Random.Shared.Next(-20, 55);
            var summary = await GetWeatherSummary(client, temperature);
            yield return new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                temperature,
                summary
            );
        }
    }

    return GetForecasts();

    static async Task<string> GetWeatherSummary(IChatClient client, int temp)
    {
        List<ChatMessage> conversation = new()
        {
             // System messages represent instructions or other guidance about how the assistant should behave
            new(ChatRole.System, "You are a helpful assistant that provides a description of the weather in one word based on the temperature."),
            // User messages represent user input, whether historical or the most recent input
            new(ChatRole.User, $"How would you describe the weather at temp {temp} in celcius? Provide the response in 1 word with no punctuation.")
        };
        var completion = await client.GetResponseAsync(conversation);

        return $"{completion.Text}";
    }
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapGet("/chat", async (ChatService chatService, string message) => await chatService.ProcessMessage(message))
.WithName("Chat")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
