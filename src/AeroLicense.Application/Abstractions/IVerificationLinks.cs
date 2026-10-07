namespace AeroLicense.Application.Abstractions;

/// <summary>QR'a gömülen herkese açık doğrulama adresini üretir (taban adres yapılandırmadan gelir).</summary>
public interface IVerificationLinks
{
    string For(string verificationCode);
}
