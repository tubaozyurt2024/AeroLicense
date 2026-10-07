using AeroLicense.Application.Abstractions;
using Microsoft.Extensions.Options;
using QRCoder;

namespace AeroLicense.Infrastructure.Documents;

public sealed class QrCodeGenerator : IQrCodeGenerator
{
    public byte[] GeneratePng(string text)
    {
        // Q seviyesi hata düzeltme: basılı belgede %25'e kadar hasar/kir olsa da okunur.
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        return new PngByteQRCode(data).GetGraphic(pixelsPerModule: 8);
    }
}

public sealed class VerificationLinks(IOptions<VerificationOptions> options) : IVerificationLinks
{
    private readonly string _baseUrl = options.Value.PublicBaseUrl.TrimEnd('/') + "/";

    public string For(string verificationCode) => _baseUrl + Uri.EscapeDataString(verificationCode);
}
