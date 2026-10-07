using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AeroLicense.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required] public string Issuer { get; init; } = string.Empty;
    [Required] public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// appsettings'te YOK: ortam değişkeni (Jwt__SigningKey) veya user-secrets'tan gelir.
    /// HS256 için en az 256 bit (32 bayt) anahtar gerekir.
    /// </summary>
    [Required, MinLength(32)] public string SigningKey { get; init; } = string.Empty;

    // Kısa ömürlü access token: çalınırsa kötüye kullanım penceresi dar kalır.
    [Range(1, 60)] public int AccessTokenMinutes { get; init; } = 15;

    [Range(1, 30)] public int RefreshTokenDays { get; init; } = 7;

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));
}
