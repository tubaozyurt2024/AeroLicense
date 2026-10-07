using AeroLicense.Application.Common;
using AeroLicense.Application.Features.Audit;
using AeroLicense.Application.Features.Training;
using AeroLicense.Domain.Entities;
using AeroLicense.Domain.Enums;
using AeroLicense.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace AeroLicense.Tests.Application;

public sealed class AuditLogTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly FakeTimeProvider _time = new(TestData.Now);
    private readonly Organization _ato = new("ATO", OrganizationType.TrainingOrg, "ATO-1");
    private readonly User _pilot = new("pilot1@aerolicense.test", "Pilot 1", UserRole.Applicant, TestData.Now, nationalId: "99999999901");
    private readonly User _atoUser;

    public AuditLogTests()
    {
        _atoUser = new User("egitim1@aerolicense.test", "Eğitim", UserRole.TrainingOrg, TestData.Now, _ato.Id);
        using var context = _db.CreateContext();
        context.Organizations.Add(_ato);
        _pilot.SetPasswordHash("x");
        _atoUser.SetPasswordHash("x");
        context.Users.AddRange(_pilot, _atoUser);
        context.SaveChanges();
    }

    private async Task<TrainingRecordDto> CreateTrainingAsync() =>
        await new TrainingService(_db.CreateContext(), FakeCurrentUser.From(_atoUser), _time)
            .CreateAsync(new CreateTrainingRecordRequest("99999999901", LicenseType.PPL, TestData.Now.AddDays(-1), 88), default);

    [Fact]
    public async Task Islem_audit_loga_kim_ne_zaman_hangi_kayit_ve_correlation_id_ile_yazilir()
    {
        var record = await CreateTrainingAsync();

        var page = await new AuditLogService(_db.CreateContext()).ListAsync(new AuditLogQuery { EntityId = record.Id }, default);

        var log = page.Items.Should().ContainSingle().Subject;
        log.Action.Should().Be(AuditActions.TrainingRecordCreated);
        log.ActorUserId.Should().Be(_atoUser.Id);
        log.ActorRole.Should().Be(UserRole.TrainingOrg);
        log.OccurredAtUtc.Should().Be(TestData.Now);
        log.CorrelationId.Should().Be(TestDb.CorrelationId);
    }

    [Fact]
    public async Task Kisisel_veri_maskelenir_eposta_ve_kimlik_no_acik_gorunmez()
    {
        await CreateTrainingAsync();

        var log = (await new AuditLogService(_db.CreateContext()).ListAsync(new AuditLogQuery(), default)).Items.Single();

        log.ActorEmailMasked.Should().Be("e***@aerolicense.test");
        log.Details.Should().NotContain("99999999901");
    }

    [Fact]
    public async Task Audit_kaydi_guncellenemez_ve_silinemez()
    {
        await CreateTrainingAsync();
        await using var context = _db.CreateContext();
        var log = await context.AuditLogs.SingleAsync();

        context.AuditLogs.Remove(log);

        await FluentActions.Awaiting(() => context.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Filtreler_ve_sayfalama_calisir()
    {
        await CreateTrainingAsync();
        await CreateTrainingAsync();
        var service = new AuditLogService(_db.CreateContext());

        (await service.ListAsync(new AuditLogQuery { ActorUserId = _pilot.Id }, default)).TotalCount.Should().Be(0);
        (await service.ListAsync(new AuditLogQuery { EntityType = nameof(TrainingRecord), PageSize = 1 }, default))
            .Should().Match<AeroLicense.Application.Common.Pagination.PagedResult<AuditLogDto>>(p => p.TotalCount == 2 && p.Items.Count == 1);
        (await service.ListAsync(new AuditLogQuery { FromUtc = TestData.Now.AddMinutes(1) }, default)).TotalCount.Should().Be(0);
    }

    [Fact]
    public void Bitis_tarihi_baslangictan_once_olamaz() =>
        new AuditLogQueryValidator()
            .Validate(new AuditLogQuery { FromUtc = TestData.Now, ToUtc = TestData.Now.AddDays(-1) })
            .IsValid.Should().BeFalse();

    public void Dispose() => _db.Dispose();
}
