using System;
using System.Text.Json;

namespace Wire;

/// <summary>
/// A typed error returned by the Wire API, decoded from the <c>{"error":{...}}</c> envelope.
/// The API key is never included in the message or any field.
/// </summary>
public class WireException : Exception
{
    public string Type { get; }
    public string? Code { get; }
    public string? Param { get; }
    public string? RequestId { get; }
    public string? DocUrl { get; }
    public string? OperatorDeclineCode { get; }
    public int? StatusCode { get; }

    public WireException(
        string message,
        string type = "api_error",
        string? code = null,
        string? param = null,
        string? requestId = null,
        string? docUrl = null,
        string? operatorDeclineCode = null,
        int? statusCode = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Type = type;
        Code = code;
        Param = param;
        RequestId = requestId;
        DocUrl = docUrl;
        OperatorDeclineCode = operatorDeclineCode;
        StatusCode = statusCode;
    }

    public override string ToString() =>
        $"WireException: {Message} (type={Type}, code={Code}, status={StatusCode}, request_id={RequestId})";

    /// <summary>Decode the Wire error envelope; fall back to a generic error.</summary>
    public static WireException Parse(int status, string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var err) &&
                err.ValueKind == JsonValueKind.Object)
            {
                return new WireException(
                    message: Str(err, "message") ?? "request failed",
                    type: Str(err, "type") ?? "api_error",
                    code: Str(err, "code"),
                    param: Str(err, "param"),
                    requestId: Str(err, "request_id"),
                    docUrl: Str(err, "doc_url"),
                    operatorDeclineCode: Str(err, "operator_decline_code"),
                    statusCode: status);
            }
        }
        catch (JsonException)
        {
            // fall through
        }

        return new WireException(
            $"unexpected response (status {status})",
            type: "api_error",
            statusCode: status);
    }

    private static string? Str(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;
}

/// <summary>
/// Raised when the request fails at the transport layer (network error or timeout)
/// after retries are exhausted, i.e. no HTTP response was obtained.
/// </summary>
public sealed class WireConnectionException : WireException
{
    public WireConnectionException(string message, Exception? innerException = null)
        : base(message, type: "api_connection_error", innerException: innerException)
    {
    }
}
