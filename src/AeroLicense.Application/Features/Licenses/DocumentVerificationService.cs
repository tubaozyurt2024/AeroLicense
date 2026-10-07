using AeroLicense.Application.Abstractions;
using AeroLicense.Application.Common;
using AeroLicense.Application.Common.Exceptions;
using AeroLicense.Domain.Entities;
using AeroLicense.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AeroLicense.Application.Features.Licenses;

public interface IDocumentVerificationService
{
    Task<VerificationResultDto> VerifyAsync(string code, CancellationToken cancellationToken);
}

public sealed class DocumentVerificationService(IAppDbContext db, IDocumentSigner signer, TimeProvider timeProvider)
    : IDocumentVerificationService
{
    public async Task<VerificationResultDto> VerifyAsync(string code, CancellationToken cancellationToken)
    {
        // Formatı bozuk kod için veritabanına hiç gidilmez (ucuz ret, rastgele deneme yükünü azaltır).
        if (!LicenseDocument.IsWellFormedCode(code))
            throw new NotFoundException("Belge bulunamadı.");

        var document = await db.LicenseDocuments.AsNoTracking()
            .Include(d => d.License)
            .SingleOrDefaultAsync(d => d.VerificationCode == code, cancellationToken)
            ?? throw new NotFoundException("Belge bulunamadı.");

        var now = timeProvider.GetUtcNow().UtcDateTime;

        // 1) İmza: içerik, imzalandığı günden beri bir bayt bile değiştiyse HMAC tutmaz.
        // 2) Tutarlılık: içerik sağlam ama lisans kaydı değiştirilmişse (ör. bitiş tarihi DB'de uzatıldı) yakalanır.
        var content = signer.Verify(document.Content, document.Signature, document.KeyId)
            ? LicenseDocumentContent.TryDeserialize(document.Content)
            : null;
        if (content is null || !MatchesLicense(content, document.License))
            return new VerificationResultDto(VerificationStatus.Tampered, null, null, null, null, now);

        var status = document.License.StatusAt(now) switch
        {
            LicenseStatus.Revoked => VerificationStatus.Revoked,
            LicenseStatus.Expired => VerificationStatus.Expired,
            _ => VerificationStatus.Valid
        };

        return new VerificationResultDto(status, content.LicenseNumber, content.LicenseType,
            Masking.FullName(content.HolderName), content.ExpiresAtUtc, now);
    }

    private static bool MatchesLicense(LicenseDocumentContent content, License license) =>
        content.LicenseNumber == license.LicenseNumber
        && content.LicenseType == license.Type
        && content.IssuedAtUtc == license.IssuedAtUtc
        && content.ExpiresAtUtc == license.ExpiresAtUtc;
}
