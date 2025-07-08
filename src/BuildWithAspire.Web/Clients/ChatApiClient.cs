// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BuildWithAspire.Web.Clients;

public class ChatApiClient(HttpClient httpClient)
{
    public async Task<string> GetChatAsync(string message, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"?message={message}", cancellationToken).ConfigureAwait(false);
        var chatResponse = response.IsSuccessStatusCode
            ? await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)
            : throw new HttpRequestException(response.ReasonPhrase);
        return chatResponse ?? "No Response";
    }
}
