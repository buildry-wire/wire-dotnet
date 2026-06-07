using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Wire;

/// <summary>Thrown when a webhook signature does not verify. Verification fails closed.</summary>
public sealed class SignatureVerificationException : Exception
{
    public SignatureVerificationException(string message) : base(message) { }
}

/// <summary>
/// Verifies inbound webhook signatures. The signature is
/// <c>HMAC-SHA256(secret, "&lt;t&gt;.&lt;rawBody&gt;")</c> carried in the
/// <c>WirePayment-Signature: t=&lt;unix&gt;,v1=&lt;hex&gt;</c> header.
/// Always verify on the RAW request body, before any JSON parsing.
/// </summary>
public static class Webhooks
{
    public const string SignatureHeader = "WirePayment-Signature";
    public const int DefaultToleranceSeconds = 300;

    /// <summary>Verify a webhook and return the parsed event. <paramref name="payload"/> is the raw body.</summary>
    public static WireEvent Verify(
        string payload,
        string header,
        string secret,
        int toleranceSeconds = DefaultToleranceSeconds) =>
        Verify(Encoding.UTF8.GetBytes(payload ?? ""), header, secret, toleranceSeconds);

    /// <summary>Verify a webhook from a raw byte body and return the parsed event.</summary>
    public static WireEvent Verify(
        byte[] payload,
        string header,
        string secret,
        int toleranceSeconds = DefaultToleranceSeconds) =>
        VerifyAt(payload, header, secret, toleranceSeconds, DateTimeOffset.UtcNow.ToUnixTimeSeconds());

    /// <summary>The testable core taking an explicit <paramref name="now"/> (unix seconds).</summary>
    public static WireEvent VerifyAt(
        string payload,
        string header,
        string secret,
        int toleranceSeconds,
        long now) =>
        VerifyAt(Encoding.UTF8.GetBytes(payload ?? ""), header, secret, toleranceSeconds, now);

    /// <summary>The testable core taking an explicit <paramref name="now"/> (unix seconds).</summary>
    public static WireEvent VerifyAt(
        byte[] payload,
        string header,
        string secret,
        int toleranceSeconds,
        long now)
    {
        var (t, v1) = ParseHeader(header);
        if (t is null || string.IsNullOrEmpty(v1))
            throw new SignatureVerificationException("malformed signature header");

        if (Math.Abs(now - t.Value) > toleranceSeconds)
            throw new SignatureVerificationException("timestamp outside tolerance");

        var body = payload ?? Array.Empty<byte>();
        var prefix = Encoding.UTF8.GetBytes(t.Value.ToString(CultureInfo.InvariantCulture) + ".");
        var signed = new byte[prefix.Length + body.Length];
        Buffer.BlockCopy(prefix, 0, signed, 0, prefix.Length);
        Buffer.BlockCopy(body, 0, signed, prefix.Length, body.Length);

        byte[] mac;
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret ?? "")))
        {
            mac = hmac.ComputeHash(signed);
        }

        var expectedHex = Encoding.ASCII.GetBytes(ToHex(mac));
        var providedHex = Encoding.ASCII.GetBytes(v1!);

        if (expectedHex.Length != providedHex.Length ||
            !CryptographicOperations.FixedTimeEquals(expectedHex, providedHex))
        {
            throw new SignatureVerificationException("signature mismatch");
        }

        try
        {
            var ev = JsonSerializer.Deserialize<WireEvent>(body, WireClient.JsonOptions);
            return ev ?? throw new SignatureVerificationException("empty event payload");
        }
        catch (JsonException e)
        {
            throw new SignatureVerificationException($"invalid event payload: {e.Message}");
        }
    }

    private static (long? t, string? v1) ParseHeader(string? header)
    {
        if (string.IsNullOrEmpty(header)) return (null, null);

        long? t = null;
        string? v1 = null;
        foreach (var rawPart in header.Split(','))
        {
            var part = rawPart.Trim();
            var idx = part.IndexOf('=');
            if (idx <= 0) continue;
            var key = part.Substring(0, idx);
            var val = part.Substring(idx + 1);
            if (key == "t")
            {
                if (!long.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                    return (null, null);
                t = n;
            }
            else if (key == "v1")
            {
                v1 = val;
            }
        }
        return (t, v1);
    }

    private static string ToHex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        return sb.ToString();
    }
}
