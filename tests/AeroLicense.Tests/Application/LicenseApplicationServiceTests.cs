using AeroLicense.Application.Common;
using AeroLicense.Application.Common.Exceptions;
using AeroLicense.Application.Features.Applications;
using AeroLicense.Domain.Common;
using AeroLicense.Domain.Entities;
using AeroLicense.Domain.Enums;
using AeroLicense.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace AeroLicense.Tests.Application;

public sealed class LicenseApplicationServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly FakeTimeProvider _time = new(TestData.Now);

    private readonly Organization _ato = new("ATO", OrganizationType.TrainingOrg, "ATO-1");
    private readonly User _pilot1 = new("p1@aerolicense.test", "Pilot 1", UserRole.Applicant, TestData.Now, nationalId: "99999999901");
    private readonly User _pilot2 = new("p2@aerolicense.test", "Pilot 2", UserRole.Applicant, TestData.Now, nationalId: "99999999902");
    private readonly User _inspector1 = new("i1@aerolicense.test", "Denetçi 1", UserRole.Inspector, TestData.Now);
    private readonly User _inspector2 = new("i2@aerolicense.test", "Denetçi 2", UserRole.Inspector, TestData.Now);

    public LicenseApplicationServiceTests()
    {
        var atoUser = new User("t@aerolicense.test", "ATO Kullanıcı", UserRole.TrainingOrg, TestData.Now, _ato.Id);
        using var context = _db.CreateContext();
        context.Organizations.Add(_ato);
        foreach (var user in new[] { _pilot1, _pilot2, _inspector1, _inspector2, atoUser })
            user.SetPasswordHash("x");
        context.Users.AddRange(_pilot1, _pilot2, _inspector1, _inspector2, atoUser);
        context.TrainingRecords.AddRange(
            new TrainingRecord(_pilot1.Id, _ato.Id, atoUser.Id, LicenseType.CPL, TestData.Now.AddDays(-10), 55, TestData.Now),
            new TrainingRecord(_pilot1.Id, _ato.Id, atoUser.Id, LicenseType.CPL, TestData.Now.AddDays(-2), 80, TestData.Now),
            new TrainingRecord(_pilot2.Id, _ato.Id, atoUser.Id, LicenseType.CPL, TestData.Now.AddDays(-2), 60, TestData.Now));
        context.SaveChanges();
    }

    private LicenseApplicationService ServiceFor(User user) =>
        new(_db.CreateContext(), FakeCurrentUser.From(user), _time, TestServices.DocumentIssuer);

    private async Task<LicenseApplicationDto> UnderReviewAsync()
    {
        var created = await ServiceFor(_pilot1).CreateAsync(new CreateLicenseApplicationRequest(LicenseType.CPL), default);
        await ServiceFor(_pilot1).SubmitAsync(created.Id, default);
        return await ServiceFor(_inspector1).StartReviewAsync(created.Id, default);
    }

    [Fact]
    public async Task Onay_lisans_uretir_ve_her_gecis_audit_loga_yazilir()
    {
        var application = await UnderReviewAsync();

        var approved = await ServiceFor(_inspector1).ApproveAsync(application.Id, default);

        approved.Status.Should().Be(ApplicationStatus.Approved);
        approved.ReviewerName.Should().Be("Denetçi 1");
        approved.LicenseNumber.Should().StartWith("AL-CPL-");

        await using var context = _db.CreateContext();
        (await context.Licenses.SingleAsync()).HolderId.Should().Be(_pilot1.Id);
        (await context.AuditLogs.OrderBy(a => a.OccurredAtUtc).Select(a => a.Action).ToListAsync())
            .Should().BeEquivalentTo(
                AuditActions.ApplicationCreated, AuditActions.ApplicationSubmitted, AuditActions.ApplicationReviewStarted,
                AuditActions.ApplicationApproved, AuditActions.LicenseIssued, AuditActions.DocumentIssued);
        (await context.LicenseDocuments.SingleAsync()).KeyId.Should().Be("test");
    }

    [Fact]
    public async Task Basarili_egitimi_olmayan_basvuru_gonderemez()
    {
        var created = await ServiceFor(_pilot2).CreateAsync(new CreateLicenseApplicationRequest(LicenseType.CPL), default);

        var act = () => ServiceFor(_pilot2).SubmitAsync(created.Id, default);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Ayni_turde_ikinci_acik_basvuru_acilamaz()
    {
        await ServiceFor(_pilot1).CreateAsync(new CreateLicenseApplicationRequest(LicenseType.CPL), default);

        var act = () => ServiceFor(_pilot1).CreateAsync(new CreateLicenseApplicationRequest(LicenseType.CPL), default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Gecerli_lisansi_olan_ayni_ture_tekrar_basvuramaz()
    {
        var application = await UnderReviewAsync();
        await ServiceFor(_inspector1).ApproveAsync(application.Id, default);

        var act = () => ServiceFor(_pilot1).CreateAsync(new CreateLicenseApplicationRequest(LicenseType.CPL), default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Baskasinin_basvurusu_gorulemez_ve_gonderilemez()
    {
        var created = await ServiceFor(_pilot1).CreateAsync(new CreateLicenseApplicationRequest(LicenseType.CPL), default);

        await FluentActions.Awaiting(() => ServiceFor(_pilot2).GetAsync(created.Id, default)).Should().ThrowAsync<NotFoundException>();
        await FluentActions.Awaiting(() => ServiceFor(_pilot2).SubmitAsync(created.Id, default)).Should().ThrowAsync<NotFoundException>();
        (await ServiceFor(_pilot2).ListAsync(new LicenseApplicationQuery(), default)).TotalCount.Should().Be(0);
        (await ServiceFor(_inspector1).ListAsync(new LicenseApplicationQuery(), default)).TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Ayni_basvuruya_eszamanli_karar_ikincisinde_concurrency_hatasi_verir()
    {
        var application = await UnderReviewAsync();

        // İki istek başvuruyu aynı anda (aynı Version ile) okur; ilk kaydeden kazanır.
        await using var first = _db.CreateContext();
        await using var second = _db.CreateContext();
        var a1 = await first.LicenseApplications.SingleAsync(a => a.Id == application.Id);
        var a2 = await second.LicenseApplications.SingleAsync(a => a.Id == application.Id);

        a1.Reject(_inspector1.Id, "Sağlık raporu eksik.", TestData.Now);
        await first.SaveChangesAsync();

        second.Licenses.Add(a2.Approve(_inspector1.Id, TestData.Now));
        await FluentActions.Awaiting(() => second.SaveChangesAsync()).Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task Baska_denetcinin_incelemesindeki_basvuruya_karar_verilemez()
    {
        var application = await UnderReviewAsync();

        var act = () => ServiceFor(_inspector2).ApproveAsync(application.Id, default);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("kısa")]
    public void Ret_gerekcesi_en_az_10_karakter_olmali(string reason) =>
        new RejectLicenseApplicationRequestValidator().Validate(new RejectLicenseApplicationRequest(reason))
            .IsValid.Should().BeFalse();

    [Fact]
    public async Task Detay_inceleme_bilgilerini_ve_kanit_egitimi_icerir()
    {
        var application = await UnderReviewAsync();

        application.ReviewerId.Should().Be(_inspector1.Id);
        application.ReviewStartedAtUtc.Should().Be(TestData.Now);
        application.TrainingRecordId.Should().NotBeNull();
    }

    [Theory]
    [InlineData(SortDirection.Asc)]
    [InlineData(SortDirection.Desc)]
    public async Task Liste_istenen_alana_ve_yone_gore_siralanir(SortDirection direction)
    {
        var first = await ServiceFor(_pilot1).CreateAsync(new CreateLicenseApplicationRequest(LicenseType.CPL), default);
        _time.Advance(TimeSpan.FromMinutes(5));
        var second = await ServiceFor(_pilot1).CreateAsync(new CreateLicenseApplicationRequest(LicenseType.ATPL), default);

        var page = await ServiceFor(_inspector1).ListAsync(
            new LicenseApplicationQuery { SortBy = ApplicationSortField.CreatedAt, SortDir = direction }, default);

        page.Items.Select(a => a.Id).Should().Equal(direction == SortDirection.Asc
            ? [first.Id, second.Id]
            : [second.Id, first.Id]);
    }

    public void Dispose() => _db.Dispose();
}
