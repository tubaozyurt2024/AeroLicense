using System.Security.Cryptography;
using System.Text;
using AeroLicense.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace AeroLicense.Infrastructure.Documents;

/// <summary>
/// HMAC-SHA256: düz SHA-256'dan farkı gizli anahtar. Düz hash'i veritabanına erişen biri içeriği değiştirip
/// yeniden hesaplayabilirdi; HMAC'i anahtarı bilmeden üretemez. Asimetrik imzaya (RSA/ECDSA) göre daha
/// basit ve hızlı; dezavantajı doğrulamanın da anahtar gerektirmesi, yani üçüncü taraflar çevrimdışı
/// doğrulayamaz ve doğrulama bizim endpoint'imizden geçer. Kurum dışı çevrimdışı doğrulama gerekirse
/// bir sonraki adım e-imza / ECDSA'dır (README: "Ölçeklenirse").
/// </summary>
public sealed class HmacDocumentSigner(IOptions<DocumentSigningOptions> options) : IDocumentSigner
{
    private readonly byte[] _key = Encoding.UTF8.GetBytes(options.Value.Key);

    public string CurrentKeyId { get; } = options.Value.KeyId;

    public string Sign(string content) => Convert.ToHexString(Compute(content));

    public bool Verify(string content, string signature, string keyId)
    {
        // Prototipte tek anahtar; döndürmede eski anahtarlar KeyId → anahtar sözlüğünde tutulur.
        if (keyId != CurrentKeyId) return false;

        byte[] expected;
        try { expected = Convert.FromHexString(signature); }
        catch (FormatException) { return false; }

        // Sabit zamanlı karşılaştırma: ilk farklı baytta erken dönen "==" yanıt süresinden imzayı sızdırabilir.
        return CryptographicOperations.FixedTimeEquals(Compute(content), expected);
    }

    private byte[] Compute(string content) => HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(content));
}
