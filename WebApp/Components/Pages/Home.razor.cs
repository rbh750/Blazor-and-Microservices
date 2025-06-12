using AzServices.Entities;
using AzServices.Enums;
using AzServices.Services;
using Microsoft.AspNetCore.Components;
using System.Net;

namespace WebApp.Components.Pages;

record Seat(int Row, int Number, string css) { }

public partial class Home : ComponentBase
{
    [Inject] private IRestService RestService { get; init; } = default!;

    public int Rows { get; set; } = 4;
    public int Seats { get; set; } = 4;
    public int NumberOfBookings { get; set; } = 20;
    public List<string> Movie { get; set; } = [];
    public string? SelectedMovie { get; set; }

    private readonly string cssAvailable = "color:black; background-color: white;";
    private readonly string cssReserved = "color:white; background-color: red;";
    private readonly string cssHeld = "color:black; background-color: yellow;";
    private List<Seat> seats = [];
    private readonly List<Booking> bookings = [];
    private List<Booking>? bookingsbatch = [];
    private const string TOPIC_BOOKINGS = "seatupdates";
    private const string SUBSCRIPTION = "WebsiteSubscription";
    private bool disableBtn = false;


    protected override void OnInitialized()
    {
        Movie = ["Echoes of Tomorrow", "The last knight", "Back to school"];
        SelectedMovie = Movie[0];
    }

    async Task SimulateBookings()
    {
        if (SelectedMovie != null)
        {
            disableBtn = true;
            ResetSeatsFormat();
            bookings.Clear(); // Clear previous bookings

            // Create a dictionary for fast seat lookups
            var seatLookup = seats.ToDictionary(s => (s.Row, s.Number));

            var bookingSimulationResult = await RestService.SimulateBookings(Rows, Seats, NumberOfBookings, SelectedMovie);
            if (bookingSimulationResult != HttpStatusCode.Accepted) return;

            do
            {
                bookingsbatch = await RestService.GetMessages<Booking>(TOPIC_BOOKINGS, SUBSCRIPTION, 1);
                if (bookingsbatch == null) break;


                // For each new incomingBooking, update existing or add new
                foreach (var incomingBooking in bookingsbatch)
                {
                    // Try to find existing incomingBooking for this seat
                    var existingBookingIndex = bookings.FindIndex(b => b.Row == incomingBooking.Row && b.Number == incomingBooking.Number);

                    if (existingBookingIndex >= 0)
                    {
                        bookings[existingBookingIndex] = incomingBooking;
                    }
                    else
                    {
                        bookings.Add(incomingBooking);
                    }
                }

                // Update seat statuses based on the new bookings
                foreach (var booking in bookings)
                {
                    // Use dictionary lookup instead of LINQ search
                    if (seatLookup.TryGetValue((booking.Row, booking.Number), out Seat? existingSeat))
                    {
                        // Update seat with new CSS style
                        var updatedSeat = existingSeat with { css = SeatCssStyle(booking) };

                        // Update both the dictionary and the list
                        seatLookup[(booking.Row, booking.Number)] = updatedSeat;
                        int index = seats.IndexOf(existingSeat);
                        if (index >= 0)
                        {
                            seats[index] = updatedSeat;
                        }
                    }
                }

                await Task.Delay(500);
                StateHasChanged();

            } while (bookingsbatch != null && bookingsbatch.Count > 0);
        }
        disableBtn = false;
        StateHasChanged();
    }

    private void ResetSeatsFormat()
    {
        seats = [.. Enumerable.Range(1, Rows)
            .SelectMany(row =>
                Enumerable.Range(1, Seats)
                .Select(seatNumber => new Seat(row, seatNumber, cssAvailable)))];
    }

    private string SeatCssStyle(Booking booking)
    {
        return booking.Status switch
        {
            SeatStatus.Available => cssAvailable,
            SeatStatus.Held => cssHeld,
            SeatStatus.Reserved => cssReserved,
            _ => cssAvailable // Default case
        };
    }
}

