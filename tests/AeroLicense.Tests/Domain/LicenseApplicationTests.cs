using AeroLicense.Domain.Common;
using AeroLicense.Domain.Entities;
using AeroLicense.Domain.Enums;
using AeroLicense.Tests.TestHelpers;
using FluentAssertions;

namespace AeroLicense.Tests.Domain;

public class LicenseApplicationTests
{
    private static readonly Guid ApplicantId = Guid.NewGuid();
    private static readonly Guid InspectorId = Guid.NewGuid();
    private static readonly DateTime Now = TestData.Now;

    private static TrainingRecord Training(int score = 85, LicenseType type = LicenseType.CPL, Guid? applicantId = null) =>
        new(applicantId ?? ApplicantId, Guid.NewGuid(), Guid.NewGuid(), type, Now.AddDays(-5), score, Now);

    private static LicenseApplication Draft() => new(ApplicantId, LicenseType.CPL, Now);

    private static LicenseApplication UnderReview()
    {
        var application = Draft();
        application.Submit(Training(), Now);
        application.StartReview(InspectorId, Now);
        return application;
    }

    [Fact]
    public void Mutlu_yol_Draft_Submitted_UnderReview_Approved_ve_lisans_uretir()
    {
        var application = UnderReview();

        var license = application.Approve(InspectorId, Now);

        application.Status.Should().Be(ApplicationStatus.Approved);
        license.HolderId.Should().Be(ApplicantId);
        license.ExpiresAtUtc.Should().Be(Now.AddYears(License.ValidityYears));
        license.LicenseNumber.Should().MatchRegex("^AL-CPL-2026-[A-Z2-9]{6}$");
        license.IsValidAt(Now).Should().BeTrue();
    }

    [Fact]
    public void Draft_dogrudan_onaylanamaz()
    {
        var act = () => Draft().Approve(InspectorId, Now);

        act.Should().Throw<DomainException>().WithMessage("*Draft*Approved*");
    }

    [Fact]
    public void Submitted_inceleme_baslamadan_reddedilemez() =>
        FluentActions.Invoking(() =>
        {
            var application = Draft();
            application.Submit(Training(), Now);
            application.Reject(InspectorId, "Eksik belge var, tekrar başvurun.", Now);
        }).Should().Throw<DomainException>();

    [Fact]
    public void Sonuclanmis_basvuru_tekrar_karara_baglanamaz()
    {
        var application = UnderReview();
        application.Reject(InspectorId, "Sağlık raporu eksik.", Now);

        FluentActions.Invoking(() => application.Approve(InspectorId, Now)).Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Ret_gerekcesi_zorunludur(string? reason)
    {
        var application = UnderReview();

        FluentActions.Invoking(() => application.Reject(InspectorId, reason!, Now)).Should().Throw<DomainException>();
        application.Status.Should().Be(ApplicationStatus.UnderReview);
    }

    [Fact]
    public void Basarisiz_egitimle_gonderilemez()
    {
        var application = Draft();

        FluentActions.Invoking(() => application.Submit(Training(score: 69), Now))
            .Should().Throw<DomainException>().WithMessage("*70*");
        application.Status.Should().Be(ApplicationStatus.Draft);
    }

    [Fact]
    public void Baska_kisinin_veya_baska_turun_egitimi_kanit_olamaz()
    {
        FluentActions.Invoking(() => Draft().Submit(Training(applicantId: Guid.NewGuid()), Now)).Should().Throw<DomainException>();
        FluentActions.Invoking(() => Draft().Submit(Training(type: LicenseType.PPL), Now)).Should().Throw<DomainException>();
    }

    [Fact]
    public void Karari_sadece_incelemeyi_ustlenen_denetci_verebilir() =>
        FluentActions.Invoking(() => UnderReview().Approve(Guid.NewGuid(), Now))
            .Should().Throw<DomainException>().WithMessage("*üstlenen*");

    [Fact]
    public void Her_gecis_eszamanlilik_versiyonunu_yeniler()
    {
        var application = Draft();
        var before = application.Version;

        application.Submit(Training(), Now);

        application.Version.Should().NotBe(before);
    }

    [Theory]
    [InlineData(ApplicationStatus.Draft, ApplicationStatus.Submitted, true)]
    [InlineData(ApplicationStatus.Submitted, ApplicationStatus.UnderReview, true)]
    [InlineData(ApplicationStatus.UnderReview, ApplicationStatus.Approved, true)]
    [InlineData(ApplicationStatus.UnderReview, ApplicationStatus.Rejected, true)]
    [InlineData(ApplicationStatus.Draft, ApplicationStatus.Approved, false)]
    [InlineData(ApplicationStatus.Submitted, ApplicationStatus.Approved, false)]
    [InlineData(ApplicationStatus.Rejected, ApplicationStatus.Submitted, false)]
    [InlineData(ApplicationStatus.Approved, ApplicationStatus.Rejected, false)]
    public void Gecis_tablosu(ApplicationStatus from, ApplicationStatus to, bool allowed) =>
        LicenseApplication.CanTransition(from, to).Should().Be(allowed);

    [Fact]
    public void Suresi_dolmus_veya_iptal_edilmis_lisans_gecersizdir()
    {
        var license = UnderReview().Approve(InspectorId, Now);

        license.IsValidAt(Now.AddYears(License.ValidityYears)).Should().BeFalse();
        license.Revoke("Sahte sağlık raporu tespit edildi.", Now);
        license.IsValidAt(Now).Should().BeFalse();
    }
}
