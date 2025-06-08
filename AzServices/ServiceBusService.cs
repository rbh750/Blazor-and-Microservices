using Azure.Messaging.ServiceBus;
using System.Text.Json;
using System.Threading.Tasks;

namespace AzServices;

public enum SeatStatus
{
    Available,
    Held,
    Reserved
}

public record SeatUpdateMessage(int Row, int Number, SeatStatus Status, string Movie);
public record BookingErrorMessage(string Error);

public interface IServiceBusService
{
    ValueTask DisposeAsync();
    Task<List<T>> ReceiveMessagesAsync<T>(string topic, string subscription, int maxMessages = 10);
    Task SendBookingErrorAsync(BookingErrorMessage message);
    Task SendSeatUpdateAsync(SeatUpdateMessage message);
}

public class ServiceBusService : IAsyncDisposable, IServiceBusService
{
    private readonly ServiceBusClient _client;
    private const string ConnectionString = "Endpoint=sb://booking-uservice-sbs.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=Ebxyy994AekuZz2eyN1mgpk96Kh8oQY2r+ASbHPu5r4=";

    public ServiceBusService()
    {
        _client = new ServiceBusClient(ConnectionString);
    }

    // Send a seat update message to the seatupdates topic
    public async Task SendSeatUpdateAsync(SeatUpdateMessage message)
    {
        var sender = _client.CreateSender("seatupdates");
        var body = JsonSerializer.Serialize(message);
        var sbMessage = new ServiceBusMessage(body);
        await sender.SendMessageAsync(sbMessage);
    }

    // Send a booking error message to the bookingerrors topic
    public async Task SendBookingErrorAsync(BookingErrorMessage message)
    {
        var sender = _client.CreateSender("bookingerrors");
        var body = JsonSerializer.Serialize(message);
        var sbMessage = new ServiceBusMessage(body);
        await sender.SendMessageAsync(sbMessage);
    }

    // Receive messages from a subscription (for both topics)
    public async Task<List<T>> ReceiveMessagesAsync<T>(string topic, string subscription, int maxMessages = 10)
    {
        var receiver = _client.CreateReceiver(topic, subscription);
        var messages = await receiver.ReceiveMessagesAsync(maxMessages, TimeSpan.FromSeconds(5));
        var result = new List<T>();

        foreach (var msg in messages)
        {
            var obj = JsonSerializer.Deserialize<T>(msg.Body);
            if (obj != null)
                result.Add(obj);

            await receiver.CompleteMessageAsync(msg);
        }

        return result;
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
    }
}