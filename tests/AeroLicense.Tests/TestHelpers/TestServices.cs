using AeroLicense.Application.Abstractions;
using AeroLicense.Application.Features.Licenses;
using AeroLicense.Infrastructure.Documents;
using Microsoft.Extensions.Options;

namespace AeroLicense.Tests.TestHelpers;

public static class TestServices
{
    public static readonly HmacDocumentSigner Signer =
        new(Options.Create(new DocumentSigningOptions { Key = new string('s', 32), KeyId = "test" }));

    public static readonly LicenseDocumentIssuer DocumentIssuer = new(Signer);

    public static readonly VerificationLinks Links =
        new(Options.Create(new VerificationOptions { PublicBaseUrl = "https://verify.test/dogrula" }));
}

/// <summary>QR içeriğini test edebilmek için PNG yerine metni bayt olarak döner.</summary>
public sealed class FakeQrCodeGenerator : IQrCodeGenerator
{
    public byte[] GeneratePng(string text) => System.Text.Encoding.UTF8.GetBytes(text);
}
