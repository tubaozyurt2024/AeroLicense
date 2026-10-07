namespace AeroLicense.Domain.Enums;

/// <summary>Saklanmaz, tarihten ve iptal bilgisinden hesaplanır (süre dolunca bir job'ın güncellemesi gerekmez).</summary>
public enum LicenseStatus
{
    Active = 1,
    Expired = 2,
    Revoked = 3
}
