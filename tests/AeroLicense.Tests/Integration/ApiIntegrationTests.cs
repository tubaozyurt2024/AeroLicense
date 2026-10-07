using System.Net;
using System.Net.Http.Json;
using AeroLicense.Application.Common.Pagination;
using AeroLicense.Application.Features.Applications;
using AeroLicense.Application.Features.Auth;
using AeroLicense.Application.Features.FlightLogs;
using AeroLicense.Application.Features.Licenses;
using AeroLicense.Domain.Enums;
using AeroLicense.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AeroLicense.Tests.Integration;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ApiIntegrationTests(ApiFixture fixture)
{
    private const string V1 = "/api/v1";

    private void SkipIfUnavailable() => Skip.If(fixture.SkipReason is not null, fixture.SkipReason);

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json.Options))!;

    [SkippableFact]
    public async Task Basvurudan_dogrulanabilir_belgeye_uctan_uca_akis()
    {
        SkipIfUnavailable();
        var pilot = await fixture.LoginAsync("pilot1");
        var inspector = await fixture.LoginAsync("inspector");

        // Seed: pilot1'in başarılı CPL eğitimi ve taslak CPL başvurusu var.
        var drafts = await ReadAsync<PagedResult<LicenseApplicationDto>>(
            await pilot.GetAsync($"{V1}/license-applications?status=Draft"));
        var id = drafts.Items.Single(a => a.LicenseType == LicenseType.CPL).Id;

        (await pilot.PostAsync($"{V1}/license-applications/{id}/submit", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await inspector.PostAsync($"{V1}/license-applications/{id}/start-review", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        var approved = await ReadAsync<LicenseApplicationDto>(await inspector.PostAsync($"{V1}/license-applications/{id}/approve", null));
        approved.Status.Should().Be(ApplicationStatus.Approved);

        var license = (await ReadAsync<PagedResult<LicenseDto>>(await pilot.GetAsync($"{V1}/licenses")))
            .Items.Single(l => l.LicenseNumber == approved.LicenseNumber);

        // Girişsiz doğrulama
        var verification = await ReadAsync<VerificationResultDto>(await fixture.Anonymous().GetAsync($"{V1}/verify/{license.VerificationCode}"));
        verification.Status.Should().Be(VerificationStatus.Valid);
        verification.HolderNameMasked.Should().Be("P**** A** D***");

        var qr = await pilot.GetAsync($"{V1}/licenses/{license.Id}/qr");
        qr.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
    }

    [SkippableFact]
    public async Task Ayni_idempotency_key_ile_eszamanli_istekler_tek_kayit_olusturur()
    {
        SkipIfUnavailable();
        var inspector = await fixture.LoginAsync("inspector");
        var airline = await fixture.LoginAsync("havayolu1");
        var licenseNumber = (await ReadAsync<PagedResult<LicenseDto>>(await inspector.GetAsync($"{V1}/licenses?status=Active&pageSize=100")))
            .Items.First(l => l.HolderName == "Pilot Ada Demo" && l.Type == LicenseType.PPL).LicenseNumber;

        var departure = DateTime.UtcNow.Date.AddDays(-1).AddHours(14);
        var request = new CreateFlightLogRequest(licenseNumber, "XDA999", "LTFM", "LTAC", departure, departure.AddMinutes(75));
        var key = Guid.NewGuid().ToString();

        // Sırayla değil, gerçekten aynı anda: servisteki ön kontrolü hepsi birlikte geçer, unique index ayırır.
        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
        {
            var message = new HttpRequestMessage(HttpMethod.Post, $"{V1}/flight-logs") { Content = JsonContent.Create(request) };
            message.Headers.Add("Idempotency-Key", key);
            return airline.SendAsync(message);
        }));

        responses.Select(r => r.StatusCode).Should().OnlyContain(s => s == HttpStatusCode.Created || s == HttpStatusCode.OK)
            .And.ContainSingle(s => s == HttpStatusCode.Created);
        var ids = await Task.WhenAll(responses.Select(async r => (await ReadAsync<FlightLogDto>(r)).Id));
        ids.Distinct().Should().ContainSingle();

        await using var scope = fixture.Factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.FlightLogs.CountAsync(f => f.FlightNumber == "XDA999")).Should().Be(1);
    }

    [SkippableFact]
    public async Task Audit_kaydi_dogrudan_SQL_ile_bile_degistirilemez()
    {
        SkipIfUnavailable();
        await using var scope = fixture.Factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.AuditLogs.AnyAsync()).Should().BeTrue("seed ve önceki testler audit kaydı üretti");

        var update = () => db.Database.ExecuteSqlRawAsync("UPDATE audit_logs SET \"Details\" = 'x'");
        var delete = () => db.Database.ExecuteSqlRawAsync("DELETE FROM audit_logs");

        (await update.Should().ThrowAsync<PostgresException>()).Which.MessageText.Should().Contain("append-only");
        await delete.Should().ThrowAsync<PostgresException>();
    }

    [SkippableFact]
    public async Task Refresh_token_rotasyonu_ve_tekrar_kullanim_tespiti_HTTP_uzerinden()
    {
        SkipIfUnavailable();
        var client = fixture.Anonymous();
        var login = await ReadAsync<LoginResponse>(await client.PostAsJsonAsync($"{V1}/auth/login",
            new LoginRequest("pilot2@aerolicense.test", ApiFixture.DemoPassword)));

        var refreshed = await client.PostAsJsonAsync($"{V1}/auth/refresh", new RefreshRequest(login.RefreshToken));
        refreshed.StatusCode.Should().Be(HttpStatusCode.OK);
        var next = await ReadAsync<LoginResponse>(refreshed);

        (await client.PostAsJsonAsync($"{V1}/auth/refresh", new RefreshRequest(login.RefreshToken)))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync($"{V1}/auth/refresh", new RefreshRequest(next.RefreshToken)))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized, "tekrar kullanım tüm aileyi iptal etti");
    }

    [SkippableFact]
    public async Task Hatalar_ProblemDetails_formatinda_ve_correlation_id_ile_doner()
    {
        SkipIfUnavailable();
        var message = new HttpRequestMessage(HttpMethod.Get, $"{V1}/license-applications");
        message.Headers.Add("X-Correlation-ID", "it-test-123");

        var response = await fixture.Anonymous().SendAsync(message);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        response.Headers.GetValues("X-Correlation-ID").Should().Equal("it-test-123");
    }

    [SkippableFact]
    public async Task Dogrulama_sayfasi_guvenlik_basliklariyla_sunulur()
    {
        SkipIfUnavailable();

        var response = await fixture.Anonymous().GetAsync("/dogrula/ABCDEFGHJKMNPQRSTVWXYZ2345");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
        response.Headers.GetValues("Content-Security-Policy").Single().Should().Contain("script-src 'self' https://cdnjs.cloudflare.com")
            .And.NotContain("unsafe-inline");
        response.Headers.GetValues("X-Frame-Options").Should().Equal("DENY");
        response.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
    }

    [SkippableFact]
    public async Task Swagger_semasi_null_olamayan_alanlari_required_isaretler()
    {
        SkipIfUnavailable();
        // Swagger sadece Development'ta; şema üretimini doğrudan servis üzerinden alıyoruz.
        var provider = fixture.Factory!.Services.GetRequiredService<Swashbuckle.AspNetCore.Swagger.ISwaggerProvider>();
        var schemas = provider.GetSwagger("v1").Components!.Schemas!;

        var dto = schemas["LicenseApplicationDto"];
        dto.Required.Should().Contain(["id", "applicantName", "status", "createdAtUtc"])
            .And.NotContain(["reviewerId", "rejectionReason", "licenseNumber"]);
    }

    [SkippableFact]
    public async Task Health_check_veritabani_ile_birlikte_saglikli()
    {
        SkipIfUnavailable();
        (await fixture.Anonymous().GetAsync("/health")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
