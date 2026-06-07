using System;
using System.Net.Http;

namespace Wire;

/// <summary>Configuration for <see cref="WireClient"/>.</summary>
public sealed class WireClientOptions
{
    /// <summary>API base URL. Default <c>https://api.wire.mn</c>.</summary>
    public string BaseUrl { get; set; } = WireClient.DefaultBaseUrl;

    /// <summary>Per-request timeout. Default 30 seconds.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Maximum retries on 429/5xx/network errors. Default 2.</summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>Base backoff before jitter and exponential growth. Default 500ms.</summary>
    public TimeSpan Backoff { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Optional pre-built <see cref="HttpClient"/> to use. If supplied, the caller owns its
    /// lifetime and disposing the <see cref="WireClient"/> will not dispose it.
    /// </summary>
    public HttpClient? HttpClient { get; set; }

    /// <summary>
    /// Optional message handler. Ignored when <see cref="HttpClient"/> is set. When supplied,
    /// the <see cref="WireClient"/> builds an internal <see cref="HttpClient"/> on top of it.
    /// </summary>
    public HttpMessageHandler? HttpMessageHandler { get; set; }
}
