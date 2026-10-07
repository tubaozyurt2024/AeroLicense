using AeroLicense.Application.Common.Exceptions;
using AeroLicense.Application.Features.FlightLogs;
using AeroLicense.Domain.Common;
using AeroLicense.Domain.Entities;
using AeroLicense.Domain.Enums;
using AeroLicense.Tests.TestHelpers;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace AeroLicense.Tests.Application;

public sealed class FlightLogServiceTests : IDisposable
{
    private static readonly DateTime Now = TestData.Now;

    private readonly TestDb _db = new();
    private readonly FakeTimeProvider _time = new(Now);

    private readonly Organization _airline1 = new("Havayolu 1", OrganizationType.Airline, "XDA");
    private readonly Organization _airline2 = new("Havayolu 2", OrganizationType.Airline, "XDB");
    private readonly User _pilot = new("p1@aerolicense.test", "Pilot 1", UserRole.Applicant, Now, nationalId: "99999999901");
    private readonly User _expiredPilot = new("p2@aerolicense.test", "Pilot 2", UserRole.Applicant, Now, nationalId: "99999999902");
    private readonly User _inspector = new("i@aerolicense.test", "Denetçi", UserRole.Inspector, Now);
    private readonly User _airline1User;
    private readonly User _airline2User;
    private readonly string _licenseNumber;
    private readonly string _expiredLicenseNumber;

    public FlightLogServiceTests()
    {
        _airline1User = new User("a1@aerolicense.test", "XDA", UserRole.Airline, Now, _airline1.Id);
        _airline2User = new User("a2@aerolicense.test", "XDB", UserRole.Airline, Now, _airline2.Id);

        using var context = _db.CreateContext();
        context.Organizations.AddRange(_airline1, _airline2);
        foreach (var user in new[] { _pilot, _expiredPilot, _inspector, _airline1User, _airline2User })
            user.SetPasswordHash("x");
        context.Users.AddRange(_pilot, _expiredPilot, _inspector, _airline1User, _airline2User);
        _licenseNumber = TestData.IssueLicense(context, _pilot, _inspector, Now.AddDays(-365)).LicenseNumber;
        _expiredLicenseNumber = TestData.IssueLicense(context, _expiredPilot, _inspector, Now.AddYears(-3)).LicenseNumber;
        context.SaveChanges();
    }

    private FlightLogService ServiceFor(User user) => new(_db.CreateContext(), FakeCurrentUser.From(user), _time);

    private CreateFlightLogRequest Flight(double hoursAgo, double durationHours = 1.5, string? license = null,
        string flightNumber = "XDA101") =>
        new(license ?? _licenseNumber, flightNumber, "LTFM", "LTAC",
            Now.AddHours(-hoursAgo), Now.AddHours(-hoursAgo + durationHours));

    private Task<CreateFlightLogResult> PostAsync(CreateFlightLogRequest request, string key = "key-1", User? airlineUser = null) =>
        ServiceFor(airlineUser ?? _airline1User).CreateAsync(request, key, default);

    private async Task<int> FlightCountAsync()
    {
        await using var context = _db.CreateContext();
        return await context.FlightLogs.CountAsync();
    }

    [Fact]
    public async Task Gecerli_ucus_kaydedilir()
    {
        var result = await PostAsync(Flight(hoursAgo: 5));

        result.Replayed.Should().BeFalse();
        result.FlightLog.DurationMinutes.Should().Be(90);
        result.FlightLog.PilotLicenseNumber.Should().Be(_licenseNumber);
    }

