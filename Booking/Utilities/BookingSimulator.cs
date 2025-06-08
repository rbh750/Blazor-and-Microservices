using AzServices;

namespace Booking.Utilities;

public enum SeatStatus
{
    Available,   // The seat is free and can be selected
    Held,        // The seat is selected (held) but not yet paid
    Reserved     // The seat is paid and fully reserved
}

public interface IBookingSimulator
{
    void BookSeats(int rows, int seatsPerRow);
}

public class BookingSimulator(IServiceBusService serviceBusService) : IBookingSimulator
{
    record Seat(int Row, int Number, SeatStatus Status)
    {
        public Seat WithStatus(SeatStatus newStatus) => this with { Status = newStatus };
    }

    List<Seat> seats = [];
    private volatile bool circuitBroken = false;

    // Simulates a circuit breaker by manually blocking and unblocking bookings threads.
    private readonly ManualResetEventSlim circuitBreakerEvent = new(true);

    public void BookSeats(int rows, int seatsPerRow)
    {
        // For each row and each seat in that row, create a Seat object
        // and the flatten the result into a single list of seats.

        //[
        //  [Seat(1,1), Seat(1,2), ...], // Row 1
        //  [Seat(2,1), Seat(2,2), ...], // Row 2
        //  ...
        //][
        //  [Seat(1,1), Seat(1,2), ...], // Row 1
        //  [Seat(2,1), Seat(2,2), ...], // Row 2
        //  ...
        //]

        seats = [.. Enumerable.Range(1, rows)
            .SelectMany(row =>
                Enumerable.Range(1, seatsPerRow)
                .Select(seatNumber => new Seat(row, seatNumber, SeatStatus.Available)))];

        // Create a list of booking tasks with a length twice the theater's capacity to simulate high concurrency.
        // and then process them in batches to simulate different booking times.
        int totalTasks = seats.Count;
        int batchSize = 10;

        for (int i = 0; i < totalTasks; i += batchSize)
        {
            var batchTasks = Enumerable.Range(i, Math.Min(batchSize, totalTasks - i))
                .Select(_ => Task.Run(() => BookSeat()))
                .ToArray();

            Task.WaitAll(batchTasks);
        }
    }

    private async Task BookSeat()
    {
        // Simulates different booking times by introducing a random delay
        int delay = Random.Shared.Next(1000, 5000);
        await Task.Delay(delay);

        // "Enter the ManualResetEvent area"
        // Blocks the current thread if the circuit breaker is closed (i.e., after ManualResetEventSlim.Reset() is called),
        // and resumes only when the circuit breaker is opened again (ManualResetEventSlim.Set() is called).
        circuitBreakerEvent.Wait();

        // Pick any available seat randomly (thread-safe)
        Seat? seatToBook = null;
        lock (seats)
        {
            var availableSeats = seats
                .Select((seat, idx) => (seat, idx))
                .Where(x => x.seat.Status == SeatStatus.Available)
                .ToList();

            if (availableSeats.Count > 0)
            {
                // Randomly select one of the available seats
                var randomIndex = Random.Shared.Next(availableSeats.Count);
                var (randomSeat, seatListIndex) = availableSeats[randomIndex];
                seatToBook = randomSeat;

                // Mark as held (simulate booking in process)
                seatToBook = randomSeat with { Status = SeatStatus.Held };
                // seats[seatListIndex] = randomSeat with { Status = SeatStatus.Held };
                Console.WriteLine($"Seat {randomSeat.Row}-{randomSeat.Number} held");
            }
        }

        if (seatToBook is { Status: SeatStatus.Held })
        {
            // Try to reserve the held seat with a 75% chance
            lock (seats)
            {
                var processedSeat = seats.First(s => s.Row == seatToBook.Row && s.Number == seatToBook.Number);

                // 75% chance to reserve, 25% to set back to available
                if (Random.Shared.NextDouble() < 0.75)
                {
                    seats[seats.IndexOf(processedSeat)] = seatToBook with { Status = SeatStatus.Reserved };
                    Console.WriteLine($"Seat {processedSeat.Row}-{processedSeat.Number} reserved");
                }
                else
                {
                    seats[seats.IndexOf(processedSeat)] = processedSeat with { Status = SeatStatus.Available };
                    Console.WriteLine($"Seat {processedSeat.Row}-{processedSeat.Number} released (set to available)");
                }
            }
        }

        // Check for circuit breaker after reservation
        TriggerCircuitBreakerIfNeeded();
    }

    // If the reservation rate exceeds 50%, trigger a circuit breaker
    // and pause further bookings for 5 seconds.
    private void TriggerCircuitBreakerIfNeeded()
    {
        if (!circuitBroken)
        {
            int reservedCount;
            int totalCount;
            lock (seats)
            {
                reservedCount = seats.Count(s => s.Status == SeatStatus.Reserved);
                totalCount = seats.Count;
            }
            if (reservedCount >= totalCount / 2)
            {
                circuitBroken = true;
                // Close gate.
                circuitBreakerEvent.Reset();
                Console.WriteLine("Circuit breaker triggered: waiting 5 seconds due to high reservation rate.");
                Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    // Open gate after 5 seconds.
                    circuitBreakerEvent.Set();
                    Console.WriteLine("Circuit breaker released: processing resumes.");
                });
            }
        }
    }
}