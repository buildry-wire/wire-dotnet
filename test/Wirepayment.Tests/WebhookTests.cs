using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Wire.Tests;

public class WebhookTests
{
    private sealed class VectorFile
    {
        [JsonPropertyName("secret")] public string Secret { get; set; } = "";
        [JsonPropertyName("now")] public long Now { get; set; }
        [JsonPropertyName("tolerance_seconds")] public int ToleranceSeconds { get; set; }
        [JsonPropertyName("cases")] public List<VectorCase> Cases { get; set; } = new();
    }

    private sealed class VectorCase
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("body")] public string Body { get; set; } = "";
        [JsonPropertyName("header")] public string Header { get; set; } = "";
        [JsonPropertyName("valid")] public bool Valid { get; set; }
    }

    private static VectorFile LoadVectors()
    {
        var path = Path.Combine(System.AppContext.BaseDirectory, "data", "webhook-signatures.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<VectorFile>(json)!;
    }

    public static IEnumerable<object[]> Cases()
    {
        var vectors = LoadVectors();
        foreach (var c in vectors.Cases)
            yield return new object[] { c.Name, c.Body, c.Header, c.Valid };
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ConformsToVectors(string name, string body, string header, bool valid)
    {
        var vectors = LoadVectors();
        _ = name;

        var ok = true;
        WireEvent? ev = null;
        try
        {
            ev = Webhooks.VerifyAt(body, header, vectors.Secret, vectors.ToleranceSeconds, vectors.Now);
        }
        catch (SignatureVerificationException)
        {
            ok = false;
        }

        Assert.Equal(valid, ok);
        if (valid)
            Assert.False(string.IsNullOrEmpty(ev!.Type));
    }

    [Fact]
    public void RejectsMalformedHeader()
    {
        Assert.Throws<SignatureVerificationException>(() =>
            Webhooks.VerifyAt("{}", "garbage", "secret", 300, 1700000300));
    }
}
