using AeroLicense.Application.Common.Exceptions;
using AeroLicense.Application.Features.Training;
using AeroLicense.Domain.Entities;
using AeroLicense.Domain.Enums;
using AeroLicense.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace AeroLicense.Tests.Application;

public sealed class TrainingServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly FakeTimeProvider _time = new(TestData.Now);

    private readonly Organization _ato1 = new("ATO 1", OrganizationType.TrainingOrg, "ATO-1");
    private readonly Organization _ato2 = new("ATO 2", OrganizationType.TrainingOrg, "ATO-2");
    private readonly User _pilot1 = new("p1@aerolicense.test", "Pilot 1", UserRole.Applicant, TestData.Now, nationalId: "99999999901");
    private readonly User _pilot2 = new("p2@aerolicense.test", "Pilot 2", UserRole.Applicant, TestData.Now, nationalId: "99999999902");
    private readonly User _inspector = new("i@aerolicense.test", "Denetçi", UserRole.Inspector, TestData.Now);
    private readonly User _ato1User;
    private readonly User _ato2User;

    public TrainingServiceTests()
    {
        _ato1User = new User("t1@aerolicense.test", "ATO 1 Kullanıcı", UserRole.TrainingOrg, TestData.Now, _ato1.Id);
        _ato2User = new User("t2@aerolicense.test", "ATO 2 Kullanıcı", UserRole.TrainingOrg, TestData.Now, _ato2.Id);

        using var context = _db.CreateContext();
        context.Organizations.AddRange(_ato1, _ato2);
        foreach (var user in new[] { _pilot1, _pilot2, _inspector, _ato1User, _ato2User })
            user.SetPasswordHash("x");
        context.Users.AddRange(_pilot1, _pilot2, _inspector, _ato1User, _ato2User);
        context.SaveChanges();
    }

    private TrainingService ServiceFor(User user) => new(_db.CreateContext(), FakeCurrentUser.From(user), _time);

    private Task<TrainingRecordDto> RecordAsync(User trainingOrgUser, string nationalId, int score = 85) =>
        ServiceFor(trainingOrgUser).CreateAsync(
            new CreateTrainingRecordRequest(nationalId, LicenseType.PPL, TestData.Now.AddDays(-3), score), default);

    [Fact]
    public async Task Kayit_tokendaki_kurulus_adina_acilir_ve_kimlik_no_maskelenir()
    {
        var dto = await RecordAsync(_ato1User, "99999999901", score: 69);

        dto.TrainingOrgName.Should().Be("ATO 1");
        dto.ApplicantId.Should().Be(_pilot1.Id);
        dto.ApplicantNationalIdMasked.Should().Be("999******01");
        dto.IsPassed.Should().BeFalse();
    }

    [Fact]
    public async Task Bilinmeyen_kimlik_no_NotFound_verir()
    {
        var act = () => RecordAsync(_ato1User, "12345678901");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Basvuru_sahibi_baskasinin_id_si_ile_sorgulasa_bile_sadece_kendi_kayitlarini_gorur()
    {
        await RecordAsync(_ato1User, "99999999901");
        await RecordAsync(_ato1User, "99999999902");

        // pilot1, pilot2'nin id'sini parametre olarak gönderiyor (IDOR denemesi).
        var result = await ServiceFor(_pilot1).ListAsync(new TrainingRecordQuery { ApplicantId = _pilot2.Id }, default);

        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Egitim_kurulusu_sadece_kendi_kayitlarini_denetci_hepsini_gorur()
    {
        await RecordAsync(_ato1User, "99999999901");
        await RecordAsync(_ato2User, "99999999901");
        await RecordAsync(_ato2User, "99999999902", score: 50);

        (await ServiceFor(_ato1User).ListAsync(new TrainingRecordQuery(), default)).TotalCount.Should().Be(1);
        (await ServiceFor(_inspector).ListAsync(new TrainingRecordQuery(), default)).TotalCount.Should().Be(3);
        (await ServiceFor(_inspector).ListAsync(new TrainingRecordQuery { PassedOnly = true }, default))
            .TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Sayfalama_istenen_sayfayi_ve_toplam_sayiyi_doner()
    {
        for (var i = 0; i < 5; i++) await RecordAsync(_ato1User, "99999999901");

        var page = await ServiceFor(_inspector).ListAsync(new TrainingRecordQuery { Page = 2, PageSize = 2 }, default);

        page.Items.Should().HaveCount(2);
        page.TotalCount.Should().Be(5);
        page.TotalPages.Should().Be(3);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 101)] // üst sınır aşılamaz
    public void Sayfa_parametreleri_dogrulanir(int page, int pageSize) =>
        new TrainingRecordQueryValidator().Validate(new TrainingRecordQuery { Page = page, PageSize = pageSize })
            .IsValid.Should().BeFalse();

    [Fact]
    public void Gelecek_tarihli_egitim_kaydi_istegi_reddedilir() =>
        new CreateTrainingRecordRequestValidator(_time)
            .Validate(new CreateTrainingRecordRequest("99999999901", LicenseType.PPL, TestData.Now.AddDays(1), 80))
            .IsValid.Should().BeFalse();

    public void Dispose() => _db.Dispose();
}
