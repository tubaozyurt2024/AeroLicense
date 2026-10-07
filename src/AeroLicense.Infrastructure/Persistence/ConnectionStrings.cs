using Npgsql;

namespace AeroLicense.Infrastructure.Persistence;

/// <summary>
/// Barındırma servisleri (Render, Heroku, Railway) veritabanı adresini URI olarak verir:
/// <c>postgresql://kullanici:parola@host:port/veritabani</c>. Npgsql ise anahtar=değer biçimi bekler.
/// URI gelirse çevrilir; anahtar=değer biçimi olduğu gibi kullanılır.
/// </summary>
public static class ConnectionStrings
{
    public static string Normalize(string value)
    {
        if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return value;

        var uri = new Uri(value);
        var userInfo = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
        };

        // Dış ağdan bağlanırken TLS zorunlu; aynı ağdaki (internal) adreste TLS'e gerek yok.
        if (uri.Host.Contains('.'))
            builder.SslMode = SslMode.Require;

        return builder.ConnectionString;
    }
}
