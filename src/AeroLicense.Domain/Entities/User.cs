using AeroLicense.Domain.Common;
using AeroLicense.Domain.Enums;

namespace AeroLicense.Domain.Entities;

public sealed class User : Entity
{
    private User() { } // EF Core için

    public User(string email, string fullName, UserRole role, DateTime createdAtUtc,
        Guid? organizationId = null, string? nationalId = null)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("E-posta zorunludur.");
        if (string.IsNullOrWhiteSpace(fullName)) throw new DomainException("Ad soyad zorunludur.");

        // Kuruluş adına işlem yapan roller bir kuruluşa bağlı olmak zorunda; diğerleri olamaz.
        var needsOrganization = role is UserRole.TrainingOrg or UserRole.Airline;
        if (needsOrganization && organizationId is null)
            throw new DomainException($"{role} rolündeki kullanıcı bir kuruluşa bağlı olmalıdır.");
        if (!needsOrganization && organizationId is not null)
            throw new DomainException($"{role} rolündeki kullanıcı bir kuruluşa bağlanamaz.");

        Email = NormalizeEmail(email);
        FullName = fullName.Trim();
        Role = role;
        OrganizationId = organizationId;
        NationalId = nationalId;
        CreatedAtUtc = createdAtUtc;
        IsActive = true;
    }

    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string FullName { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public Organization? Organization { get; private set; }

    /// <summary>Kişisel veri (KVKK). DTO'larda ve loglarda yalnızca maskelenmiş hali gösterilir.</summary>
    public string? NationalId { get; private set; }

    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    // Hash'i domain üretmez (kripto altyapı işidir); sadece saklar.
    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;

    public void Deactivate() => IsActive = false;

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
