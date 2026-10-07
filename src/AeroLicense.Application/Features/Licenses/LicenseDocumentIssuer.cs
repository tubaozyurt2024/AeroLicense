using System.Text.Json;
using System.Text.Json.Serialization;
using AeroLicense.Application.Abstractions;
using AeroLicense.Domain.Entities;
using AeroLicense.Domain.Enums;

namespace AeroLicense.Application.Features.Licenses;

/// <summary>
/// Belgenin imzalanan içeriği. SchemaVersion: alan eklenirse eski belgeler eski şemayla okunmaya devam eder.
/// Sadece belgede basılı olması gereken bilgiler var; TC kimlik no, e-posta gibi veriler yok.
/// </summary>
public sealed record LicenseDocumentContent(
    int SchemaVersion,
    string LicenseNumber,
    string HolderName,
    LicenseType LicenseType,
    DateTime IssuedAtUtc,
    DateTime ExpiresAtUtc)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public string Serialize() => JsonSerializer.Serialize(this, JsonOptions);

    public static LicenseDocumentContent? TryDeserialize(string json)
    {
        try { return JsonSerializer.Deserialize<LicenseDocumentContent>(json, JsonOptions); }
        catch (JsonException) { return null; }
    }
}

public sealed class LicenseDocumentIssuer(IDocumentSigner signer)
{
    public LicenseDocument Issue(License license, string holderName, DateTime nowUtc)
    {
        var content = new LicenseDocumentContent(1, license.LicenseNumber, holderName, license.Type,
            license.IssuedAtUtc, license.ExpiresAtUtc).Serialize();

        return new LicenseDocument(license.Id, content, signer.Sign(content), signer.CurrentKeyId, nowUtc);
    }
}
