namespace BuildWithAspire.Web.Clients;

public class ChatApiClient(HttpClient httpClient)
{
    public async Task<string> GetChatAsync(string message, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"/chat?message={message}", cancellationToken);
        var chatResponse = response.IsSuccessStatusCode
            ? await response.Content.ReadAsStringAsync(cancellationToken)
            : throw new Exception(response.ReasonPhrase);
        return chatResponse ?? "No Response";
    }
}
