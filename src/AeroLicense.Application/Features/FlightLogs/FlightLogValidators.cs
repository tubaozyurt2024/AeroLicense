using FluentValidation;

namespace AeroLicense.Application.Features.FlightLogs;

public sealed class CreateFlightLogRequestValidator : AbstractValidator<CreateFlightLogRequest>
{
    public const int MaxIdempotencyKeyLength = 64;

    public CreateFlightLogRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.PilotLicenseNumber).NotEmpty().MaximumLength(32);

        // IATA/ICAO havayolu kodu (2-3 karakter) + 1-4 haneli numara + isteğe bağlı sonek harfi: TK1, XDA1234A
        RuleFor(x => x.FlightNumber).NotEmpty()
            .Matches("^[A-Z0-9]{2,3}[0-9]{1,4}[A-Z]?$").WithMessage("Uçuş numarası formatı geçersiz (ör. XDA123).");

        // ICAO havalimanı kodu: 4 büyük harf (ör. LTFM). Kaynağında büyük harf istenir; sessizce düzeltmeyiz.
        RuleFor(x => x.DepartureAirport).NotEmpty().Matches("^[A-Z]{4}$").WithMessage("ICAO havalimanı kodu 4 büyük harf olmalıdır.");
        RuleFor(x => x.ArrivalAirport).NotEmpty().Matches("^[A-Z]{4}$").WithMessage("ICAO havalimanı kodu 4 büyük harf olmalıdır.")
            .NotEqual(x => x.DepartureAirport).WithMessage("Kalkış ve varış havalimanı aynı olamaz.");

        // Saat dilimi belirsizliği kabul edilmez: "2026-09-01T10:00:00" yerel mi UTC mi bilinemez, "Z" zorunlu.
        RuleFor(x => x.DepartureAtUtc).Must(d => d.Kind == DateTimeKind.Utc).WithMessage("Zaman UTC olmalıdır (ör. 2026-09-01T10:00:00Z).");
        RuleFor(x => x.ArrivalAtUtc).Must(d => d.Kind == DateTimeKind.Utc).WithMessage("Zaman UTC olmalıdır (ör. 2026-09-01T12:00:00Z).")
            .GreaterThan(x => x.DepartureAtUtc).WithMessage("Varış zamanı kalkıştan sonra olmalıdır.")
            .Must(d => d <= timeProvider.GetUtcNow().UtcDateTime).WithMessage("Henüz gerçekleşmemiş bir uçuş kaydedilemez.");
    }

    public static bool IsValidIdempotencyKey(string? key) =>
        !string.IsNullOrEmpty(key)
        && key.Length <= MaxIdempotencyKeyLength
        && key.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
