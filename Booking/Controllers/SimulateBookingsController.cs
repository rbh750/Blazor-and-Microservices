using Booking.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SimulateBookingsController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            new BookingSimulator().BookSeats(10, 10);
            return Ok();
        }
    }
}
