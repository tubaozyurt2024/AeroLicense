using System.Security.Cryptography;
using System.Text;
using AeroLicense.Application.Abstractions;
using AeroLicense.Application.Common;
using AeroLicense.Application.Common.Exceptions;
using AeroLicense.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AeroLicense.Application.Features.Auth;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<LoginResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken);
    Task LogoutAsync(RefreshRequest request, CancellationToken cancellationToken);
    Task<CurrentUserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed class AuthService(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    IJwtTokenService tokenService,
    TimeProvider timeProvider,
    ILogger<AuthService> logger) : IAuthService
{
    // Kullanıcı yok / parola yanlış / hesap pasif ayrımı yapılmaz: saldırgan hangi e-postanın
    // kayıtlı olduğunu öğrenemesin (user enumeration).
    private const string InvalidCredentials = "E-posta veya parola hatalı.";
    private const string InvalidRefreshToken = "Oturum yenilenemedi, tekrar giriş yapın.";

    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = User.NormalizeEmail(request.Email);
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
        {
            // Yanıt süresi de ipucu vermesin diye var olmayan kullanıcıda da hash maliyeti ödenir.
            passwordHasher.Hash(request.Password);
            logger.LogWarning("Başarısız giriş: kullanıcı yok ({Email})", Masking.Email(email));
            throw new UnauthorizedException(InvalidCredentials);
        }

        if (!passwordHasher.Verify(user.PasswordHash, request.Password) || !user.IsActive)
        {
            // Güvenlik olayı loglanır ama e-posta maskeli: log sistemine erişen herkes kişisel veri görmesin.
            logger.LogWarning("Başarısız giriş: {UserId} ({Email}), aktif: {IsActive}", user.Id, Masking.Email(email), user.IsActive);
            throw new UnauthorizedException(InvalidCredentials);
        }

        // Her giriş yeni bir belirteç ailesi başlatır.
        var refreshValue = NewTokenValue();
        var refreshToken = new RefreshToken(user.Id, Hash(refreshValue), familyId: Guid.NewGuid(), Now,
            tokenService.RefreshTokenLifetime);
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Başarılı giriş: {UserId} {Role}", user.Id, user.Role);
        return Response(user, refreshValue, refreshToken);
    }

    public async Task<LoginResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        var hash = Hash(request.RefreshToken);
        var current = await db.RefreshTokens.Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken)
            ?? throw new UnauthorizedException(InvalidRefreshToken);

        if (current.RevokedAtUtc is not null)
        {
            // Kullanılmış belirteç tekrar geldi: ya saldırgan ya da gerçek kullanıcı çalınmış bir kopyayı
            // kullanıyor. Hangisi olduğu bilinemez; güvenli taraf tüm aileyi iptal edip yeniden giriş istemek.
            await db.RefreshTokens.Where(t => t.FamilyId == current.FamilyId && t.RevokedAtUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAtUtc, Now), cancellationToken);
            logger.LogWarning("Refresh token tekrar kullanımı tespit edildi, aile iptal edildi: {UserId} {FamilyId}",
                current.UserId, current.FamilyId);
            throw new UnauthorizedException(InvalidRefreshToken);
        }

        if (!current.IsActiveAt(Now) || !current.User.IsActive)
            throw new UnauthorizedException(InvalidRefreshToken);

        var refreshValue = NewTokenValue();
        var next = current.Rotate(Hash(refreshValue), Now, tokenService.RefreshTokenLifetime);
        db.RefreshTokens.Add(next);
        await db.SaveChangesAsync(cancellationToken); // eşzamanlı ikinci yenileme burada concurrency hatası alır (409)

        return Response(current.User, refreshValue, next);
    }

    public async Task LogoutAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        var hash = Hash(request.RefreshToken);
        var token = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (token is null) return; // bilinmeyen belirteç için de aynı yanıt: bilgi sızdırılmaz

        // Çıkış bu cihazın ailesini kapatır. Access token kısa ömürlü olduğu için birkaç dakika içinde kendiliğinden düşer.
        await db.RefreshTokens.Where(t => t.FamilyId == token.FamilyId && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAtUtc, Now), cancellationToken);
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await db.Users.Where(u => u.Id == userId && u.IsActive)
            .Select(u => new CurrentUserResponse(u.Id, u.Role, u.FullName, u.OrganizationId,
                u.Organization != null ? u.Organization.Name : null))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new UnauthorizedException("Kullanıcı bulunamadı veya pasif."); // token geçerli ama hesap kapatılmış

    // 256 bit rastgele değer, URL-güvenli Base64. İstemciye bir kez verilir, sunucuda sadece özeti kalır.
    private static string NewTokenValue() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    // Yüksek entropili rastgele değer için tuzsuz, hızlı SHA-256 yeterli (parolalardaki gibi sözlük saldırısı yok).
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private LoginResponse Response(User user, string refreshValue, RefreshToken refreshToken)
    {
        var access = tokenService.CreateAccessToken(user);
        return new LoginResponse(access.Token, access.ExpiresAtUtc, user.Role.ToString(), refreshValue, refreshToken.ExpiresAtUtc);
    }
}
