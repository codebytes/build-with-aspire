namespace BuildWithAspire.ApiService.Models;

public class Conversation
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation property
    public List<Message> Messages { get; set; } = new();
}