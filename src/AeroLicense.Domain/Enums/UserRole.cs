namespace AeroLicense.Domain.Enums;

public enum UserRole
{
    Applicant = 1,
    TrainingOrg = 2,
    Airline = 3,
    Inspector = 4
}

/// <summary>[Authorize(Roles = ...)] attribute'u sabit string istediği için enum adlarının sabit karşılıkları.</summary>
public static class Roles
{
    public const string Applicant = nameof(UserRole.Applicant);
    public const string TrainingOrg = nameof(UserRole.TrainingOrg);
    public const string Airline = nameof(UserRole.Airline);
    public const string Inspector = nameof(UserRole.Inspector);
}
