using EventApi.Domain.Entities;
using Integration.Tests.Fixtures;

namespace Integration.Tests;

[Collection("Database")]
public class BookingRepositoryTests : RepositoryTestBase
{
    public BookingRepositoryTests(PostgresContainerFixture fixture) : base(fixture)
    {
    }

    private async Task<Event> CreatePersistedEventAsync(int totalSeats = 10)
    {
        var testEvent = Event.Create("Test Event", null, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), totalSeats);
        await EventRepository.AddAsync(testEvent);
        return testEvent;
    }

    [Fact]
    public async Task GetByIdAsync_WhenBookingExists_ReturnsBooking()
    {
        // Arrange
        var testEvent = await CreatePersistedEventAsync();
        var booking = Booking.Create(testEvent.Id, Guid.NewGuid(), BookingStatus.Pending, DateTime.UtcNow);
        await BookingRepository.AddAsync(booking);

        // Act
        var result = await BookingRepository.GetByIdAsync(booking.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(booking.Id, result!.Id);
        Assert.Equal(BookingStatus.Pending, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_WhenBookingDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await BookingRepository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsForEventAsync_WhenBookingExists_ReturnsTrue()
    {
        // Arrange
        var testEvent = await CreatePersistedEventAsync();
        var booking = Booking.Create(testEvent.Id, Guid.NewGuid(), BookingStatus.Pending, DateTime.UtcNow);
        await BookingRepository.AddAsync(booking);

        // Act
        var exists = await BookingRepository.ExistsForEventAsync(testEvent.Id);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsForEventAsync_WhenNoBookingsForEvent_ReturnsFalse()
    {
        // Arrange
        var testEvent = await CreatePersistedEventAsync();

        // Act
        var exists = await BookingRepository.ExistsForEventAsync(testEvent.Id);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task AddAsync_PersistsBooking()
    {
        // Arrange
        var testEvent = await CreatePersistedEventAsync();
        var booking = Booking.Create(testEvent.Id, Guid.NewGuid(), BookingStatus.Pending, DateTime.UtcNow);

        // Act
        await BookingRepository.AddAsync(booking);

        // Assert
        var saved = await Context.Bookings.FindAsync(booking.Id);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task UpdateAsync_PersistsStatusChange()
    {
        // Arrange
        var testEvent = await CreatePersistedEventAsync();
        var booking = Booking.Create(testEvent.Id, Guid.NewGuid(), BookingStatus.Pending, DateTime.UtcNow);
        await BookingRepository.AddAsync(booking);

        // Act
        booking.Confirm();
        await BookingRepository.UpdateAsync(booking);

        // Assert
        var reloaded = await BookingRepository.GetByIdAsync(booking.Id);
        Assert.Equal(BookingStatus.Confirmed, reloaded!.Status);
        Assert.NotNull(reloaded.ProcessedAt);
    }

    [Fact]
    public async Task GetPendingIdsAsync_ReturnsOnlyPendingBookings()
    {
        // Arrange
        var testEvent = await CreatePersistedEventAsync();
        var pending = Booking.Create(testEvent.Id, Guid.NewGuid(), BookingStatus.Pending, DateTime.UtcNow);
        var confirmed = Booking.Create(testEvent.Id, Guid.NewGuid(), BookingStatus.Confirmed, DateTime.UtcNow);
        var rejected = Booking.Create(testEvent.Id, Guid.NewGuid(), BookingStatus.Rejected, DateTime.UtcNow);
        await BookingRepository.AddAsync(pending);
        await BookingRepository.AddAsync(confirmed);
        await BookingRepository.AddAsync(rejected);

        // Act
        var pendingIds = await BookingRepository.GetPendingIdsAsync();

        // Assert
        Assert.Single(pendingIds);
        Assert.Contains(pending.Id, pendingIds);
    }
}
