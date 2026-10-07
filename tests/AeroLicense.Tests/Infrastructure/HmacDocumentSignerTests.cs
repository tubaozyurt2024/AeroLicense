using AeroLicense.Infrastructure.Documents;
using AeroLicense.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace AeroLicense.Tests.Infrastructure;

public class HmacDocumentSignerTests
{
    private const string Content = """{"licenseNumber":"AL-CPL-2026-ABC234","expiresAtUtc":"2028-09-29T12:00:00Z"}""";
    private readonly HmacDocumentSigner _signer = TestServices.Signer;

    [Fact]
    public void Imzalanan_icerik_dogrulanir() =>
        _signer.Verify(Content, _signer.Sign(Content), "test").Should().BeTrue();

    [Fact]
    public void Tek_karakter_degisse_imza_tutmaz() =>
        _signer.Verify(Content.Replace("2028", "2029"), _signer.Sign(Content), "test").Should().BeFalse();

    [Fact]
    public void Anahtari_bilmeyen_gecerli_imza_uretemez()
    {
        // Saldırgan içeriği değiştirip kendi anahtarıyla (veya düz hash ile) yeniden imzalasa bile tutmaz.
        var attacker = new HmacDocumentSigner(Options.Create(new DocumentSigningOptions { Key = new string('x', 32), KeyId = "test" }));
        var forged = Content.Replace("2028", "2099");

        _signer.Verify(forged, attacker.Sign(forged), "test").Should().BeFalse();
    }

    [Theory]
    [InlineData("baska-anahtar")]
    public void Bilinmeyen_anahtar_kimligi_reddedilir(string keyId) =>
        _signer.Verify(Content, _signer.Sign(Content), keyId).Should().BeFalse();

    [Fact]
    public void Bozuk_hex_imza_istisna_firlatmaz_false_doner() =>
        _signer.Verify(Content, "zz-not-hex", "test").Should().BeFalse();
}
