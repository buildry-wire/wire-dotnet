using System;
using System.Threading.Tasks;
using Xunit;

namespace Wire.Tests;

public class ClientTests
{
    private static WireClient NewClient(StubHandler handler) =>
        new("sk_test_123", new WireClientOptions
        {
            BaseUrl = "https://api.wire.test",
            HttpMessageHandler = handler,
            Backoff = TimeSpan.FromMilliseconds(1),
        });

    [Fact]
    public async Task SendsAuthAndDecodes()
    {
        var handler = new StubHandler(_ =>
            new StubResponse(200, "{\"id\":\"pi_1\",\"object\":\"payment_intent\",\"amount\":50000}"));
        using var wire = NewClient(handler);

        var pi = await wire.PaymentIntents.RetrieveAsync("pi_1");

        Assert.Equal("Bearer sk_test_123", handler.Requests[0].Headers["Authorization"]);
        Assert.Equal("pi_1", pi.Id);
        Assert.Equal(50000, pi.Amount);
    }

    [Fact]
    public async Task RetriesOn503()
    {
        var handler = new StubHandler((_, attempt) =>
            attempt < 2
                ? new StubResponse(503, "{}")
                : new StubResponse(200, "{\"id\":\"pi_1\",\"object\":\"payment_intent\"}"));
        using var wire = new WireClient("sk_test_123", new WireClientOptions
        {
            BaseUrl = "https://api.wire.test",
            HttpMessageHandler = handler,
            MaxRetries = 3,
            Backoff = TimeSpan.FromMilliseconds(1),
        });

        await wire.PaymentIntents.RetrieveAsync("pi_1");

        Assert.Equal(3, handler.Requests.Count);
    }

    [Fact]
    public async Task DoesNotRetryOn400()
    {
        var handler = new StubHandler(_ =>
            new StubResponse(400, "{\"error\":{\"type\":\"invalid_request_error\",\"message\":\"bad\"}}"));
        using var wire = new WireClient("sk_test_123", new WireClientOptions
        {
            BaseUrl = "https://api.wire.test",
            HttpMessageHandler = handler,
            MaxRetries = 3,
            Backoff = TimeSpan.FromMilliseconds(1),
        });

        await Assert.ThrowsAsync<WireException>(() => wire.PaymentIntents.RetrieveAsync("x"));
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task SendsIdempotencyKeyOnPost()
    {
        var handler = new StubHandler(_ =>
            new StubResponse(200, "{\"id\":\"pi_1\",\"object\":\"payment_intent\"}"));
        using var wire = NewClient(handler);

        await wire.PaymentIntents.CreateAsync(new PaymentIntentCreateParams { Amount = 1 });

        Assert.True(handler.Requests[0].Headers.ContainsKey("Idempotency-Key"));
        Assert.False(string.IsNullOrEmpty(handler.Requests[0].Headers["Idempotency-Key"]));
    }

    [Fact]
    public async Task ReusesIdempotencyKeyAcrossRetries()
    {
        var handler = new StubHandler((_, attempt) =>
            attempt == 0
                ? new StubResponse(500, "{}")
                : new StubResponse(200, "{\"id\":\"pi_1\",\"object\":\"payment_intent\"}"));
        using var wire = NewClient(handler);

        await wire.PaymentIntents.CreateAsync(new PaymentIntentCreateParams { Amount = 1 });

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(
            handler.Requests[0].Headers["Idempotency-Key"],
            handler.Requests[1].Headers["Idempotency-Key"]);
    }

    [Fact]
    public async Task SuppliedIdempotencyKeyIsUsed()
    {
        var handler = new StubHandler(_ =>
            new StubResponse(200, "{\"id\":\"pi_1\",\"object\":\"payment_intent\"}"));
        using var wire = NewClient(handler);

        await wire.PaymentIntents.CreateAsync(new PaymentIntentCreateParams { Amount = 1, IdempotencyKey = "my-key" });

        Assert.Equal("my-key", handler.Requests[0].Headers["Idempotency-Key"]);
    }

    [Fact]
    public async Task HonorsRetryAfterHeader()
    {
        var handler = new StubHandler((_, attempt) =>
            attempt == 0
                ? new StubResponse(429, "{}", new System.Collections.Generic.Dictionary<string, string> { ["Retry-After"] = "0" })
                : new StubResponse(200, "{\"id\":\"pi_1\",\"object\":\"payment_intent\"}"));
        using var wire = NewClient(handler);

        await wire.PaymentIntents.RetrieveAsync("pi_1");

        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task NoIdempotencyKeyOnGet()
    {
        var handler = new StubHandler(_ =>
            new StubResponse(200, "{\"id\":\"pi_1\",\"object\":\"payment_intent\"}"));
        using var wire = NewClient(handler);

        await wire.PaymentIntents.RetrieveAsync("pi_1");

        Assert.False(handler.Requests[0].Headers.ContainsKey("Idempotency-Key"));
    }
}
