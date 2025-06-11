using AzServices.Entities;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzServices.Services;

public interface IRestService
{
    Task<HttpStatusCode?> SimulateBookings(int rows, int seatsPerRow, int numberOfBookings, string movie);
    Task<List<Booking>?> GetBookings(string topic, string subscription, int maxMessages);
}

public class RestService(HttpClient httpClient) : IRestService
{

    // Simulates concurrent bookings.
    public async Task<HttpStatusCode?> SimulateBookings(int rows, int seatsPerRow, int numberOfBookings, string movie)
    {
        var requestBody = new
        {
            rows,
            seatsPerRow,
            numberOfBookings,
            movie
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var url = new Uri(httpClient.BaseAddress!, "SimulateBookings");
        var response = await httpClient.PostAsync(url, content);

        return response.StatusCode;
    }

    // Retrieves a list of bookings from the specified topic and subscription in the message broker.
    public async Task<List<Booking>?> GetBookings(string topic, string subscription, int maxMessages)
    {
        List<Booking>? result = null;

        StringBuilder sb = new();
        sb.Append(httpClient.BaseAddress!);
        sb.Append($"GetBookings");
        sb.Append($"?topic={topic}");
        sb.Append($"&subscription={subscription}");
        sb.Append($"&maxMessages={maxMessages}");

        var url = new Uri(sb.ToString());

        var response = await httpClient.GetAsync(url);

        if (response != null && response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(responseContent))
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                };

                result = JsonSerializer.Deserialize<List<Booking>>(responseContent, options);
            }
        }

        return result;
    }
}
