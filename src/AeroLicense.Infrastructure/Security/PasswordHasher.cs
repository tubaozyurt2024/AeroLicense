using AeroLicense.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using AppPasswordHasher = AeroLicense.Application.Abstractions.IPasswordHasher;

namespace AeroLicense.Infrastructure.Security;

/// <summary>
/// ASP.NET Core Identity'nin PasswordHasher'ı: PBKDF2-HMAC-SHA512, kullanıcı başına rastgele salt,
/// yüksek iterasyon. Hash formatı sürüm bilgisi taşıdığı için algoritma ileride güçlendirilebilir.
/// </summary>
public sealed class PasswordHasher : AppPasswordHasher
{
    private readonly PasswordHasher<User> _inner = new();

    // Varsayılan implementasyon user parametresini kullanmıyor, bu yüzden null geçmek güvenli.
    public string Hash(string password) => _inner.HashPassword(null!, password);

    public bool Verify(string passwordHash, string password) =>
        _inner.VerifyHashedPassword(null!, passwordHash, password) != PasswordVerificationResult.Failed;
}
