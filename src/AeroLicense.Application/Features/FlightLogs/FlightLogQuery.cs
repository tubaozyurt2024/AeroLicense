using AeroLicense.Application.Common.Pagination;
using FluentValidation;

namespace AeroLicense.Application.Features.FlightLogs;

public sealed class FlightLogQuery : PageQuery
{
    /// <summary>Sadece şüpheli kayıtlar: süre ≥ 12 saat veya uçuştan 30+ gün sonra bildirilmiş.</summary>
    public bool Suspicious { get; init; }
    public string? PilotLicenseNumber { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
}

public enum SuspicionReason
{
    LongDuration = 1,
    LateReport = 2
}

/// <param name="PilotName">Sadece denetçiye dolu döner; havayolu pilotu zaten lisans numarasıyla tanır (veri minimizasyonu).</param>
public sealed record FlightLogListItemDto(
    Guid Id,
    string PilotLicenseNumber,
    string? PilotName,
    string AirlineCode,
    string FlightNumber,
    string DepartureAirport,
    string ArrivalAirport,
    DateTime DepartureAtUtc,
    DateTime ArrivalAtUtc,
    int DurationMinutes,
    DateTime RecordedAtUtc,
    IReadOnlyList<SuspicionReason> SuspicionReasons);

public sealed class FlightLogQueryValidator : PageQueryValidator<FlightLogQuery>
{
    public FlightLogQueryValidator()
    {
        RuleFor(x => x.PilotLicenseNumber).MaximumLength(32);
        RuleFor(x => x.ToUtc).GreaterThanOrEqualTo(x => x.FromUtc).When(x => x.FromUtc is not null && x.ToUtc is not null)
            .WithMessage("Bitiş tarihi başlangıçtan önce olamaz.");
    }
}
