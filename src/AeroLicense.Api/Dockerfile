# Çok aşamalı build: SDK sadece derlemede, çalışan imajda yalnızca ASP.NET runtime var (küçük ve daha az saldırı yüzeyi).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Önce sadece proje dosyaları: kod değişip bağımlılıklar değişmediğinde restore katmanı cache'ten gelir.
COPY src/AeroLicense.Domain/AeroLicense.Domain.csproj src/AeroLicense.Domain/
COPY src/AeroLicense.Application/AeroLicense.Application.csproj src/AeroLicense.Application/
COPY src/AeroLicense.Infrastructure/AeroLicense.Infrastructure.csproj src/AeroLicense.Infrastructure/
COPY src/AeroLicense.Api/AeroLicense.Api.csproj src/AeroLicense.Api/
RUN dotnet restore src/AeroLicense.Api/AeroLicense.Api.csproj

COPY src/ src/
RUN dotnet publish src/AeroLicense.Api/AeroLicense.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
# .NET imajlarındaki hazır root olmayan kullanıcı: konteyner ele geçirilse bile root yetkisi yok.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "AeroLicense.Api.dll"]
