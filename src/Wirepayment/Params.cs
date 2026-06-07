using System.Collections.Generic;

namespace Wire;

/// <summary>Common cursor-pagination parameters.</summary>
public sealed class ListParams
{
    /// <summary>Page size, 1-100.</summary>
    public int? Limit { get; set; }

    /// <summary>Return items after this object id (forward pagination).</summary>
    public string? StartingAfter { get; set; }

    /// <summary>Return items before this object id (backward pagination).</summary>
    public string? EndingBefore { get; set; }
}

/// <summary>Parameters for creating a PaymentIntent.</summary>
public sealed class PaymentIntentCreateParams
{
    /// <summary>Amount in minor units (e.g. MNT integer). Required.</summary>
    public long Amount { get; set; }

    public string? Currency { get; set; }

    /// <summary>Let Wire pick the operator automatically.</summary>
    public bool? AutomaticOperator { get; set; }

    /// <summary>The operator ids enabled on your account that this intent may use.</summary>
    public IList<string>? AllowedOperators { get; set; }

    public IDictionary<string, string>? Metadata { get; set; }

    /// <summary>Optional idempotency key; one is generated if omitted.</summary>
    public string? IdempotencyKey { get; set; }
}

/// <summary>Parameters for confirming a PaymentIntent.</summary>
public sealed class PaymentIntentConfirmParams
{
    public string? ReturnUrl { get; set; }
    public string? IdempotencyKey { get; set; }
}

/// <summary>Parameters for creating a webhook endpoint.</summary>
public sealed class WebhookEndpointCreateParams
{
    public string Url { get; set; } = "";
    public IList<string>? EnabledEvents { get; set; }
    public string? IdempotencyKey { get; set; }
}

/// <summary>Parameters for updating a webhook endpoint.</summary>
public sealed class WebhookEndpointUpdateParams
{
    public string? Url { get; set; }
    public IList<string>? EnabledEvents { get; set; }
    public string? Status { get; set; }
    public string? IdempotencyKey { get; set; }
}
