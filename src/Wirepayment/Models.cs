using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Wire;

/// <summary>A PaymentIntent represents an intent to collect a payment.</summary>
public sealed class PaymentIntent
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("object")] public string Object { get; init; } = "payment_intent";
    [JsonPropertyName("amount")] public long Amount { get; init; }
    [JsonPropertyName("currency")] public string Currency { get; init; } = "";
    [JsonPropertyName("status")] public string Status { get; init; } = "";
    [JsonPropertyName("client_secret")] public string? ClientSecret { get; init; }
    [JsonPropertyName("automatic_operator")] public bool AutomaticOperator { get; init; }
    [JsonPropertyName("allowed_operators")] public IReadOnlyList<string> AllowedOperators { get; init; } = new List<string>();
    [JsonPropertyName("selected_operator")] public string? SelectedOperator { get; init; }
    [JsonPropertyName("next_action")] public Dictionary<string, object>? NextAction { get; init; }
    [JsonPropertyName("metadata")] public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
    [JsonPropertyName("livemode")] public bool Livemode { get; init; }
    [JsonPropertyName("created")] public long Created { get; init; }
    [JsonPropertyName("expires_at")] public long? ExpiresAt { get; init; }
}

/// <summary>A Charge is the result of dispatching a PaymentIntent to an operator.</summary>
public sealed class Charge
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("object")] public string Object { get; init; } = "charge";
    [JsonPropertyName("payment_intent")] public string PaymentIntent { get; init; } = "";
    [JsonPropertyName("operator")] public string Operator { get; init; } = "";
    [JsonPropertyName("operator_charge_id")] public string? OperatorChargeId { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = "";
    [JsonPropertyName("amount")] public long Amount { get; init; }
    [JsonPropertyName("fee")] public long Fee { get; init; }
    [JsonPropertyName("amount_refunded")] public long AmountRefunded { get; init; }
    [JsonPropertyName("failure_code")] public string? FailureCode { get; init; }
    [JsonPropertyName("failure_message")] public string? FailureMessage { get; init; }
    [JsonPropertyName("livemode")] public bool Livemode { get; init; }
    [JsonPropertyName("created")] public long Created { get; init; }
}

/// <summary>A WireEvent is an event emitted by the Wire API (and delivered via webhooks).</summary>
public sealed class WireEvent
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("object")] public string Object { get; init; } = "event";
    [JsonPropertyName("type")] public string Type { get; init; } = "";
    [JsonPropertyName("api_version")] public string? ApiVersion { get; init; }
    [JsonPropertyName("data")] public Dictionary<string, object>? Data { get; init; }
    [JsonPropertyName("livemode")] public bool Livemode { get; init; }
    [JsonPropertyName("created")] public long Created { get; init; }
}

/// <summary>A WebhookEndpoint is a registered URL that receives event notifications.</summary>
public sealed class WebhookEndpoint
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("object")] public string Object { get; init; } = "webhook_endpoint";
    [JsonPropertyName("url")] public string Url { get; init; } = "";
    [JsonPropertyName("enabled_events")] public IReadOnlyList<string> EnabledEvents { get; init; } = new List<string>();
    [JsonPropertyName("status")] public string Status { get; init; } = "";
    [JsonPropertyName("secret")] public string? Secret { get; init; }
    [JsonPropertyName("livemode")] public bool Livemode { get; init; }
    [JsonPropertyName("created")] public long Created { get; init; }
}

/// <summary>Returned by delete operations.</summary>
public sealed class Deleted
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("object")] public string Object { get; init; } = "";
    [JsonPropertyName("deleted")] public bool IsDeleted { get; init; }
}

/// <summary>A cursor-paginated list page.</summary>
public sealed class WireList<T>
{
    [JsonPropertyName("object")] public string Object { get; init; } = "list";
    [JsonPropertyName("data")] public IReadOnlyList<T> Data { get; init; } = new List<T>();
    [JsonPropertyName("has_more")] public bool HasMore { get; init; }
}
