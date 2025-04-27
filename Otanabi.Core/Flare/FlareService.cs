using Newtonsoft.Json;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

namespace Otanabi.Core.Flare;

public class FlareService
{
    internal string URL_FLARE = "http://localhost:8191";
    internal string URL_VERSION = "v1";
    internal string GetFlareUrl => string.Concat(URL_FLARE, "/", URL_VERSION);

    private readonly RestClient _client;

    public FlareService()
    {
        var options = new RestClientOptions(GetFlareUrl);
        _client = (RestClient)
            new RestClient(options, configureSerialization: s => s.UseNewtonsoftJson()).AddDefaultHeader(
                KnownHeaders.ContentType,
                "application/json"
            );
    }

    public async Task<Session> CreateFlareSession()
    {
        var request = sameRequester(new { cmd = "sessions.create", session = "AnimeScrapper" });
        var response = await _client.PostAsync(request);
        var sessionCreated = JsonConvert.DeserializeObject<SesionCreated>(response.Content);
        return new Session { session = sessionCreated.session };
    }

    internal RestRequest sameRequester(object body)
    {
        var request = new RestRequest("");
        request.AddHeader("content-type", "application/json");
        if (body != null)
            request.AddBody(body);

        return request;
    }

    public async Task<List<string>> GetSessionsList()
    {
        var request = sameRequester(new { cmd = "sessions.list" });

        var response = await _client.PostAsync(request);
        var content = JsonConvert.DeserializeObject<SessionListResp>(response.Content);
        var sessions = content.sessions;

        return sessions;
    }

    public async Task<Session> GetOrCreateSession()
    {
        var session = new Session();
        var sessions = await GetSessionsList();
        if (sessions.Count == 0)
        {
            session = await CreateFlareSession();
        }
        else
        {
            session.session = sessions[0];
        }
        return session;
    }

    public async Task<Solution> GetRequest(string url)
    {
        var flaverSession = await GetOrCreateSession();
        var request = sameRequester(
            new
            {
                cmd = "request.get",
                flaverSession.session,
                url,
            }
        );

        var response = await _client.PostAsync(request);
        var content = JsonConvert.DeserializeObject<GetResponse>(response.Content);
        return content.solution;
    }

    public async Task<object> GetCookiesData(string url)
    {
        var flaverSession = await GetOrCreateSession();
        var request = sameRequester(
            new
            {
                cmd = "request.get",
                flaverSession.session,
                url,
            }
        );

        var response = await _client.PostAsync(request);
        return response.Content;
    }
}
