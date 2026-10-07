using System.Globalization;
using AeroLicense.Application.Features.Applications;
using AeroLicense.Application.Features.Audit;
using AeroLicense.Application.Features.Auth;
using AeroLicense.Application.Features.FlightLogs;
using AeroLicense.Application.Features.Licenses;
using AeroLicense.Application.Features.Training;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AeroLicense.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("tr");
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITrainingService, TrainingService>();
        services.AddScoped<ILicenseApplicationService, LicenseApplicationService>();
        services.AddScoped<IFlightLogService, FlightLogService>();
        services.AddScoped<ILicenseService, LicenseService>();
        services.AddScoped<IDocumentVerificationService, DocumentVerificationService>();
        services.AddScoped<LicenseDocumentIssuer>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        return services;
    }
}
