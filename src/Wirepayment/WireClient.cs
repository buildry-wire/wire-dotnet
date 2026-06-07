using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Wire;

/// <summary>
/// WireClient is the entry point to the Wire payment API. Construct it with an API key
/// (<c>sk_live_...</c>) and optional <see cref="WireClientOptions"/>.
/// The API key is never logged nor included in any exception.
/// </summary>
public sealed class WireClient : IDisposable
{
    public const string DefaultBaseUrl = "https://api.wire.mn";

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly TimeSpan _timeout;
    private readonly int _maxRetries;
    private readonly TimeSpan _backoff;
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;

    public PaymentIntents PaymentIntents { get; }
    public Charges Charges { get; }
    public Events Events { get; }
    public WebhookEndpoints WebhookEndpoints { get; }

    public WireClient(string apiKey, WireClientOptions? options = null)
    {
        if (string.IsNullOrEmpty(apiKey))
            throw new ArgumentException("apiKey is required", nameof(apiKey));

        options ??= new WireClientOptions();
        _apiKey = apiKey;
        _baseUrl = (options.BaseUrl ?? DefaultBaseUrl).TrimEnd('/');
        _timeout = options.Timeout;
        _maxRetries = Math.Max(0, options.MaxRetries);
        _backoff = options.Backoff;

        if (options.HttpClient is not null)
        {
            _http = options.HttpClient;
            _ownsHttp = false;
        }
        else if (options.HttpMessageHandler is not null)
        {
            _http = new HttpClient(options.HttpMessageHandler, disposeHandler: false);
            _ownsHttp = true;
        }
        else
        {
            _http = new HttpClient();
            _ownsHttp = true;
        }

        PaymentIntents = new PaymentIntents(this);
        Charges = new Charges(this);
        Events = new Events(this);
        WebhookEndpoints = new WebhookEndpoints(this);
    }

    internal async Task<T> RequestAsync<T>(
        HttpMethod method,
        string path,
        object? body = null,
        IReadOnlyDictionary<string, string?>? query = null,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl(path, query);
        string? bodyJson = body is null ? null : JsonSerializer.Serialize(body, JsonOptions);

        // POST always carries an Idempotency-Key, reused across retries.
        string? idemKey = null;
        if (method == HttpMethod.Post)
            idemKey = idempotencyKey ?? NewIdempotencyKey();

        for (var attempt = 0; ; attempt++)
        {
            HttpResponseMessage? response = null;
            try
            {
                using var request = new HttpRequestMessage(method, url);
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_apiKey}");
                request.Headers.TryAddWithoutValidation("Accept", "application/json");
                if (idemKey is not null)
                    request.Headers.TryAddWithoutValidation("Idempotency-Key", idemKey);
                if (bodyJson is not null)
                    request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(_timeout);

                try
                {
                    response = await _http.SendAsync(request, timeoutCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    // Timeout (not a caller-initiated cancel): treat as a retryable network error.
                    if (attempt < _maxRetries)
                    {
                        await DelayAsync(null, attempt, cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                    throw new WireConnectionException("request timed out");
                }
            }
            catch (HttpRequestException e)
            {
                if (attempt < _maxRetries)
                {
                    await DelayAsync(null, attempt, cancellationToken).ConfigureAwait(false);
                    continue;
                }
                throw new WireConnectionException($"request failed: {e.Message}", e);
            }

            using (response)
            {
                var status = (int)response.StatusCode;

                if ((status == 429 || status >= 500) && attempt < _maxRetries)
                {
                    var retryAfter = ParseRetryAfter(response);
                    await DelayAsync(retryAfter, attempt, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    if (string.IsNullOrEmpty(text))
                        return JsonSerializer.Deserialize<T>("{}", JsonOptions)!;
                    return JsonSerializer.Deserialize<T>(text, JsonOptions)!;
                }

                throw WireException.Parse(status, text);
            }
        }
    }

    private string BuildUrl(string path, IReadOnlyDictionary<string, string?>? query)
    {
        var url = _baseUrl + path;
        if (query is null) return url;

        var parts = new List<string>();
        foreach (var kv in query)
        {
            if (string.IsNullOrEmpty(kv.Value)) continue;
            parts.Add($"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}");
        }
        if (parts.Count == 0) return url;
        return url + "?" + string.Join("&", parts);
    }

    private async Task DelayAsync(TimeSpan? retryAfter, int attempt, CancellationToken ct)
    {
        TimeSpan delay;
        if (retryAfter is { } ra && ra > TimeSpan.Zero)
        {
            delay = ra;
        }
        else
        {
            // Exponential backoff with full jitter.
            var baseMs = _backoff.TotalMilliseconds * Math.Pow(2, attempt);
            var jitter = RandomNumberGenerator.GetInt32(0, (int)Math.Max(1, baseMs) + 1);
            delay = TimeSpan.FromMilliseconds(jitter);
        }
        await Task.Delay(delay, ct).ConfigureAwait(false);
    }

    private static TimeSpan? ParseRetryAfter(HttpResponseMessage resp)
    {
        if (resp.Headers.RetryAfter is { } ra)
        {
            if (ra.Delta is { } d) return d;
            if (ra.Date is { } date)
            {
                var diff = date - DateTimeOffset.UtcNow;
                return diff > TimeSpan.Zero ? diff : TimeSpan.Zero;
            }
        }
        if (resp.Headers.TryGetValues("Retry-After", out var values))
        {
            foreach (var v in values)
            {
                if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var secs))
                    return TimeSpan.FromSeconds(secs);
            }
        }
        return null;
    }

    private static string NewIdempotencyKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(16);
        var sb = new StringBuilder("idk_", 4 + 32);
        foreach (var b in bytes)
            sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        return sb.ToString();
    }

    public void Dispose()
    {
        if (_ownsHttp)
            _http.Dispose();
    }
}
