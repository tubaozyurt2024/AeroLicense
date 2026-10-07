using AeroLicense.Application.Common;
using FluentAssertions;

namespace AeroLicense.Tests.Application;

public class MaskingTests
{
    [Theory]
    [InlineData("99999999901", "999******01")]
    [InlineData("123", "***")]
    [InlineData(null, null)]
    public void Kimlik_no_maskelenir(string? input, string? expected) =>
        Masking.NationalId(input).Should().Be(expected);

    [Theory]
    [InlineData("pilot1@aerolicense.test", "p***@aerolicense.test")]
    [InlineData("gecersiz", "***")]
    public void Eposta_maskelenir(string input, string expected) =>
        Masking.Email(input).Should().Be(expected);
}
