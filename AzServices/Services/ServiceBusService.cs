using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace AzServices.Services;

public record SeatUpdateMessage(int Row, int Number, string Status, string Movie);
public record ApiStatusMessage(string Status);

public interface IServiceBusService
{
    ValueTask DisposeAsync();
    Task<List<T>> ReceiveMessagesAsync<T>(string topic, string subscription, int maxMessages = 10);
    Task SendApiStatusAsync(ApiStatusMessage message);
    Task SendSeatUpdateAsync(SeatUpdateMessage message);

    void EnableDisposal();
}

public class ServiceBusService : IAsyncDisposable, IServiceBusService
{
    private bool canDispose = false; // Prevents the client to be disposed by a task.
    private readonly ServiceBusClient client;
    private const string ConnectionString = "Endpoint=sb://booking-uservice-sbs.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=Ebxyy994AekuZz2eyN1mgpk96Kh8oQY2r+ASbHPu5r4=";

    public ServiceBusService()
    {
        client = new ServiceBusClient(ConnectionString);
    }

    // Send a seat update message to the seatupdates topic
    public async Task SendSeatUpdateAsync(SeatUpdateMessage message)
    {
        var sender = client.CreateSender("seatupdates");
        var body = JsonSerializer.Serialize(message);
        var sbMessage = new ServiceBusMessage(body);
        await sender.SendMessageAsync(sbMessage);
    }

    // Send a booking error message to the bookingerrors topic
    public async Task SendApiStatusAsync(ApiStatusMessage message)
    {
        var sender = client.CreateSender("bookingerrors");
        var body = JsonSerializer.Serialize(message);
        var sbMessage = new ServiceBusMessage(body);
        await sender.SendMessageAsync(sbMessage);
    }

    // Receive messages from a subscription (for both topics)
    public async Task<List<T>> ReceiveMessagesAsync<T>(string topic, string subscription, int maxMessages = 10)
    {
        var receiver = client.CreateReceiver(topic, subscription);
        var messages = await receiver.ReceiveMessagesAsync(maxMessages, TimeSpan.FromSeconds(2));
        var result = new List<T>();

        foreach (var msg in messages.Reverse())
        {
            var obj = JsonSerializer.Deserialize<T>(msg.Body);
            if (obj != null)
                result.Add(obj);

            await receiver.CompleteMessageAsync(msg);
        }

        return result;
    }

    public void EnableDisposal()
    {
        canDispose = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (canDispose)
        {
            await client.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }
}