using System;
using System.Threading.Tasks;
using Xunit;

namespace Wire.Tests;

public class ErrorTests
{
    [Fact]
    public void ParsesTheEnvelope()
    {
        const string body =
            "{\"error\":{\"type\":\"invalid_request_error\",\"code\":\"amount_invalid\",\"message\":\"amount must be positive\",\"param\":\"amount\",\"request_id\":\"req_123\"}}";

        var err = WireException.Parse(400, body);

        Assert.Equal(400, err.StatusCode);
        Assert.Equal("invalid_request_error", err.Type);
        Assert.Equal("amount_invalid", err.Code);
        Assert.Equal("amount", err.Param);
        Assert.Equal("req_123", err.RequestId);
        Assert.Contains("amount must be positive", err.ToString());
    }

    [Fact]
    public void FallsBackOnNonJson()
    {
        var err = WireException.Parse(500, "not json");

        Assert.Equal(500, err.StatusCode);
        Assert.Equal("api_error", err.Type);
    }

    [Fact]
    public void DecodesOperatorDeclineCode()
    {
        const string body =
            "{\"error\":{\"type\":\"operator_error\",\"code\":\"card_declined\",\"message\":\"declined\",\"operator_decline_code\":\"insufficient_funds\",\"doc_url\":\"https://docs.wire.mn/errors\"}}";

        var err = WireException.Parse(402, body);

        Assert.Equal("operator_error", err.Type);
        Assert.Equal("insufficient_funds", err.OperatorDeclineCode);
        Assert.Equal("https://docs.wire.mn/errors", err.DocUrl);
    }

    [Fact]
    public async Task NetworkFailureRaisesConnectionException()
    {
        var handler = new ThrowingHandler();
        using var wire = new WireClient("sk_test_123", new WireClientOptions
        {
            BaseUrl = "https://api.wire.test",
            HttpMessageHandler = handler,
            MaxRetries = 0,
            Backoff = TimeSpan.FromMilliseconds(1),
        });

        await Assert.ThrowsAsync<WireConnectionException>(() => wire.PaymentIntents.RetrieveAsync("pi_1"));
    }

    private sealed class ThrowingHandler : System.Net.Http.HttpMessageHandler
    {
        protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(
            System.Net.Http.HttpRequestMessage request,
            System.Threading.CancellationToken cancellationToken) =>
            throw new System.Net.Http.HttpRequestException("connection refused");
    }
}
