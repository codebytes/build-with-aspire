using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BuildWithAspire.ApiService.Models;

public class Message
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    // Navigation property
    [JsonIgnore]
    public Conversation Conversation { get; set; } = null!;
}
