using Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Events.Infrastructure.Persistence.Configurations;

public class ProcessedBookingConfiguration : IEntityTypeConfiguration<ProcessedBooking>
{
    public void Configure(EntityTypeBuilder<ProcessedBooking> builder)
    {
        builder.ToTable("ProcessedBookings");
        builder.HasKey(e => e.BookingId);

        builder.Property(e => e.BookingId).ValueGeneratedNever();
        builder.HasIndex(e => e.EventId);
        builder.Property(e => e.ProcessedAt).HasColumnType("timestamp with time zone");
    }
}