namespace AeroLicense.Application.Features.FlightLogs;

// Havayolu pilotu lisans numarasıyla tanır; sistem içi Guid'i bilmesi beklenmez.
public sealed record CreateFlightLogRequest(
    string PilotLicenseNumber,
    string FlightNumber,
    string DepartureAirport,
    string ArrivalAirport,
    DateTime DepartureAtUtc,
    DateTime ArrivalAtUtc);

// Veri minimizasyonu: havayoluna pilotun adı/kimliği dönülmez, zaten bildiği bilgiler yeterli.
public sealed record FlightLogDto(
    Guid Id,
    string PilotLicenseNumber,
    string FlightNumber,
    string DepartureAirport,
    string ArrivalAirport,
    DateTime DepartureAtUtc,
    DateTime ArrivalAtUtc,
    int DurationMinutes,
    DateTime RecordedAtUtc);

/// <param name="Replayed">true ise yeni kayıt oluşmadı; aynı Idempotency-Key ile önceki sonuç döndü.</param>
public sealed record CreateFlightLogResult(FlightLogDto FlightLog, bool Replayed);

public sealed record PilotSummaryDto(
    Guid PilotId,
    int TotalFlights,
    decimal TotalHours,
    decimal Last30DaysHours,
    decimal Last90DaysHours,
    DateTime? LastFlightAtUtc);
