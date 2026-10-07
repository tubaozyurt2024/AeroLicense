using AeroLicense.Application.Abstractions;
using AeroLicense.Application.Common;
using AeroLicense.Application.Common.Exceptions;
using AeroLicense.Application.Common.Pagination;
using AeroLicense.Application.Features.Licenses;
using AeroLicense.Domain.Common;
using AeroLicense.Domain.Entities;
using AeroLicense.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AeroLicense.Application.Features.Applications;

public interface ILicenseApplicationService
{
    Task<LicenseApplicationDto> CreateAsync(CreateLicenseApplicationRequest request, CancellationToken cancellationToken);
    Task<LicenseApplicationDto> SubmitAsync(Guid id, CancellationToken cancellationToken);
    Task<LicenseApplicationDto> StartReviewAsync(Guid id, CancellationToken cancellationToken);
    Task<LicenseApplicationDto> ApproveAsync(Guid id, CancellationToken cancellationToken);
    Task<LicenseApplicationDto> RejectAsync(Guid id, RejectLicenseApplicationRequest request, CancellationToken cancellationToken);
    Task<LicenseApplicationDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<LicenseApplicationDto>> ListAsync(LicenseApplicationQuery query, CancellationToken cancellationToken);
}

/// <summary>
/// Servis sadece orkestrasyon yapar: kaydı yükle → domain metodunu çağır → audit ekle → tek SaveChanges.
/// İş kuralları (geçiş geçerli mi, gerekçe var mı) entity'de; bu yüzden servis ince kalır.
/// </summary>
public sealed class LicenseApplicationService(
    IAppDbContext db,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    LicenseDocumentIssuer documentIssuer) : ILicenseApplicationService
{
    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<LicenseApplicationDto> CreateAsync(CreateLicenseApplicationRequest request, CancellationToken cancellationToken)
    {
        var applicantId = currentUser.UserId;

        if (await db.LicenseApplications.AnyAsync(a => a.ApplicantId == applicantId && a.LicenseType == request.LicenseType
                && LicenseApplication.OpenStatuses.Contains(a.Status), cancellationToken))
            throw new ConflictException($"Bu türde ({request.LicenseType}) açık bir başvurunuz zaten var.");

        var now = Now;
        if (await db.Licenses.AnyAsync(l => l.HolderId == applicantId && l.Type == request.LicenseType
                && l.RevokedAtUtc == null && l.ExpiresAtUtc > now, cancellationToken))
            throw new ConflictException($"Geçerli bir {request.LicenseType} lisansınız zaten var.");

        var application = new LicenseApplication(applicantId, request.LicenseType, now);
        db.LicenseApplications.Add(application);
        Audit(AuditActions.ApplicationCreated, application.Id, $"Tür: {request.LicenseType}");
        await db.SaveChangesAsync(cancellationToken);

        return await GetAsync(application.Id, cancellationToken);
    }

    public async Task<LicenseApplicationDto> SubmitAsync(Guid id, CancellationToken cancellationToken)
    {
        // Başvuru sahibi sadece kendi başvurusunu bulabilir; başkasınınki için 404 (varlığı bile sızmaz).
        var application = await db.LicenseApplications
            .SingleOrDefaultAsync(a => a.Id == id && a.ApplicantId == currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("Başvuru bulunamadı.");

        // En güncel başarılı eğitim kanıt olarak alınır (70 puan kuralı domain'de tekrar doğrulanır).
        var training = await db.TrainingRecords
            .Where(t => t.ApplicantId == application.ApplicantId && t.LicenseType == application.LicenseType
                && t.ExamScore >= TrainingRecord.PassingScore)
            .OrderByDescending(t => t.CompletedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new DomainException(
                $"{application.LicenseType} başvurusu için tamamlanmış ve başarılı bir eğitim kaydı bulunamadı.");

        application.Submit(training, Now);
        Audit(AuditActions.ApplicationSubmitted, application.Id, $"Eğitim kaydı: {training.Id}");
        await db.SaveChangesAsync(cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    public async Task<LicenseApplicationDto> StartReviewAsync(Guid id, CancellationToken cancellationToken)
    {
        var application = await FindAsync(id, cancellationToken);
        application.StartReview(currentUser.UserId, Now);
        Audit(AuditActions.ApplicationReviewStarted, application.Id);
        await db.SaveChangesAsync(cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    public async Task<LicenseApplicationDto> ApproveAsync(Guid id, CancellationToken cancellationToken)
    {
        var application = await db.LicenseApplications.Include(a => a.Applicant)
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException("Başvuru bulunamadı.");
        var license = application.Approve(currentUser.UserId, Now);
        db.Licenses.Add(license);

        // Doğrulanabilir belge de aynı işlemde imzalanır: "lisans var ama belgesi yok" durumu oluşmaz.
        var document = documentIssuer.Issue(license, application.Applicant.FullName, Now);
        db.LicenseDocuments.Add(document);

        Audit(AuditActions.ApplicationApproved, application.Id);
        Audit(AuditActions.LicenseIssued, license.Id, $"Lisans no: {license.LicenseNumber}", nameof(License));
        Audit(AuditActions.DocumentIssued, document.Id, $"Anahtar: {document.KeyId}", nameof(LicenseDocument));
        await db.SaveChangesAsync(cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    public async Task<LicenseApplicationDto> RejectAsync(Guid id, RejectLicenseApplicationRequest request, CancellationToken cancellationToken)
    {
        var application = await FindAsync(id, cancellationToken);
        application.Reject(currentUser.UserId, request.Reason, Now);
        // Gerekçe serbest metin: kişisel veri içerebileceği için audit'e kopyalanmaz, başvuruda durur.
        Audit(AuditActions.ApplicationRejected, application.Id);
        await db.SaveChangesAsync(cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    public async Task<LicenseApplicationDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        await Project(VisibleApplications().Where(a => a.Id == id)).SingleOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("Başvuru bulunamadı.");

    public async Task<PagedResult<LicenseApplicationDto>> ListAsync(LicenseApplicationQuery query, CancellationToken cancellationToken)
    {
        var applications = VisibleApplications();
        if (query.Status is { } status)
            applications = applications.Where(a => a.Status == status);

        var totalCount = await applications.CountAsync(cancellationToken);
        var items = await Project(Sort(applications, query)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize))
            .ToListAsync(cancellationToken);

        return new PagedResult<LicenseApplicationDto>(items, query.Page, query.PageSize, totalCount);
    }

    // Id ikincil anahtar: aynı tarihli kayıtlarda sayfalar arası kayma/tekrar olmasın (kararlı sıralama).
    private static IQueryable<LicenseApplication> Sort(IQueryable<LicenseApplication> source, LicenseApplicationQuery query) =>
        (query.SortBy, query.SortDir) switch
        {
            (ApplicationSortField.SubmittedAt, SortDirection.Asc) => source.OrderBy(a => a.SubmittedAtUtc).ThenBy(a => a.Id),
            (ApplicationSortField.SubmittedAt, _) => source.OrderByDescending(a => a.SubmittedAtUtc).ThenByDescending(a => a.Id),
            (_, SortDirection.Asc) => source.OrderBy(a => a.CreatedAtUtc).ThenBy(a => a.Id),
            _ => source.OrderByDescending(a => a.CreatedAtUtc).ThenByDescending(a => a.Id)
        };

    // Kayıt düzeyinde yetki (IDOR'a karşı): başvuru sahibi kendi başvurularını, denetçi hepsini görür.
    private IQueryable<LicenseApplication> VisibleApplications() => currentUser.Role switch
    {
        UserRole.Applicant => db.LicenseApplications.Where(a => a.ApplicantId == currentUser.UserId),
        UserRole.Inspector => db.LicenseApplications,
        _ => throw new ForbiddenException("Bu rol lisans başvurularını görüntüleyemez.")
    };

    // Denetçi işlemleri için (rol kontrolü controller'da [Authorize] ile yapılır).
    private async Task<LicenseApplication> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await db.LicenseApplications.SingleOrDefaultAsync(a => a.Id == id, cancellationToken)
        ?? throw new NotFoundException("Başvuru bulunamadı.");

    private void Audit(string action, Guid entityId, string? details = null, string entityType = nameof(LicenseApplication)) =>
        db.AuditLogs.Add(new AuditLog(currentUser.UserId, action, entityType, entityId, Now, details));

    private static IQueryable<LicenseApplicationDto> Project(IQueryable<LicenseApplication> source) =>
        source.Select(a => new LicenseApplicationDto(
            a.Id, a.ApplicantId, a.Applicant.FullName, a.LicenseType, a.Status, a.CreatedAtUtc, a.SubmittedAtUtc,
            a.TrainingRecordId, a.ReviewerId, a.Reviewer != null ? a.Reviewer.FullName : null, a.ReviewStartedAtUtc,
            a.DecidedAtUtc, a.RejectionReason,
            a.License != null ? a.License.LicenseNumber : null,
            a.License != null ? a.License.ExpiresAtUtc : null));
}
