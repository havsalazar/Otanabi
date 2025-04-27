using System.Net;
using System.Reflection;
using System.Text;

namespace Otanabi.Core.Services;

public sealed class HttpService
{
    private readonly HttpClient _client;

    public HttpService()
    {
        var handler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All };

        _client = new HttpClient();
        var name = Assembly.GetExecutingAssembly().GetName().Name;
        _client.DefaultRequestHeaders.UserAgent.ParseAdd(name);
    }

    public HttpClient GetHttpClient()
    {
        return _client;
    }

    public async Task<string> GetAsync(string uri)
    {
        using var response = await _client.GetAsync(uri);

        return await response.Content.ReadAsStringAsync();
    }

    public async Task<HttpResponseMessage> GetAsyncResponse(string uri)
    {
        using var response = await _client.GetAsync(uri);

        return response;
    }

    public async Task<string> PostAsync(string uri, string data, string contentType)
    {
        using HttpContent content = new StringContent(data, Encoding.UTF8, contentType);

        var requestMessage = new HttpRequestMessage()
        {
            Content = content,
            Method = HttpMethod.Post,
            RequestUri = new Uri(uri),
        };

        using var response = await _client.SendAsync(requestMessage);

        return await response.Content.ReadAsStringAsync();
    }
}
