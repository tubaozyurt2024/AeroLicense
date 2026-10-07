using AeroLicense.Domain.Common;
using AeroLicense.Domain.Entities;
using AeroLicense.Tests.TestHelpers;
using FluentAssertions;

namespace AeroLicense.Tests.Domain;

public class FlightLogTests
{
    private static FlightLog Create(DateTime departure, DateTime arrival, string from = "LTFM", string to = "LTAC") =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "XDA101", from, to, departure, arrival, TestData.Now);

    [Fact]
    public void Sure_dakika_olarak_hesaplanir() =>
        Create(TestData.Now.AddHours(-3), TestData.Now.AddHours(-1).AddMinutes(-15)).DurationMinutes.Should().Be(105);

    [Fact]
    public void Ters_saat_reddedilir() =>
        FluentActions.Invoking(() => Create(TestData.Now.AddHours(-1), TestData.Now.AddHours(-2)))
            .Should().Throw<DomainException>().WithMessage("*önce*");

    [Fact]
    public void Gelecekteki_ucus_reddedilir() =>
        FluentActions.Invoking(() => Create(TestData.Now.AddHours(-1), TestData.Now.AddHours(1))).Should().Throw<DomainException>();

    [Fact]
    public void Ayni_havalimanina_ucus_reddedilir() =>
        FluentActions.Invoking(() => Create(TestData.Now.AddHours(-2), TestData.Now.AddHours(-1), "LTFM", "LTFM"))
            .Should().Throw<DomainException>();

    [Fact]
    public void Yirmi_saati_asan_ucus_reddedilir() =>
        FluentActions.Invoking(() => Create(TestData.Now.AddHours(-21), TestData.Now)).Should().Throw<DomainException>();

    [Theory]
    [InlineData(10, 12, 11, 13, true)]  // kısmi çakışma
    [InlineData(10, 14, 11, 12, true)]  // biri diğerini kapsıyor
    [InlineData(10, 12, 12, 14, false)] // uç uca: biri indiği anda diğeri kalkabilir
    [InlineData(10, 11, 12, 13, false)]
    public void Cakisma_kurali(int s1, int e1, int s2, int e2, bool expected)
    {
        var day = TestData.Now.Date;
        FlightLog.Overlaps(day.AddHours(s1), day.AddHours(e1), day.AddHours(s2), day.AddHours(e2)).Should().Be(expected);
    }
}
