using AeroLicense.Domain.Enums;

namespace AeroLicense.Application.Abstractions;

/// <summary>
/// İsteği yapan kullanıcının token'dan okunan kimliği. Kimlik ve kuruluş bilgisi asla request
/// body'den alınmaz; aksi halde istemci başka biri adına işlem yapabilirdi (parametre manipülasyonu).
/// </summary>
public interface ICurrentUser
{
    Guid UserId { get; }
    UserRole Role { get; }
    Guid? OrganizationId { get; }
}
