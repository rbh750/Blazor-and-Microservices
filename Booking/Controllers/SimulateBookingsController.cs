using AzServices.Services;
using Booking.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Controllers
{
    public record SimulateBookingRequest(int Rows, int SeatsPerRow, int NumberOfBookings, string Movie);

    [ApiController]
    [Route("[controller]")]
    public class SimulateBookingsController(IServiceBusService serviceBusService) : ControllerBase
    {
        [HttpPost]
        public IActionResult Post([FromBody] SimulateBookingRequest request)
        {
            Task.Run(() =>
            {
                new BookingSimulator(serviceBusService).BookSeats(
                    request.Rows,
                    request.SeatsPerRow,
                    request.NumberOfBookings,
                    request.Movie
                );
            });

            return Accepted();
        }
    }
}