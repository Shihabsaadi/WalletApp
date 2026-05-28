using Google.Apis.Http;

namespace WalletApp.Services;

public class CustomHttpClientFactory : IHttpClientFactory
{
    public ConfigurableHttpClient CreateHttpClient(CreateHttpClientArgs args)
    {
        // Create a handler with compression disabled
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.None
        };

        // ConfigurableMessageHandler wraps our handler — required by Google API client
        var configurableHandler = new ConfigurableMessageHandler(handler)
        {
            // Disable GZip from Google's side too
            IsLoggingEnabled = false
        };

        var client = new ConfigurableHttpClient(configurableHandler);

        // Clear accept-encoding so Google server sends uncompressed response
        client.DefaultRequestHeaders.AcceptEncoding.Clear();

        return client;
    }
}