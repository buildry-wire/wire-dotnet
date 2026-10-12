# Wirepayment

Official .NET server-side SDK for the [Wire](https://wire.mn) payment API. Targets .NET 8, with zero third-party dependencies (built on `System.Net.Http` and `System.Text.Json`).

## Install

```bash
dotnet add package Wirepayment
```

## Quickstart

```csharp
using Wire;

using var wire = new WireClient("sk_live_...");

// Amounts are whole tögrög (MNT integer, no fractional part): 50000 = 50,000 ₮.
// Without AllowedOperators, Wire routes to the operators enabled on your project.
var pi = await wire.PaymentIntents.CreateAsync(new PaymentIntentCreateParams
{
    Amount = 50000,
    Currency = "MNT",
});

var confirmed = await wire.PaymentIntents.ConfirmAsync(pi.Id);
Console.WriteLine($"{confirmed.Id} {confirmed.Status}");
```

## Auto-pagination

```csharp
await foreach (var charge in wire.Charges.ListAsync(new ListParams { Limit = 50 }))
{
    Console.WriteLine(charge.Id);
}
```

## Webhook verification

Always verify on the raw, unparsed request body.

```csharp
using Wire;

// rawBody is the unparsed request body (string or byte[]).
// header is the value of the WirePayment-Signature request header.
var ev = Webhooks.Verify(rawBody, header, endpointSecret);
Console.WriteLine(ev.Type);
```

`Webhooks.Verify` throws `SignatureVerificationException` on any mismatch, malformed header,
or timestamp outside tolerance (default 300s). It fails closed.

## Errors

```csharp
using Wire;

try
{
    await wire.PaymentIntents.CreateAsync(new PaymentIntentCreateParams { Amount = -1 });
}
catch (WireException e)
{
    Console.WriteLine($"{e.Code} {e.RequestId} {e.StatusCode}");
}
```

Network and timeout failures surface as `WireConnectionException` (a subclass of `WireException`).
The API key is never logged nor included in any exception.

## Configuration

```csharp
using var wire = new WireClient("sk_live_...", new WireClientOptions
{
    BaseUrl = "https://api.wire.mn", // default
    Timeout = TimeSpan.FromSeconds(30),
    MaxRetries = 2,
    // HttpClient = myHttpClient,          // inject your own
    // HttpMessageHandler = myHandler,     // or a custom handler
});
```

Requests are retried with exponential backoff and jitter on 429, 5xx, and network errors,
honoring `Retry-After`. POST requests carry an `Idempotency-Key` (generated if not supplied),
reused across retries.

## Documentation

Full API documentation: [docs.wire.mn](https://docs.wire.mn).

## License

MIT
