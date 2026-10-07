namespace AeroLicense.Application.Common;

/// <summary>KVKK veri minimizasyonu: kişisel veriler yanıt ve loglarda sadece maskelenmiş halde görünür.</summary>
public static class Masking
{
    // 99999999901 → 999******01 (kişiyi ayırt etmeye yeter, kimlik numarasını açığa çıkarmaz)
    public static string? NationalId(string? value) => value switch
    {
        null => null,
        { Length: 11 } => string.Concat(value.AsSpan(0, 3), "******", value.AsSpan(9)),
        _ => "***"
    };

    // pilot1@aerolicense.test → p***@aerolicense.test
    public static string Email(string value)
    {
        var at = value.IndexOf('@');
        return at < 1 ? "***" : string.Concat(value.AsSpan(0, 1), "***", value.AsSpan(at));
    }

    // "Pilot Ada Demo" → "P**** A** D***": belgeyi elinde tutan kişi ismi eşleştirebilir, üçüncü kişi öğrenemez.
    public static string FullName(string value) =>
        string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => string.Concat(part.AsSpan(0, 1), new string('*', part.Length - 1))));
}
