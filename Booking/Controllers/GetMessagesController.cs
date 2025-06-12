using AzServices.Services;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GetMessagesController(IServiceBusService serviceBusService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetMessages(
            [FromQuery] string topic,
            [FromQuery] string subscription,
            [FromQuery] int maxMessages = 10)
        {
            // Use object for generic deserialization, or you can use a specific type if you know it
            var messages = await serviceBusService.ReceiveMessagesAsync<object>(topic, subscription, maxMessages);
            return Ok(messages);
        }
    }
}