using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace imicCharge.API.Services;

public class EaseeService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private string? _accessToken;
    private DateTime _tokenExpiryTime;

    public EaseeService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _httpClient.BaseAddress = new Uri("https://api.easee.com/");
    }

    private async Task AuthenticateIfNeededAsync()
    {
        if (string.IsNullOrEmpty(_accessToken) || DateTime.UtcNow >= _tokenExpiryTime)
        {
            var easeeSettings = _configuration.GetSection("EaseeSettings");
            var credentials = new { userName = easeeSettings["Username"], password = easeeSettings["Password"] };
            var content = new StringContent(JsonSerializer.Serialize(credentials), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/accounts/token", content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

            _accessToken = tokenResponse.GetProperty("accessToken").GetString();
            var expiresIn = tokenResponse.GetProperty("expiresIn").GetInt32();
            _tokenExpiryTime = DateTime.UtcNow.AddSeconds(expiresIn - 60); // Subtract a minute for safety
        }
    }

    public async Task<bool> StartChargingAsync(string chargerId)
    {
        await AuthenticateIfNeededAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        var response = await _httpClient.PostAsync($"api/chargers/{chargerId}/commands/start_charging", null);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> StopChargingAsync(string chargerId)
    {
        await AuthenticateIfNeededAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        var response = await _httpClient.PostAsync($"api/chargers/{chargerId}/commands/stop_charging", null);

        return response.IsSuccessStatusCode;
    }
}