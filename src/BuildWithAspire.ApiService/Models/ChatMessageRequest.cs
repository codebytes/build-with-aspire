using System.ComponentModel.DataAnnotations;

namespace BuildWithAspire.ApiService.Models;

public class ChatMessageRequest
{
    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;
}
