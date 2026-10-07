using AeroLicense.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroLicense.Infrastructure.Persistence.Configurations;

public sealed class FlightLogConfiguration : IEntityTypeConfiguration<FlightLog>
{
    public void Configure(EntityTypeBuilder<FlightLog> builder)
    {
        // Domain kuralları veritabanında da (derinlemesine savunma).
        builder.ToTable("flight_logs", t =>
        {
            t.HasCheckConstraint("ck_flight_logs_departure_before_arrival", "\"DepartureAtUtc\" < \"ArrivalAtUtc\"");
            t.HasCheckConstraint("ck_flight_logs_duration_positive", "\"DurationMinutes\" > 0");
        });

        builder.Property(f => f.FlightNumber).HasMaxLength(8).IsRequired();
        builder.Property(f => f.DepartureAirport).HasMaxLength(4).IsFixedLength().IsRequired();
        builder.Property(f => f.ArrivalAirport).HasMaxLength(4).IsFixedLength().IsRequired();

        builder.HasOne(f => f.Pilot).WithMany().HasForeignKey(f => f.PilotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(f => f.License).WithMany().HasForeignKey(f => f.LicenseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(f => f.Airline).WithMany().HasForeignKey(f => f.AirlineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(f => f.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);

        // Hem özet (PilotId = ? AND DepartureAtUtc >= ?) hem çakışma kontrolü (PilotId = ? AND DepartureAtUtc < ?)
        // bu index'in ön ekini kullanır. INCLUDE: covering index, SUM/MAX için tabloya gitmeye gerek kalmaz.
        builder.HasIndex(f => new { f.PilotId, f.DepartureAtUtc })
            .IncludeProperties(f => new { f.DurationMinutes, f.ArrivalAtUtc });
        builder.HasIndex(f => new { f.AirlineId, f.RecordedAtUtc });
    }
}
