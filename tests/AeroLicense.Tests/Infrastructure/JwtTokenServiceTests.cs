using AeroLicense.Domain.Entities;
using AeroLicense.Domain.Enums;
using AeroLicense.Infrastructure.Security;
using AeroLicense.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;

namespace AeroLicense.Tests.Infrastructure;

public class JwtTokenServiceTests
{
    [Fact]
    public void Token_rol_ve_kurulus_icerir_ama_kisisel_veri_icermez()
    {
        var service = new JwtTokenService(
            Options.Create(new JwtOptions { Issuer = "i", Audience = "a", SigningKey = new string('k', 32) }),
            new FakeTimeProvider(TestData.Now));
        var organizationId = Guid.NewGuid();
        var user = new User("havayolu@aerolicense.test", "Entegrasyon", UserRole.Airline, TestData.Now, organizationId);

        var token = new JsonWebToken(service.CreateAccessToken(user).Token);

        token.Subject.Should().Be(user.Id.ToString());
        token.GetClaim("role").Value.Should().Be("Airline");
        token.GetClaim("org_id").Value.Should().Be(organizationId.ToString());
        token.Claims.Select(c => c.Value).Should().NotContain(user.Email); // token herkesçe okunabilir
    }
}
