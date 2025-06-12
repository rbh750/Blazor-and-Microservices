using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzServices.Services;

public interface IRestService
{
    Task<HttpStatusCode?> SimulateBookings(int rows, int seatsPerRow, int numberOfBookings, string movie);
    Task<List<T>?> GetMessages<T>(string topic, string subscription, int maxMessages);
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

    // Retrieves messages from the specified topic and subscription.
    public async Task<List<T>?> GetMessages<T>(string topic, string subscription, int maxMessages)
    {
        List<T>? result = null;

        StringBuilder sb = new();
        sb.Append(httpClient.BaseAddress!);
        sb.Append($"GetMessages");
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
                result = JsonSerializer.Deserialize<List<T>>(responseContent, GetSerializerSettings());
            }
        }

        return result;
    }

    private static JsonSerializerOptions GetSerializerSettings() =>
        new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

}
