using OpenAI.Chat;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureOpenAIClient("openai");

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/weatherforecast", (OpenAIClient client) =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
    {
        var temperature = Random.Shared.Next(-20, 55);
        var summary = GetWeatherSummary(client, temperature);
        return new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            temperature,
            summary
        );
    })
       .ToArray();
    return forecast;

    static string GetWeatherSummary(OpenAIClient client, int temp)
    {
        var chatClient = client.GetChatClient("chat");
        ChatCompletion completion = chatClient.CompleteChat(
            [
            // System messages represent instructions or other guidance about how the assistant should behave
            new SystemChatMessage("You are a helpful assistant that provides a description of the weather in one word based on the temperature."),
            // User messages represent user input, whether historical or the most recen tinput
            new UserChatMessage($"How would you describe the weather at temp {temp} in celcius? Provide the response in 1 word with no punctuation."),
            // Assistant messages in a request represent conversation history for responses
            ]
        );

        return $"{completion.Content[0].Text}";
    }
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapGet("/chat", (OpenAIClient client, string message) =>
{
    var chatClient = client.GetChatClient("chat");
    ChatCompletion completion = chatClient.CompleteChat(
        [
        // System messages represent instructions or other guidance about how the assistant should behave
        new SystemChatMessage("""
            You are an AI demonstration application. 
            You are a helpful chatbot. 
            Respond to the user' input responsibly.
            All responses should be safe for work.
            """),
        // User messages represent user input, whether historical or the most recen tinput
        new UserChatMessage(message),
        // Assistant messages in a request represent conversation history for responses
        ]
    );
    return completion.Content[0].Text ?? "No Response";
})
.WithName("Chat")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
