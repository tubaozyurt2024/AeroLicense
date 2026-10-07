namespace AeroLicense.Application.Common;

/// <summary>Audit kayıtlarında kullanılan işlem adları. Sabit olmaları sorgulamayı ve raporlamayı kolaylaştırır.</summary>
public static class AuditActions
{
    public const string TrainingRecordCreated = "TrainingRecord.Created";
    public const string FlightLogCreated = "FlightLog.Created";
    public const string ApplicationCreated = "Application.Created";
    public const string ApplicationSubmitted = "Application.Submitted";
    public const string ApplicationReviewStarted = "Application.ReviewStarted";
    public const string ApplicationApproved = "Application.Approved";
    public const string ApplicationRejected = "Application.Rejected";
    public const string LicenseIssued = "License.Issued";
    public const string LicenseRevoked = "License.Revoked";
    public const string DocumentIssued = "Document.Issued";
}
