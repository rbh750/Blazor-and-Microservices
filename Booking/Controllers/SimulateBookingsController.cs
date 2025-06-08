using AzServices;
using Booking.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SimulateBookingsController(IServiceBusService serviceBusService) : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            new BookingSimulator(serviceBusService).BookSeats(10, 10);
            return Ok();
        }
    }
}
