using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Otanabi.Core.Anilist.Enums;

namespace Otanabi.Core.Anilist;

public sealed class AnilistClient
{
    //singleton

    private static readonly Lazy<AnilistClient> _instance = new Lazy<AnilistClient>(
        () => new AnilistClient(),
        LazyThreadSafetyMode.ExecutionAndPublication
    );
    public static AnilistClient Instance => _instance.Value;

    private readonly HttpClient _httpClient;
    private readonly string _graphqlEndpoint = "https://graphql.anilist.co";

    private readonly ConcurrentQueue<Func<Task>> _requestQueue;
    private readonly SemaphoreSlim _rateLimitSemaphore;
    private DateTime _rateLimitResetTime = DateTime.MinValue;
    private int _rateLimitRemaining = int.MinValue;

    private const int MaxRetryAttempts = 3;
    private static readonly TimeSpan[] RetryDelays = new[]
    {
        TimeSpan.FromSeconds(1), // First retry after 1 second
        TimeSpan.FromSeconds(5), // Second retry after 5 seconds
        TimeSpan.FromSeconds(10), // Final retry after 10 seconds
    };

    private AnilistClient()
    {
        _httpClient = new HttpClient();
        _requestQueue = new ConcurrentQueue<Func<Task>>();
        _rateLimitSemaphore = new SemaphoreSlim(1, 1);
        StartQueueProcessor();
    }

