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
    void BookSeats(int rows, int seatsPerRow, int numberOfBookings, string Movie);
}

public class BookingSimulator(IServiceBusService serviceBusService) : IBookingSimulator
{
    record Seat(int Row, int Number, SeatStatus Status)
    {
        public Seat WithStatus(SeatStatus newStatus) => this with { Status = newStatus };
    }

    List<Seat> seats = [];
    private volatile bool circuitBroken = false;
    private readonly ManualResetEventSlim circuitBreakerEvent = new(true);

    // For unique random selection
    private List<int> shuffledIndices = [];
    private int nextIndex = 0;

    public void BookSeats(int rows, int seatsPerRow, int numberOfBookings, string movie)
    {
        seats = [.. Enumerable.Range(1, rows)
            .SelectMany(row =>
                Enumerable.Range(1, seatsPerRow)
                .Select(seatNumber => new Seat(row, seatNumber, SeatStatus.Available)))];

        // Prepare and shuffle indices for unique random selection
        shuffledIndices = Enumerable.Range(0, seats.Count).OrderBy(_ => Random.Shared.Next()).ToList();
        nextIndex = 0;

        int totalTasks = numberOfBookings;
        int batchSize = 10;

        for (int i = 0; i < totalTasks; i += batchSize)
        {
            var batchTasks = Enumerable.Range(i, Math.Min(batchSize, totalTasks - i))
                .Select(_ => Task.Run(() => BookSeat(movie)))
                .ToArray();

            Task.WaitAll(batchTasks);
        }

        serviceBusService.EnableDisposal();

        // Count seat statuses
        int available = seats.Count(s => s.Status == SeatStatus.Available);
        int held = seats.Count(s => s.Status == SeatStatus.Held);
        int reserved = seats.Count(s => s.Status == SeatStatus.Reserved);

        Console.WriteLine($"Available: {available}, Held: {held}, Reserved: {reserved}");
    }

    // Returns a unique random seat index from the shuffled list. 
    // If all indices have been used, returns the index of the next available seat.
    private int GetNextUniqueRandomIndex()
    {
        lock (shuffledIndices)
        {
            if (nextIndex >= shuffledIndices.Count)
            {
                // If all the random index have been used, pick the next avialable seat.
                lock (seats)
                {
                    var avilableSeat = seats.FirstOrDefault(x => x.Status == SeatStatus.Available);
                    return seats.IndexOf(avilableSeat!);
                }
            }
            else
            {
                // Ramdom seat index.
                return shuffledIndices[nextIndex++];
            }
        }
    }

    private async Task BookSeat(string movie)
    {
        int delay = Random.Shared.Next(1000, 5000);
        await Task.Delay(delay);

        circuitBreakerEvent.Wait();

        try
        {
            Seat? seatToBook = null;
            lock (seats)
            {
                var availableSeats = seats
                    .Select((seat, idx) => (seat, idx))
                    .Where(x => x.seat.Status == SeatStatus.Available)
                    .ToList();

                if (availableSeats.Count > 0)
                {
                    // Get a unique random index from the shuffled list
                    var uniqueRandomIndex = GetNextUniqueRandomIndex();
                    var (randomSeat, seatListIndex) = availableSeats.FirstOrDefault(x => x.idx == uniqueRandomIndex);

                    // If the unique index is not in availableSeats (seat already taken), fallback to random
                    if (randomSeat == null)
                    {
                        var fallbackIndex = Random.Shared.Next(availableSeats.Count);
                        (randomSeat, seatListIndex) = availableSeats[fallbackIndex];
                    }

                    seatToBook = randomSeat;
                    seats[seats.IndexOf(seatToBook)] = randomSeat with { Status = SeatStatus.Held };

                    seatToBook = randomSeat with { Status = SeatStatus.Held };
                    serviceBusService.SendSeatUpdateAsync(BrokerMessage(seatToBook, movie));
                    Console.WriteLine($"Seat {seatToBook.Row}-{seatToBook.Number} held");
                }
            }

            if (seatToBook is { Status: SeatStatus.Held })
            {
                lock (seats)
                {
                    var processedSeat = seats.First(s => s.Row == seatToBook.Row && s.Number == seatToBook.Number);

                    if (Random.Shared.NextDouble() < 0.75)
                    {
                        seats[seats.IndexOf(processedSeat)] = processedSeat with { Status = SeatStatus.Reserved };
                        var reservedSeat = processedSeat with { Status = SeatStatus.Reserved };
                        serviceBusService.SendSeatUpdateAsync(BrokerMessage(reservedSeat, movie));
                        Console.WriteLine($"Seat {processedSeat.Row}-{processedSeat.Number} reserved");
                    }
                    else
                    {
                        seats[seats.IndexOf(processedSeat)] = processedSeat with { Status = SeatStatus.Available };
                        var availableSeat = processedSeat with { Status = SeatStatus.Available };
                        serviceBusService.SendSeatUpdateAsync(BrokerMessage(availableSeat, movie));
                        Console.WriteLine($"Seat {processedSeat.Row}-{processedSeat.Number} released (set to available)");
                    }
                }
            }
        }

        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        TriggerCircuitBreakerIfNeeded();
    }

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
                circuitBreakerEvent.Reset();
                Console.WriteLine("Circuit breaker triggered: waiting 5 seconds due to high reservation rate.");
                Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    circuitBreakerEvent.Set();
                    Console.WriteLine("Circuit breaker released: processing resumes.");
                });
            }
        }
    }

    private static SeatUpdateMessage BrokerMessage(Seat seat, string movie) =>
        new(seat.Row, seat.Number, seat.Status.ToString(), movie);
}