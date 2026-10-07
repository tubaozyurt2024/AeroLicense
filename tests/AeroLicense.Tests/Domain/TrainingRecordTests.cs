using AeroLicense.Domain.Common;
using AeroLicense.Domain.Entities;
using AeroLicense.Domain.Enums;
using AeroLicense.Tests.TestHelpers;
using FluentAssertions;

namespace AeroLicense.Tests.Domain;

public class TrainingRecordTests
{
    private static TrainingRecord Create(int score, DateTime? completedAt = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), LicenseType.PPL,
            completedAt ?? TestData.Now.AddDays(-1), score, TestData.Now);

    [Theory]
    [InlineData(69, false)]
    [InlineData(70, true)] // sınır değer: 70 başarılı sayılır
    [InlineData(100, true)]
    public void Yetmis_ve_uzeri_basarili_sayilir(int score, bool expected) =>
        Create(score).IsPassed.Should().Be(expected);

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Puan_0_100_disinda_olamaz(int score) =>
        FluentActions.Invoking(() => Create(score)).Should().Throw<DomainException>();

    [Fact]
    public void Tamamlanma_tarihi_gelecekte_olamaz() =>
        FluentActions.Invoking(() => Create(80, TestData.Now.AddMinutes(1))).Should().Throw<DomainException>();
}