    private void StartQueueProcessor()
    {
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await ProcessQueueAsync();
                await Task.Delay(100);
            }
        });
    }

    private async Task ProcessQueueAsync()
    {
        await _rateLimitSemaphore.WaitAsync();
        try
        {
            if (DateTime.UtcNow < _rateLimitResetTime)
            {
                return;
            }
            if (_requestQueue.TryDequeue(out var request))
            {
                await request();
            }
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    public async Task<JObject> SendQueryAsync(
        string query,
        object variables,
        CancellationToken cancellationToken = default
    )
    {
        for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
        {
            try
            {
                var variablesJson = JsonConvert.SerializeObject(variables);
                var requestBody = new { query, variables = JsonConvert.DeserializeObject<object>(variablesJson) };
                var response = await MakeRequest(requestBody);
                return response;
            }
            catch (RateLimitException ex)
            {
                if (attempt == MaxRetryAttempts - 1)
                {
                    throw;
                }
                var delay = GetRetryDelay(attempt, ex);

                System.Diagnostics.Debug.WriteLine(
                    $"Rate limit hit. Attempt {attempt + 1}. Waiting {delay.TotalSeconds} seconds."
                );

                // Wait before retrying
                await Task.Delay(delay, cancellationToken);
            }
        }
        throw new InvalidOperationException("Unexpected retry failure");
    }

    public async Task<JObject> SendQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
        {
            try
            {
                var requestBody = new { query };
                var response = await MakeRequest(requestBody);
                return response;
            }
            catch (RateLimitException ex)
            {
                if (attempt == MaxRetryAttempts - 1)
                {
                    throw;
                }
                var delay = GetRetryDelay(attempt, ex);

                System.Diagnostics.Debug.WriteLine(
                    $"Rate limit hit. Attempt {attempt + 1}. Waiting {delay.TotalSeconds} seconds."
                );

                // Wait before retrying
                await Task.Delay(delay, cancellationToken);
            }
        }
        throw new InvalidOperationException("Unexpected retry failure");
    }

    private async Task<JObject> MakeRequest(object requestBody, CancellationToken cancellationToken = default)
    {
        var tsc = new TaskCompletionSource<JObject>();
        _requestQueue.Enqueue(async () =>
        {
            try
            {
                var jsonRequest = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_graphqlEndpoint, content, cancellationToken);

                var rlLimit = GetHeaderValue(response.Headers, "X-RateLimit-Limit");
                var rlRemaining = GetHeaderValue(response.Headers, "X-RateLimit-Remaining");

                if (int.TryParse(rlLimit, out int remaining))
                {
                    _rateLimitRemaining = remaining;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    var retryAfter = GetHeaderValue(response.Headers, "Retry-After");
                    var rateLimitReset = GetHeaderValue(response.Headers, "X-RateLimit-Reset");
                    if (long.TryParse(rateLimitReset, out long resetTimestamp))
                    {
                        _rateLimitResetTime = DateTimeOffset.FromUnixTimeSeconds(resetTimestamp).DateTime;
                    }
                    else if (int.TryParse(retryAfter, out int retrySeconds))
                    {
                        _rateLimitResetTime = DateTime.UtcNow.AddSeconds(retrySeconds);
                    }
                    else
                    {
                        _rateLimitResetTime = DateTime.UtcNow.AddMinutes(1);
                    }

                    throw new HttpRequestException("Rate limit exceeded. Retry after specified time.");
                }
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var jsonObjectResponse = JObject.Parse(jsonResponse);

                tsc.TrySetResult(jsonObjectResponse);
            }
            catch (Exception ex)
            {
                tsc.TrySetException(ex);
            }
        });

        return await tsc.Task;
        //var jsonRequest = JsonConvert.SerializeObject(requestBody);
        //var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        //var response = await _httpClient.PostAsync(_graphqlEndpoint, content);
        //response.EnsureSuccessStatusCode();

        //var jsonResponse = await response.Content.ReadAsStringAsync();
        //var jsonObjectResponse = JObject.Parse(jsonResponse);
        //return jsonObjectResponse;
    }

    public string GetQuery(QueryType queryType)
    {
        try
        {
            var queryName = queryType switch
            {
                QueryType.Search => "SearchQuery",
                QueryType.Seasonal => "Seasonal",
                QueryType.SeasonalFullDetail => "SeasonalFullDetail",
                QueryType.ById => "ById",
                QueryType.ByIdFullDetail => "ByIdFullDetail",
                QueryType.GetTags => "GetTags",
                QueryType.ByName => "ByName",
                _ => "SearchQuery",
            };

            var currPath = Assembly.GetExecutingAssembly().Location;
            var queryPath = Path.Combine(Path.GetDirectoryName(currPath), "Anilist", "Queries", $"{queryName}.graphql");
            return File.ReadAllText(queryPath);
        }
        catch (Exception e)
        {
            return "";
        }
    }

    private TimeSpan GetRetryDelay(int attempt, RateLimitException ex)
    {
        // Priority 1: Use Retry-After header if available
        if (ex.RetryAfter.HasValue)
            return ex.RetryAfter.Value;

        // Priority 2: Use predefined retry delays
        if (attempt < RetryDelays.Length)
            return RetryDelays[attempt];

        // Fallback: Exponential backoff
        return TimeSpan.FromSeconds(Math.Pow(2, attempt));
    }

    private bool IsRateLimitError(HttpRequestException ex)
    {
        // Check if the exception indicates a rate limit error
        return ex.Message.Contains("Rate limit exceeded")
            || (
                ex.InnerException is HttpRequestException innerEx
                && innerEx.StatusCode == System.Net.HttpStatusCode.TooManyRequests
            );
    }

    private string GetHeaderValue(System.Net.Http.Headers.HttpResponseHeaders headers, string headerName)
    {
        return headers.TryGetValues(headerName, out var values) ? values.FirstOrDefault() : null;
    }

    private class RateLimitException : Exception
    {
        public TimeSpan? RetryAfter { get; }
        public string RateLimitLimit { get; }
        public string RateLimitRemaining { get; }
        public string RateLimitReset { get; }

        public RateLimitException(
            string message,
            TimeSpan? retryAfter = null,
            string rateLimitLimit = null,
            string rateLimitRemaining = null,
            string rateLimitReset = null
        )
            : base(message)
        {
            RetryAfter = retryAfter;
            RateLimitLimit = rateLimitLimit;
            RateLimitRemaining = rateLimitRemaining;
            RateLimitReset = rateLimitReset;
        }
    }
}
