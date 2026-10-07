using AeroLicense.Domain.Common;
using AeroLicense.Domain.Entities;
using AeroLicense.Domain.Enums;
using AeroLicense.Tests.TestHelpers;
using FluentAssertions;

namespace AeroLicense.Tests.Domain;

public class UserTests
{
    [Theory]
    [InlineData(UserRole.TrainingOrg)]
    [InlineData(UserRole.Airline)]
    public void Kurulus_rolu_kurulussuz_olusturulamaz(UserRole role)
    {
        var act = () => new User("a@aerolicense.test", "Ad", role, TestData.Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Basvuru_sahibi_bir_kurulusa_baglanamaz()
    {
        var act = () => new User("a@aerolicense.test", "Ad", UserRole.Applicant, TestData.Now, Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Eposta_kucuk_harfe_ve_bosluksuz_hale_getirilir()
    {
        var user = new User("  Pilot@AeroLicense.TEST ", "Ad", UserRole.Applicant, TestData.Now);

        user.Email.Should().Be("pilot@aerolicense.test");
    }
}
