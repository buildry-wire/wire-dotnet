using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Wire;

internal static class Pagination
{
    /// <summary>Yields every item across pages, following has_more via starting_after.</summary>
    public static async IAsyncEnumerable<T> Paginate<T>(
        WireClient client,
        string path,
        Func<T, string> idOf,
        ListParams? parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken)
        where T : class
    {
        var after = parameters?.StartingAfter;
        var endingBefore = parameters?.EndingBefore;
        var limit = parameters?.Limit;

        while (true)
        {
            var query = new Dictionary<string, string?>();
            if (limit is { } l && l > 0)
                query["limit"] = l.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(after))
                query["starting_after"] = after;
            if (!string.IsNullOrEmpty(endingBefore))
                query["ending_before"] = endingBefore;

            var page = await client.RequestAsync<WireList<T>>(
                HttpMethod.Get, path, query: query, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (page.Data.Count == 0)
                yield break;

            foreach (var item in page.Data)
            {
                after = idOf(item);
                yield return item;
            }

            // ending_before only applies to the first page request.
            endingBefore = null;

            if (!page.HasMore)
                yield break;
        }
    }
}

/// <summary>PaymentIntents resource.</summary>
public sealed class PaymentIntents
{
    private readonly WireClient _client;
    internal PaymentIntents(WireClient client) => _client = client;

    public Task<PaymentIntent> CreateAsync(PaymentIntentCreateParams p, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, object?>
        {
            ["amount"] = p.Amount,
            ["currency"] = p.Currency,
            ["automatic_operator"] = p.AutomaticOperator,
            ["allowed_operators"] = p.AllowedOperators,
            ["metadata"] = p.Metadata,
        };
        return _client.RequestAsync<PaymentIntent>(
            HttpMethod.Post, "/v1/payment_intents", body: body,
            idempotencyKey: p.IdempotencyKey, cancellationToken: cancellationToken);
    }

    public Task<PaymentIntent> RetrieveAsync(string id, CancellationToken cancellationToken = default) =>
        _client.RequestAsync<PaymentIntent>(
            HttpMethod.Get, $"/v1/payment_intents/{Uri.EscapeDataString(id)}",
            cancellationToken: cancellationToken);

    public Task<PaymentIntent> ConfirmAsync(string id, PaymentIntentConfirmParams? p = null, CancellationToken cancellationToken = default)
    {
        p ??= new PaymentIntentConfirmParams();
        var body = new Dictionary<string, object?> { ["return_url"] = p.ReturnUrl };
        return _client.RequestAsync<PaymentIntent>(
            HttpMethod.Post, $"/v1/payment_intents/{Uri.EscapeDataString(id)}/confirm",
            body: body, idempotencyKey: p.IdempotencyKey, cancellationToken: cancellationToken);
    }

    public Task<PaymentIntent> CancelAsync(string id, CancellationToken cancellationToken = default) =>
        _client.RequestAsync<PaymentIntent>(
            HttpMethod.Post, $"/v1/payment_intents/{Uri.EscapeDataString(id)}/cancel",
            body: new Dictionary<string, object?>(), cancellationToken: cancellationToken);

    public IAsyncEnumerable<PaymentIntent> ListAsync(ListParams? parameters = null, CancellationToken cancellationToken = default) =>
        Pagination.Paginate<PaymentIntent>(_client, "/v1/payment_intents", x => x.Id, parameters, cancellationToken);
}

/// <summary>Charges resource.</summary>
public sealed class Charges
{
    private readonly WireClient _client;
    internal Charges(WireClient client) => _client = client;

    public Task<Charge> RetrieveAsync(string id, CancellationToken cancellationToken = default) =>
        _client.RequestAsync<Charge>(
            HttpMethod.Get, $"/v1/charges/{Uri.EscapeDataString(id)}",
            cancellationToken: cancellationToken);

    public IAsyncEnumerable<Charge> ListAsync(ListParams? parameters = null, CancellationToken cancellationToken = default) =>
        Pagination.Paginate<Charge>(_client, "/v1/charges", x => x.Id, parameters, cancellationToken);
}

/// <summary>Events resource.</summary>
public sealed class Events
{
    private readonly WireClient _client;
    internal Events(WireClient client) => _client = client;

    public Task<WireEvent> RetrieveAsync(string id, CancellationToken cancellationToken = default) =>
        _client.RequestAsync<WireEvent>(
            HttpMethod.Get, $"/v1/events/{Uri.EscapeDataString(id)}",
            cancellationToken: cancellationToken);

    public IAsyncEnumerable<WireEvent> ListAsync(ListParams? parameters = null, CancellationToken cancellationToken = default) =>
        Pagination.Paginate<WireEvent>(_client, "/v1/events", x => x.Id, parameters, cancellationToken);
}

/// <summary>WebhookEndpoints resource.</summary>
public sealed class WebhookEndpoints
{
    private readonly WireClient _client;
    internal WebhookEndpoints(WireClient client) => _client = client;

    public Task<WebhookEndpoint> CreateAsync(WebhookEndpointCreateParams p, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, object?>
        {
            ["url"] = p.Url,
            ["enabled_events"] = p.EnabledEvents,
        };
        return _client.RequestAsync<WebhookEndpoint>(
            HttpMethod.Post, "/v1/webhook_endpoints", body: body,
            idempotencyKey: p.IdempotencyKey, cancellationToken: cancellationToken);
    }

    public Task<WebhookEndpoint> RetrieveAsync(string id, CancellationToken cancellationToken = default) =>
        _client.RequestAsync<WebhookEndpoint>(
            HttpMethod.Get, $"/v1/webhook_endpoints/{Uri.EscapeDataString(id)}",
            cancellationToken: cancellationToken);

    public Task<WebhookEndpoint> UpdateAsync(string id, WebhookEndpointUpdateParams p, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, object?>
        {
            ["url"] = p.Url,
            ["enabled_events"] = p.EnabledEvents,
            ["status"] = p.Status,
        };
        return _client.RequestAsync<WebhookEndpoint>(
            HttpMethod.Post, $"/v1/webhook_endpoints/{Uri.EscapeDataString(id)}",
            body: body, idempotencyKey: p.IdempotencyKey, cancellationToken: cancellationToken);
    }

    public Task<Deleted> DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        _client.RequestAsync<Deleted>(
            HttpMethod.Delete, $"/v1/webhook_endpoints/{Uri.EscapeDataString(id)}",
            cancellationToken: cancellationToken);

    public IAsyncEnumerable<WebhookEndpoint> ListAsync(ListParams? parameters = null, CancellationToken cancellationToken = default) =>
        Pagination.Paginate<WebhookEndpoint>(_client, "/v1/webhook_endpoints", x => x.Id, parameters, cancellationToken);
}
