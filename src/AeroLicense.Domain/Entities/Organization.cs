using AeroLicense.Domain.Common;
using AeroLicense.Domain.Enums;

namespace AeroLicense.Domain.Entities;

public sealed class Organization : Entity
{
    private Organization() { } // EF Core için

    public Organization(string name, OrganizationType type, string code)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Kuruluş adı zorunludur.");
        if (string.IsNullOrWhiteSpace(code)) throw new DomainException("Kuruluş kodu zorunludur.");

        Name = name.Trim();
        Type = type;
        Code = code.Trim().ToUpperInvariant();
    }

    public string Name { get; private set; } = null!;
    public OrganizationType Type { get; private set; }

    /// <summary>Havayolu için 3 harfli (kurgusal) ICAO kodu, eğitim kuruluşu için yetki kodu.</summary>
    public string Code { get; private set; } = null!;
}
