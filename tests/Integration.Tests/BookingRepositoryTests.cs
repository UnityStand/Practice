using Bookings.Domain.Entities;
using Bookings.Infrastructure.Persistence;
using Integration.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Integration.Tests;

public class BookingRepositoryTests(PostgresContainerFixture fixture)
    : DatabaseTestBase<BookingDbContext>(fixture, "booking_db_test")
{
    protected override BookingDbContext CreateContext(DbContextOptions<BookingDbContext> options) => new(options);

    private BookingRepository Repository => new(Context);

    private static Booking NewBooking(Guid? userId = null, BookingStatus status = BookingStatus.Pending) =>
        Booking.Create(Guid.NewGuid(), userId ?? Guid.NewGuid(), status, DateTime.UtcNow);

    [Fact]
    public async Task Migration_CreatesOnlyBookingsTables()
    {
        var tables = await PublicTablesAsync();

        Assert.Equal(["Bookings", "__EFMigrationsHistory"], tables);
    }

    // EventId и UserId — просто идентификаторы из других сервисов, без внешних ключей
    [Fact]
    public async Task AddAsync_PersistsBooking_WithoutForeignKeysToOtherServices()
    {
        var booking = NewBooking();

        await Repository.AddAsync(booking);

        var saved = await NewContext().Bookings.SingleAsync(b => b.Id == booking.Id);
        Assert.Equal(booking.EventId, saved.EventId);
        Assert.Equal(booking.UserId, saved.UserId);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        Assert.Null(await Repository.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateAsync_PersistsStatusChange()
    {
        var booking = NewBooking();
        await Repository.AddAsync(booking);

        booking.Confirm();
        await Repository.UpdateAsync(booking);

        var reloaded = await new BookingRepository(NewContext()).GetByIdAsync(booking.Id);
        Assert.Equal(BookingStatus.Confirmed, reloaded!.Status);
        Assert.NotNull(reloaded.ProcessedAt);
    }

    [Fact]
    public async Task CountActiveBookingsByUserIdAsync_CountsOnlyPendingAndConfirmed()
    {
        var userId = Guid.NewGuid();
        await Repository.AddAsync(NewBooking(userId, BookingStatus.Pending));
        await Repository.AddAsync(NewBooking(userId, BookingStatus.Confirmed));
        await Repository.AddAsync(NewBooking(userId, BookingStatus.Cancelled));
        await Repository.AddAsync(NewBooking(userId, BookingStatus.Rejected));
        await Repository.AddAsync(NewBooking());

        Assert.Equal(2, await Repository.CountActiveBookingsByUserIdAsync(userId));
    }

    [Fact]
    public async Task GetPendingIdsAsync_ReturnsOnlyPending()
    {
        var pending = NewBooking(status: BookingStatus.Pending);
        await Repository.AddAsync(pending);
        await Repository.AddAsync(NewBooking(status: BookingStatus.Confirmed));
        await Repository.AddAsync(NewBooking(status: BookingStatus.Rejected));

        var ids = await Repository.GetPendingIdsAsync();

        Assert.Equal([pending.Id], ids);
    }
}
