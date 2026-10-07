using AeroLicense.Application.Common.Exceptions;
using AeroLicense.Application.Features.Licenses;
using AeroLicense.Domain.Entities;
using AeroLicense.Domain.Enums;
using AeroLicense.Infrastructure.Persistence;
using AeroLicense.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace AeroLicense.Tests.Application;

public sealed class LicenseDocumentTests : IDisposable
{
    private static readonly DateTime Now = TestData.Now;

    private readonly TestDb _db = new();
    private readonly FakeTimeProvider _time = new(Now);
    private readonly User _pilot = new("p1@aerolicense.test", "Pilot Ada Demo", UserRole.Applicant, Now, nationalId: "99999999901");
    private readonly User _otherPilot = new("p2@aerolicense.test", "Pilot Bora", UserRole.Applicant, Now, nationalId: "99999999902");
    private readonly User _inspector = new("i@aerolicense.test", "Denetçi", UserRole.Inspector, Now);
    private readonly License _license;
    private readonly LicenseDocument _document;

    public LicenseDocumentTests()
    {
        using var context = _db.CreateContext();
        foreach (var user in new[] { _pilot, _otherPilot, _inspector }) user.SetPasswordHash("x");
        context.Users.AddRange(_pilot, _otherPilot, _inspector);
        _license = TestData.IssueLicense(context, _pilot, _inspector, Now.AddDays(-30));
        _document = TestServices.DocumentIssuer.Issue(_license, _pilot.FullName, Now.AddDays(-30));
        context.LicenseDocuments.Add(_document);
        context.SaveChanges();
    }

    private DocumentVerificationService Verifier() => new(_db.CreateContext(), TestServices.Signer, _time);

    private LicenseService LicenseServiceFor(User user) =>
        new(_db.CreateContext(), FakeCurrentUser.From(user), _time, TestServices.Links, new FakeQrCodeGenerator());

    // Veritabanına doğrudan erişen bir saldırganı taklit eder: uygulama kurallarını atlayarak satırı değiştirir.
    private async Task TamperAsync(Func<AppDbContext, Task> sql)
    {
        await using var context = _db.CreateContext();
        await sql(context);
    }

    [Fact]
    public async Task Gecerli_belge_minimum_ve_maskeli_bilgiyle_dogrulanir()
    {
        var result = await Verifier().VerifyAsync(_document.VerificationCode, default);

        result.Status.Should().Be(VerificationStatus.Valid);
        result.LicenseNumber.Should().Be(_license.LicenseNumber);
        result.HolderNameMasked.Should().Be("P**** A** D***");
        result.ExpiresAtUtc.Should().Be(_license.ExpiresAtUtc);
    }

    [Fact]
    public async Task Belge_icerigi_degistirilirse_Tampered_doner_ve_icerik_gosterilmez()
    {
        await TamperAsync(c => c.Database.ExecuteSqlRawAsync(
            "UPDATE license_documents SET Content = replace(Content, 'Pilot Ada Demo', 'Sahte Kisi')"));

        var result = await Verifier().VerifyAsync(_document.VerificationCode, default);

        result.Status.Should().Be(VerificationStatus.Tampered);
        result.LicenseNumber.Should().BeNull();
        result.HolderNameMasked.Should().BeNull();
    }

    [Fact]
    public async Task Lisans_kaydinda_bitis_tarihi_uzatilirsa_Tampered_doner()
    {
        await TamperAsync(c => c.Licenses.Where(l => l.Id == _license.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.ExpiresAtUtc, Now.AddYears(10))));

        (await Verifier().VerifyAsync(_document.VerificationCode, default)).Status.Should().Be(VerificationStatus.Tampered);
    }

    [Fact]
    public async Task Suresi_dolmus_lisansin_belgesi_Expired_doner()
    {
        _time.SetUtcNow(_license.ExpiresAtUtc.AddSeconds(1));

        var result = await Verifier().VerifyAsync(_document.VerificationCode, default);

        result.Status.Should().Be(VerificationStatus.Expired);
        result.LicenseNumber.Should().NotBeNull(); // belge gerçek, sadece süresi dolmuş
    }

    [Fact]
    public async Task Iptal_edilen_lisansin_belgesi_Revoked_doner()
    {
        var revoked = await LicenseServiceFor(_inspector)
            .RevokeAsync(_license.Id, new RevokeLicenseRequest("Sahte sağlık raporu tespit edildi."), default);

        revoked.Status.Should().Be(LicenseStatus.Revoked);
        (await Verifier().VerifyAsync(_document.VerificationCode, default)).Status.Should().Be(VerificationStatus.Revoked);
    }

    [Theory]
    [InlineData("KISA")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAA")]    // doğru format ama kayıt yok
    [InlineData("'; DROP TABLE licenses; --xx")] // format kontrolü DB'ye gitmeden reddeder
    public async Task Bilinmeyen_veya_bozuk_kod_NotFound_verir(string code) =>
        await FluentActions.Awaiting(() => Verifier().VerifyAsync(code, default)).Should().ThrowAsync<NotFoundException>();

    [Fact]
    public void Dogrulama_kodu_tahmin_edilemez_ve_benzersizdir()
    {
        var codes = Enumerable.Range(0, 1000)
            .Select(_ => new LicenseDocument(Guid.NewGuid(), "{}", "00", "k", Now).VerificationCode).ToList();

        codes.Should().OnlyHaveUniqueItems();
        codes.Should().AllSatisfy(c => LicenseDocument.IsWellFormedCode(c).Should().BeTrue());
    }

    [Fact]
    public async Task QR_dogrulama_adresini_icerir_ve_sadece_sahibi_ile_denetci_alabilir()
    {
        var qr = await LicenseServiceFor(_pilot).GetQrCodePngAsync(_license.Id, default);

        System.Text.Encoding.UTF8.GetString(qr).Should().Be("https://verify.test/dogrula/" + _document.VerificationCode);
        await FluentActions.Awaiting(() => LicenseServiceFor(_otherPilot).GetQrCodePngAsync(_license.Id, default))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Lisans_listesi_durum_ve_dogrulama_baglantisi_icerir()
    {
        var page = await LicenseServiceFor(_pilot).ListAsync(new LicenseQuery { Status = LicenseStatus.Active }, default);

        page.Items.Should().ContainSingle().Which.VerificationUrl.Should().EndWith(_document.VerificationCode);
        (await LicenseServiceFor(_otherPilot).ListAsync(new LicenseQuery(), default)).TotalCount.Should().Be(0);
    }

    [Fact]
    public void Lisans_tarihleri_saniyeye_yuvarlanir_imzali_icerikle_DB_birebir_eslesir()
    {
        var application = new LicenseApplication(_pilot.Id, LicenseType.PPL, Now);
        application.Submit(new TrainingRecord(_pilot.Id, Guid.NewGuid(), Guid.NewGuid(), LicenseType.PPL, Now.AddDays(-1), 90, Now), Now);
        application.StartReview(_inspector.Id, Now);

        var license = application.Approve(_inspector.Id, Now.AddTicks(1234567));

        license.IssuedAtUtc.Should().Be(Now.AddSeconds(0)); // 0,1234567 sn atıldı
    }

    public void Dispose() => _db.Dispose();
}
