using AeroLicense.Application.Abstractions;
using AeroLicense.Application.Common;
using AeroLicense.Application.Common.Exceptions;
using AeroLicense.Application.Common.Pagination;
using AeroLicense.Domain.Entities;
using AeroLicense.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AeroLicense.Application.Features.Training;

public interface ITrainingService
{
    Task<TrainingRecordDto> CreateAsync(CreateTrainingRecordRequest request, CancellationToken cancellationToken);
    Task<PagedResult<TrainingRecordDto>> ListAsync(TrainingRecordQuery query, CancellationToken cancellationToken);
}

public sealed class TrainingService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider) : ITrainingService
{
    public async Task<TrainingRecordDto> CreateAsync(CreateTrainingRecordRequest request, CancellationToken cancellationToken)
    {
        // Kuruluş kimliği token'dan gelir: bir eğitim kuruluşu başka kuruluş adına kayıt açamaz.
        var trainingOrgId = currentUser.OrganizationId
            ?? throw new ForbiddenException("Kullanıcı bir eğitim kuruluşuna bağlı değil.");

        var applicant = await db.Users.SingleOrDefaultAsync(
            u => u.NationalId == request.ApplicantNationalId && u.Role == UserRole.Applicant, cancellationToken)
            ?? throw new NotFoundException("Bu kimlik numarasıyla kayıtlı başvuru sahibi bulunamadı.");

        var trainingOrg = await db.Organizations.SingleAsync(o => o.Id == trainingOrgId, cancellationToken);

        var record = new TrainingRecord(applicant.Id, trainingOrgId, currentUser.UserId, request.LicenseType,
            request.CompletedAtUtc, request.ExamScore, timeProvider.GetUtcNow().UtcDateTime);

        db.TrainingRecords.Add(record);
        db.AuditLogs.Add(new AuditLog(currentUser.UserId, AuditActions.TrainingRecordCreated, nameof(TrainingRecord),
            record.Id, record.RecordedAtUtc, $"Tür: {record.LicenseType}, puan: {record.ExamScore}"));
        await db.SaveChangesAsync(cancellationToken);

        return new TrainingRecordDto(record.Id, applicant.Id, applicant.FullName, Masking.NationalId(applicant.NationalId),
            trainingOrg.Name, record.LicenseType, record.CompletedAtUtc, record.ExamScore, record.IsPassed);
    }

    public async Task<PagedResult<TrainingRecordDto>> ListAsync(TrainingRecordQuery query, CancellationToken cancellationToken)
    {
        // Kayıt düzeyinde yetki: rol kontrolü yetmez, "hangi kayıtları görebilir?" sorusu da cevaplanmalı.
        // Aksi halde bir pilot applicantId'yi değiştirerek başkasının kayıtlarını görebilirdi (IDOR).
        var records = currentUser.Role switch
        {
            UserRole.Applicant => db.TrainingRecords.Where(r => r.ApplicantId == currentUser.UserId),
            UserRole.TrainingOrg => db.TrainingRecords.Where(r => r.TrainingOrgId == currentUser.OrganizationId),
            UserRole.Inspector => db.TrainingRecords,
            _ => throw new ForbiddenException("Bu rol eğitim kayıtlarını görüntüleyemez.")
        };

        if (query.ApplicantId is { } applicantId)
            records = records.Where(r => r.ApplicantId == applicantId);
        if (query.PassedOnly)
            records = records.Where(r => r.ExamScore >= TrainingRecord.PassingScore);

        var totalCount = await records.CountAsync(cancellationToken);

        // Projection: sadece gereken kolonlar SQL'de seçilir, entity'ler takip edilmez (AsNoTracking gerekmez).
        var rows = await records
            .OrderByDescending(r => r.CompletedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new
            {
                r.Id, r.ApplicantId, ApplicantName = r.Applicant.FullName, r.Applicant.NationalId,
                TrainingOrgName = r.TrainingOrg.Name, r.LicenseType, r.CompletedAtUtc, r.ExamScore
            })
            .ToListAsync(cancellationToken);

        // Maskeleme SQL'e çevrilemeyeceği için veri geldikten sonra bellekte yapılır.
        var items = rows.Select(r => new TrainingRecordDto(r.Id, r.ApplicantId, r.ApplicantName,
            Masking.NationalId(r.NationalId), r.TrainingOrgName, r.LicenseType, r.CompletedAtUtc, r.ExamScore,
            r.ExamScore >= TrainingRecord.PassingScore)).ToList();

        return new PagedResult<TrainingRecordDto>(items, query.Page, query.PageSize, totalCount);
    }
}
