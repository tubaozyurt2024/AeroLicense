using AeroLicense.Application.Common.Exceptions;
using AeroLicense.Application.Features.Auth;
using AeroLicense.Infrastructure.Security;
using AeroLicense.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace AeroLicense.Tests.Application;

public sealed class AuthServiceTests : IDisposable
{
    private const string Password = "Dogru-Parola-1";

    private readonly TestDb _db = new();
    private readonly FakeTimeProvider _time = new(TestData.Now);
    private readonly PasswordHasher _hasher = new();
    private readonly JwtTokenService _tokenService = new(
        Options.Create(new JwtOptions
        {
            Issuer = "test",
            Audience = "test",
            SigningKey = new string('k', 32)
        }),
        new FakeTimeProvider(TestData.Now));

    private AuthService CreateService() => new(_db.CreateContext(), _hasher, _tokenService, _time, NullLogger<AuthService>.Instance);

    private async Task SeedUserAsync(bool active = true)
    {
        await using var context = _db.CreateContext();
        var user = TestData.Applicant();
        user.SetPasswordHash(_hasher.Hash(Password));
        if (!active) user.Deactivate();
        context.Users.Add(user);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Dogru_bilgilerle_token_ve_rol_doner()
    {
        await SeedUserAsync();

        // E-posta büyük harfle yazılsa da normalize edilip bulunmalı.
        var response = await CreateService().LoginAsync(new LoginRequest("PILOT@aerolicense.test", Password), default);

        response.AccessToken.Should().NotBeNullOrEmpty();
        response.Role.Should().Be("Applicant");
        response.ExpiresAtUtc.Should().Be(TestData.Now.AddMinutes(15));
    }

    [Fact]
    public async Task Yanlis_parola_ve_olmayan_kullanici_ayni_hatayi_verir()
    {
        await SeedUserAsync();
        var service = CreateService();

        var wrongPassword = () => service.LoginAsync(new LoginRequest("pilot@aerolicense.test", "yanlis"), default);
        var unknownUser = () => service.LoginAsync(new LoginRequest("yok@aerolicense.test", Password), default);

        // Aynı mesaj: hangi e-postanın kayıtlı olduğu dışarıdan anlaşılamamalı.
        var first = await wrongPassword.Should().ThrowAsync<UnauthorizedException>();
        var second = await unknownUser.Should().ThrowAsync<UnauthorizedException>();
        first.Which.Message.Should().Be(second.Which.Message);
    }

    [Fact]
    public async Task Pasif_kullanici_giris_yapamaz()
    {
        await SeedUserAsync(active: false);

        var act = () => CreateService().LoginAsync(new LoginRequest("pilot@aerolicense.test", Password), default);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Theory]
    [InlineData("eposta-degil", "parola")]
    [InlineData("pilot@aerolicense.test", "")]
    public void Validator_gecersiz_girdiyi_reddeder(string email, string password)
    {
        var result = new LoginRequestValidator().Validate(new LoginRequest(email, password));

        result.IsValid.Should().BeFalse();
    }

    private async Task<LoginResponse> LoginAsync()
    {
        await SeedUserAsync();
        return await CreateService().LoginAsync(new LoginRequest("pilot@aerolicense.test", Password), default);
    }

    [Fact]
    public async Task Refresh_token_veritabaninda_acik_degil_ozet_olarak_saklanir()
    {
        var login = await LoginAsync();

        await using var context = _db.CreateContext();
        var stored = await context.RefreshTokens.SingleAsync();
        stored.TokenHash.Should().NotBe(login.RefreshToken).And.HaveLength(64);
        login.RefreshTokenExpiresAtUtc.Should().Be(TestData.Now.AddDays(7));
    }

    [Fact]
    public async Task Refresh_yeni_token_cifti_verir_eskisi_gecersiz_olur()
    {
        var login = await LoginAsync();

        var refreshed = await CreateService().RefreshAsync(new RefreshRequest(login.RefreshToken), default);

        refreshed.RefreshToken.Should().NotBe(login.RefreshToken);
        refreshed.AccessToken.Should().NotBeNullOrEmpty();
        await using var context = _db.CreateContext();
        (await context.RefreshTokens.CountAsync(t => t.RevokedAtUtc == null)).Should().Be(1);
    }

    [Fact]
    public async Task Kullanilmis_token_tekrar_gelirse_tum_aile_iptal_edilir()
    {
        var login = await LoginAsync();
        var refreshed = await CreateService().RefreshAsync(new RefreshRequest(login.RefreshToken), default);

        // Saldırgan çaldığı eski token'ı kullanıyor.
        await FluentActions.Awaiting(() => CreateService().RefreshAsync(new RefreshRequest(login.RefreshToken), default))
            .Should().ThrowAsync<UnauthorizedException>();

        // Artık meşru kullanıcının güncel token'ı da geçersiz: yeniden giriş gerekir.
        await FluentActions.Awaiting(() => CreateService().RefreshAsync(new RefreshRequest(refreshed.RefreshToken), default))
            .Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Suresi_dolmus_refresh_token_reddedilir()
    {
        var login = await LoginAsync();
        _time.Advance(TimeSpan.FromDays(7));

        await FluentActions.Awaiting(() => CreateService().RefreshAsync(new RefreshRequest(login.RefreshToken), default))
            .Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Cikistan_sonra_refresh_token_kullanilamaz()
    {
        var login = await LoginAsync();

        await CreateService().LogoutAsync(new RefreshRequest(login.RefreshToken), default);

        await FluentActions.Awaiting(() => CreateService().RefreshAsync(new RefreshRequest(login.RefreshToken), default))
            .Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Oturumdaki_kullanici_ad_ve_rolle_doner()
    {
        await SeedUserAsync();
        await using var context = _db.CreateContext();
        var id = (await context.Users.SingleAsync()).Id;

        var me = await CreateService().GetCurrentUserAsync(id, default);

        me.FullName.Should().Be("Test Pilot");
        me.Role.Should().Be(AeroLicense.Domain.Enums.UserRole.Applicant);
    }

    [Fact]
    public async Task Bilinmeyen_refresh_token_reddedilir() =>
        await FluentActions.Awaiting(() => CreateService().RefreshAsync(new RefreshRequest("uydurma"), default))
            .Should().ThrowAsync<UnauthorizedException>();

    public void Dispose() => _db.Dispose();
}
