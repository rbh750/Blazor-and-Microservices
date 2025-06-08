namespace Booking.Utilities;

public enum SeatStatus
{
    Available,   // The seat is free and can be selected
    Held,        // The seat is selected (held) but not yet paid
    Reserved     // The seat is paid and fully reserved
}

public class BookingSimulator
{
    record Seat(int Row, int Number, SeatStatus Status = SeatStatus.Available);
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
                .Select(seatNumber => new Seat(row, seatNumber)))];

        // Create a list of booking tasks with a length twice the theater's capacity to simulate high concurrency.
        List<Task> tasks = [.. Enumerable.Range(0, seats.Count * 2)
            .Select(i =>
            {
                return Task.Run(() => BookSeat());
            })];

        Task.WaitAll([.. tasks]);
    }

    private async Task BookSeat()
    {
        // Check if the circuit breaker is active.
        circuitBreakerEvent.Wait();

        int delay = Random.Shared.Next(1000, 5000);
        await Task.Delay(delay);

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
                seats[seatListIndex] = randomSeat with { Status = SeatStatus.Held };
                Console.WriteLine($"Seat {randomSeat.Row}-{randomSeat.Number} held");
            }
        }

        if (seatToBook is { Status: SeatStatus.Held })
        {
            // Try to reserve the held seat with a 75% chance
            lock (seats)
            {
                var heldSeat = seats.FirstOrDefault(s => s.Row == seatToBook.Row && s.Number == seatToBook.Number && s.Status == SeatStatus.Held);
                if (heldSeat != null)
                {
                    // 75% chance to reserve, 25% to set back to available
                    if (Random.Shared.NextDouble() < 0.75)
                    {
                        seats[seats.IndexOf(heldSeat)] = heldSeat with { Status = SeatStatus.Reserved };
                        Console.WriteLine($"Seat {heldSeat.Row}-{heldSeat.Number} reserved");
                    }
                    else
                    {
                        seats[seats.IndexOf(heldSeat)] = heldSeat with { Status = SeatStatus.Available };
                        Console.WriteLine($"Seat {heldSeat.Row}-{heldSeat.Number} released (set to available)");
                    }
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