    [Fact]
    public async Task Ayni_anahtar_ve_ayni_icerik_ilk_sonucu_doner_ikinci_kayit_olusmaz()
    {
        var first = await PostAsync(Flight(hoursAgo: 5));

        var second = await PostAsync(Flight(hoursAgo: 5));

        second.Replayed.Should().BeTrue();
        second.FlightLog.Should().BeEquivalentTo(first.FlightLog);
        (await FlightCountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Ayni_anahtar_farkli_icerikle_reddedilir()
    {
        await PostAsync(Flight(hoursAgo: 5));

        var act = () => PostAsync(Flight(hoursAgo: 30));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*Idempotency-Key*");
        (await FlightCountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Anahtar_havayolu_kapsamindadir_farkli_havayollari_ayni_anahtari_kullanabilir()
    {
        await PostAsync(Flight(hoursAgo: 5), "shared-key");

        var other = await PostAsync(Flight(hoursAgo: 30, flightNumber: "XDB200"), "shared-key", _airline2User);

        other.Replayed.Should().BeFalse();
        (await FlightCountAsync()).Should().Be(2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("geçersiz karakter!")]
    public async Task Idempotency_key_zorunlu_ve_formatli_olmali(string? key)
    {
        var act = () => ServiceFor(_airline1User).CreateAsync(Flight(hoursAgo: 5), key, default);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Basarisiz_istek_anahtari_harcamaz_duzeltilmis_istek_ayni_anahtarla_gonderilebilir()
    {
        await FluentActions.Awaiting(() => PostAsync(Flight(hoursAgo: 5, license: "AL-YOK"))).Should().ThrowAsync<DomainException>();

        var result = await PostAsync(Flight(hoursAgo: 5));

        result.Replayed.Should().BeFalse();
    }

    [Fact]
    public async Task Suresi_dolmus_lisansla_ucus_reddedilir() =>
        await FluentActions.Awaiting(() => PostAsync(Flight(hoursAgo: 5, license: _expiredLicenseNumber)))
            .Should().ThrowAsync<DomainException>().WithMessage("*geçerli bir lisans*");

    [Fact]
    public async Task Olmayan_lisans_numarasiyla_ucus_reddedilir() =>
        await FluentActions.Awaiting(() => PostAsync(Flight(hoursAgo: 5, license: "AL-PPL-2026-YOKYOK")))
            .Should().ThrowAsync<DomainException>();

    [Fact]
    public async Task Lisans_suresi_dolmadan_once_yapilmis_ucus_sonradan_kaydedilebilir()
    {
        // Lisans 3 yıl önce verildi, 1 yıl önce doldu: 2,5 yıl önceki uçuş o tarihte geçerliydi.
        var result = await PostAsync(Flight(hoursAgo: 24 * 365 * 2.5, license: _expiredLicenseNumber));

        result.FlightLog.DurationMinutes.Should().Be(90);
    }

    [Fact]
    public async Task Ayni_pilotun_cakisan_ucusu_baska_havayolundan_bile_gelse_reddedilir()
    {
        await PostAsync(Flight(hoursAgo: 5, durationHours: 2), "k1");

        var act = () => PostAsync(Flight(hoursAgo: 4, durationHours: 2, flightNumber: "XDB200"), "k2", _airline2User);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*çakışan*");
    }

    [Fact]
    public async Task Uc_uca_ucuslar_cakisma_sayilmaz()
    {
        await PostAsync(Flight(hoursAgo: 5, durationHours: 2), "k1");

        var result = await PostAsync(Flight(hoursAgo: 3, durationHours: 1, flightNumber: "XDA102"), "k2");

        result.Replayed.Should().BeFalse();
    }

    [Fact]
    public async Task Ters_saatli_ucus_reddedilir() =>
        await FluentActions.Awaiting(() => PostAsync(Flight(hoursAgo: 5, durationHours: -1)))
            .Should().ThrowAsync<DomainException>();

    [Fact]
    public async Task Ozet_toplam_ve_son_30_90_gun_saatlerini_hesaplar()
    {
        await PostAsync(Flight(hoursAgo: 24 * 5, durationHours: 1), "k1");     // 5 gün önce, 60 dk
        await PostAsync(Flight(hoursAgo: 24 * 45, durationHours: 2), "k2");    // 45 gün önce, 120 dk
        await PostAsync(Flight(hoursAgo: 24 * 200, durationHours: 1.5), "k3"); // 200 gün önce, 90 dk

        var summary = await ServiceFor(_inspector).GetPilotSummaryAsync(_pilot.Id, default);

        summary.TotalFlights.Should().Be(3);
        summary.TotalHours.Should().Be(4.5m);
        summary.Last30DaysHours.Should().Be(1m);
        summary.Last90DaysHours.Should().Be(3m);
        summary.LastFlightAtUtc.Should().Be(Now.AddDays(-5).AddHours(1));
    }

    [Fact]
    public async Task Ucusu_olmayan_pilotun_ozeti_sifirdir() =>
        (await ServiceFor(_pilot).GetPilotSummaryAsync(_pilot.Id, default))
            .Should().BeEquivalentTo(new PilotSummaryDto(_pilot.Id, 0, 0, 0, 0, null));

    [Fact]
    public async Task Pilot_baskasinin_ozetini_goremez() =>
        await FluentActions.Awaiting(() => ServiceFor(_pilot).GetPilotSummaryAsync(_expiredPilot.Id, default))
            .Should().ThrowAsync<NotFoundException>();

    [Theory]
    [InlineData("LTF", "LTAC")]    // 3 harf
    [InlineData("ltfm", "LTAC")]   // küçük harf
    [InlineData("LTFM", "LTFM")]   // aynı havalimanı
    public void Havalimani_kodu_ICAO_formatinda_olmali(string from, string to) =>
        new CreateFlightLogRequestValidator(_time)
            .Validate(Flight(hoursAgo: 5) with { DepartureAirport = from, ArrivalAirport = to })
            .IsValid.Should().BeFalse();

    [Fact]
    public void UTC_olmayan_zaman_reddedilir() =>
        new CreateFlightLogRequestValidator(_time)
            .Validate(Flight(hoursAgo: 5) with { DepartureAtUtc = DateTime.SpecifyKind(Now.AddHours(-5), DateTimeKind.Unspecified) })
            .IsValid.Should().BeFalse();

    [Fact]
    public void Gecerli_istek_validasyondan_gecer() =>
        new CreateFlightLogRequestValidator(_time).Validate(Flight(hoursAgo: 5)).IsValid.Should().BeTrue();

    private async Task SeedRecordedAsync(double hoursAgo, double durationHours, DateTime recordedAtUtc, User airlineUser)
    {
        // Servis kayıt zamanını "şimdi" alır; geç bildirim senaryosu için entity doğrudan eklenir.
        await using var context = _db.CreateContext();
        var license = await context.Licenses.SingleAsync(l => l.LicenseNumber == _licenseNumber);
        var departure = Now.AddHours(-hoursAgo);
        context.FlightLogs.Add(new FlightLog(_pilot.Id, license.Id, airlineUser.OrganizationId!.Value, airlineUser.Id,
            "XDA900", "LTFM", "LTAC", departure, departure.AddHours(durationHours), recordedAtUtc));
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Havayolu_sadece_kendi_kayitlarini_gorur_ve_pilot_adi_ona_donmez()
    {
        await PostAsync(Flight(hoursAgo: 5), "k1");
        await PostAsync(Flight(hoursAgo: 30, flightNumber: "XDB200"), "k2", _airline2User);

        var airlinePage = await ServiceFor(_airline1User).ListAsync(new FlightLogQuery(), default);
        var inspectorPage = await ServiceFor(_inspector).ListAsync(new FlightLogQuery(), default);

        airlinePage.Items.Should().ContainSingle().Which.Should().Match<FlightLogListItemDto>(f =>
            f.AirlineCode == "XDA" && f.PilotName == null);
        inspectorPage.TotalCount.Should().Be(2);
        inspectorPage.Items.Should().OnlyContain(f => f.PilotName == "Pilot 1");
    }

    [Fact]
    public async Task Supheli_filtresi_uzun_ve_gec_bildirilen_ucuslari_getirir()
    {
        await PostAsync(Flight(hoursAgo: 5), "normal");                                      // normal
        await SeedRecordedAsync(hoursAgo: 24 * 3, durationHours: 13, Now, _airline1User);     // 13 saat
        await SeedRecordedAsync(hoursAgo: 24 * 60, durationHours: 1, Now, _airline1User);     // 60 gün geç

        var suspicious = await ServiceFor(_inspector).ListAsync(new FlightLogQuery { Suspicious = true }, default);

        suspicious.TotalCount.Should().Be(2);
        suspicious.Items.Select(f => f.SuspicionReasons.Single()).Should()
            .BeEquivalentTo([SuspicionReason.LongDuration, SuspicionReason.LateReport]);
    }

    [Fact]
    public async Task Otuz_gun_icinde_bildirilen_ucus_supheli_degildir()
    {
        await SeedRecordedAsync(hoursAgo: 24 * 20, durationHours: 1, Now, _airline1User);

        (await ServiceFor(_inspector).ListAsync(new FlightLogQuery { Suspicious = true }, default)).TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Pilot_ucus_listesine_erisemez() =>
        await FluentActions.Awaiting(() => ServiceFor(_pilot).ListAsync(new FlightLogQuery(), default))
            .Should().ThrowAsync<ForbiddenException>();

    public void Dispose() => _db.Dispose();
}
