using AeroLicense.Domain.Common;

namespace AeroLicense.Domain.Entities;

/// <summary>Havayolunun gönderdiği tek bir uçuş kaydı (dijital logbook satırı).</summary>
public sealed class FlightLog : Entity
{
    // Dünyanın en uzun tarifeli uçuşu ~19 saat; üstü büyük olasılıkla hatalı veri.
    public static readonly TimeSpan MaxDuration = TimeSpan.FromHours(20);

    // "Şüpheli" = geçersiz değil ama denetçinin bakması gereken kayıt. Geçersiz olanlar (çakışma, lisanssız
    // pilot, ters saat) zaten kayıt anında reddedilir; bunlar ise geçerli ama olağandışı.
    public const int SuspiciousDurationMinutes = 12 * 60;
    public const int LateReportDays = 30;

    private FlightLog() { } // EF Core için

    public FlightLog(Guid pilotId, Guid licenseId, Guid airlineId, Guid recordedByUserId, string flightNumber,
        string departureAirport, string arrivalAirport, DateTime departureAtUtc, DateTime arrivalAtUtc, DateTime nowUtc)
    {
        if (departureAtUtc >= arrivalAtUtc)
            throw new DomainException("Kalkış zamanı varış zamanından önce olmalıdır.");
        if (arrivalAtUtc - departureAtUtc > MaxDuration)
            throw new DomainException($"Uçuş süresi {MaxDuration.TotalHours} saati aşamaz.");
        if (arrivalAtUtc > nowUtc)
            throw new DomainException("Henüz gerçekleşmemiş bir uçuş kaydedilemez.");
        if (string.Equals(departureAirport, arrivalAirport, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Kalkış ve varış havalimanı aynı olamaz.");

        PilotId = pilotId;
        LicenseId = licenseId;
        AirlineId = airlineId;
        RecordedByUserId = recordedByUserId;
        FlightNumber = flightNumber.ToUpperInvariant();
        DepartureAirport = departureAirport.ToUpperInvariant();
        ArrivalAirport = arrivalAirport.ToUpperInvariant();
        DepartureAtUtc = departureAtUtc;
        ArrivalAtUtc = arrivalAtUtc;
        DurationMinutes = (int)(arrivalAtUtc - departureAtUtc).TotalMinutes;
        RecordedAtUtc = nowUtc;
    }

    public Guid PilotId { get; private set; }
    public User Pilot { get; private set; } = null!;

    /// <summary>Uçuş anında geçerli olan lisans: hangi yetkiyle uçulduğu sonradan da bilinir.</summary>
    public Guid LicenseId { get; private set; }
    public License License { get; private set; } = null!;

    public Guid AirlineId { get; private set; }
    public Organization Airline { get; private set; } = null!;
    public Guid RecordedByUserId { get; private set; }

    public string FlightNumber { get; private set; } = null!;
    public string DepartureAirport { get; private set; } = null!;
    public string ArrivalAirport { get; private set; } = null!;
    public DateTime DepartureAtUtc { get; private set; }
    public DateTime ArrivalAtUtc { get; private set; }

    /// <summary>
    /// Saklanan türetilmiş değer: özet sorgusu tarih aritmetiği yapmadan doğrudan SUM alır. Değer sadece
    /// constructor'da hesaplanır ve setter private olduğu için zamanlarla çelişemez.
    /// </summary>
    public int DurationMinutes { get; private set; }

    public DateTime RecordedAtUtc { get; private set; }

    public bool IsLongDuration => DurationMinutes >= SuspiciousDurationMinutes;
    public bool IsLateReport => RecordedAtUtc > ArrivalAtUtc.AddDays(LateReportDays);

    /// <summary>İki zaman aralığı çakışıyor mu? (uç uca eklenen uçuşlar çakışma sayılmaz)</summary>
    public static bool Overlaps(DateTime start1, DateTime end1, DateTime start2, DateTime end2) =>
        start1 < end2 && start2 < end1;
}
