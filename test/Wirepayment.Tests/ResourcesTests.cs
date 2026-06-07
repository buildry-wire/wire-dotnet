using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Wire.Tests;

public class ResourcesTests
{
    private static WireClient NewClient(StubHandler handler) =>
        new("sk_test_123", new WireClientOptions
        {
            BaseUrl = "https://api.wire.test",
            HttpMessageHandler = handler,
            Backoff = TimeSpan.FromMilliseconds(1),
        });

    [Fact]
    public async Task CreatesPaymentIntent()
    {
        var handler = new StubHandler(req =>
        {
            Assert.Equal(System.Net.Http.HttpMethod.Post, req.Method);
            Assert.Equal("/v1/payment_intents", req.Path);
            Assert.Contains("\"amount\":50000", req.Body);
            Assert.Contains("\"sandbox\"", req.Body);
            return new StubResponse(200,
                "{\"id\":\"pi_1\",\"object\":\"payment_intent\",\"amount\":50000,\"status\":\"requires_payment_method\"}");
        });
        using var wire = NewClient(handler);

        var pi = await wire.PaymentIntents.CreateAsync(new PaymentIntentCreateParams
        {
            Amount = 50000,
            Currency = "MNT",
            AllowedOperators = new List<string> { "sandbox" },
        });

        Assert.Equal("pi_1", pi.Id);
        Assert.Equal("requires_payment_method", pi.Status);
    }

    [Fact]
    public async Task ConfirmAndCancel()
    {
        var handler = new StubHandler(req =>
        {
            if (req.Path.EndsWith("/confirm", StringComparison.Ordinal))
                return new StubResponse(200, "{\"id\":\"pi_1\",\"object\":\"payment_intent\",\"status\":\"processing\"}");
            return new StubResponse(200, "{\"id\":\"pi_1\",\"object\":\"payment_intent\",\"status\":\"canceled\"}");
        });
        using var wire = NewClient(handler);

        var confirmed = await wire.PaymentIntents.ConfirmAsync("pi_1");
        var canceled = await wire.PaymentIntents.CancelAsync("pi_1");

        Assert.Equal("processing", confirmed.Status);
        Assert.Equal("canceled", canceled.Status);
    }

    [Fact]
    public async Task AutoPaginatesList()
    {
        var handler = new StubHandler(req =>
            !req.Query.Contains("starting_after")
                ? new StubResponse(200, "{\"object\":\"list\",\"has_more\":true,\"data\":[{\"id\":\"ch_1\",\"object\":\"charge\"}]}")
                : new StubResponse(200, "{\"object\":\"list\",\"has_more\":false,\"data\":[{\"id\":\"ch_2\",\"object\":\"charge\"}]}"));
        using var wire = NewClient(handler);

        var ids = new List<string>();
        await foreach (var ch in wire.Charges.ListAsync(new ListParams { Limit = 1 }))
            ids.Add(ch.Id);

        Assert.Equal(new[] { "ch_1", "ch_2" }, ids);
    }

    [Fact]
    public async Task ListStopsOnEmptyData()
    {
        var handler = new StubHandler(_ =>
            new StubResponse(200, "{\"object\":\"list\",\"has_more\":true,\"data\":[]}"));
        using var wire = NewClient(handler);

        var count = 0;
        await foreach (var _ in wire.Events.ListAsync())
            count++;

        Assert.Equal(0, count);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task DeletesWebhookEndpoint()
    {
        var handler = new StubHandler(_ =>
            new StubResponse(200, "{\"id\":\"we_1\",\"object\":\"webhook_endpoint\",\"deleted\":true}"));
        using var wire = NewClient(handler);

        var d = await wire.WebhookEndpoints.DeleteAsync("we_1");

        Assert.True(d.IsDeleted);
        Assert.Equal(System.Net.Http.HttpMethod.Delete, handler.Requests[0].Method);
    }

    [Fact]
    public async Task CreatesWebhookEndpoint()
    {
        var handler = new StubHandler(req =>
        {
            Assert.Contains("https://example.test/hook", req.Body);
            return new StubResponse(200,
                "{\"id\":\"we_1\",\"object\":\"webhook_endpoint\",\"url\":\"https://example.test/hook\",\"status\":\"enabled\"}");
        });
        using var wire = NewClient(handler);

        var we = await wire.WebhookEndpoints.CreateAsync(new WebhookEndpointCreateParams
        {
            Url = "https://example.test/hook",
            EnabledEvents = new List<string> { "payment_intent.succeeded" },
        });

        Assert.Equal("we_1", we.Id);
        Assert.Equal("enabled", we.Status);
    }
}
