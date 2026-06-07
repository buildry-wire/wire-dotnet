using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Wire.Tests;

/// <summary>A captured snapshot of an outgoing request.</summary>
public sealed record CapturedRequest(
    HttpMethod Method,
    string Path,
    string Query,
    IReadOnlyDictionary<string, string> Headers,
    string Body);

/// <summary>The reply a stub should return for a given attempt.</summary>
public sealed record StubResponse(int Status, string Json, IReadOnlyDictionary<string, string>? Headers = null);

/// <summary>
/// A test <see cref="HttpMessageHandler"/> that records each request and returns scripted
/// responses via a handler delegate. The delegate receives the 0-based attempt number.
/// </summary>
public sealed class StubHandler : HttpMessageHandler
{
    private readonly Func<CapturedRequest, int, StubResponse> _handler;
    public List<CapturedRequest> Requests { get; } = new();

    public StubHandler(Func<CapturedRequest, int, StubResponse> handler) => _handler = handler;

    public StubHandler(Func<CapturedRequest, StubResponse> handler)
        : this((req, _) => handler(req)) { }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var attempt = Requests.Count;

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var h in request.Headers)
            headers[h.Key] = string.Join(",", h.Value);

        var body = "";
        if (request.Content is not null)
        {
            foreach (var h in request.Content.Headers)
                headers[h.Key] = string.Join(",", h.Value);
            body = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        var uri = request.RequestUri!;
        var captured = new CapturedRequest(request.Method, uri.AbsolutePath, uri.Query, headers, body);
        Requests.Add(captured);

        var reply = _handler(captured, attempt);
        var response = new HttpResponseMessage((HttpStatusCode)reply.Status)
        {
            Content = new StringContent(reply.Json, System.Text.Encoding.UTF8, "application/json"),
        };
        if (reply.Headers is not null)
        {
            foreach (var kv in reply.Headers)
                response.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
        }
        return response;
    }
}
