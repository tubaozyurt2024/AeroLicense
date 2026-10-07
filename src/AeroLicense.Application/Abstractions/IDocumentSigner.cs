namespace AeroLicense.Application.Abstractions;

/// <summary>Belge içeriğini gizli anahtarla imzalar (HMAC). Anahtar Infrastructure'da, uygulama sadece arayüzü bilir.</summary>
public interface IDocumentSigner
{
    /// <summary>Yeni imzalarda kullanılan anahtarın kimliği.</summary>
    string CurrentKeyId { get; }

    string Sign(string content);

    /// <summary>Sabit zamanlı karşılaştırma yapar; bilinmeyen anahtar kimliğinde false döner.</summary>
    bool Verify(string content, string signature, string keyId);
}
