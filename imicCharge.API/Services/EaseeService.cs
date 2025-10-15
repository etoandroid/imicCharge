using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using imicCharge.API.Models;

namespace imicCharge.API.Services;

public class ChargeSession
{
    public double Kwh { get; set; }
}

/// <summary>
/// Handles all communication with the Easee API for charging operations.
/// </summary>
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

    /// <summary>
    /// Authenticates with the Easee API if the current access token is missing or expired.
    /// </summary>
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

    /// <summary>
    /// Sends a command to start charging a specific charger.
    /// </summary>
    /// <param name="chargerId">The ID of the charger to start.</param>
    /// <returns>True if the command was successful, otherwise false.</returns>
    public async Task<bool> StartChargingAsync(string chargerId)
    {
        await AuthenticateIfNeededAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        var response = await _httpClient.PostAsync($"api/chargers/{chargerId}/commands/start_charging", null);

        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Sends a command to stop charging a specific charger.
    /// </summary>
    /// <param name="chargerId">The ID of the charger to stop.</param>
    /// <returns>True if the command was successful, otherwise false.</returns>
    public async Task<bool> StopChargingAsync(string chargerId)
    {
        await AuthenticateIfNeededAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        var response = await _httpClient.PostAsync($"api/chargers/{chargerId}/commands/stop_charging", null);

        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Retrieves the latest charging session details for a specific charger.
    /// </summary>
    /// <param name="chargerId">The ID of the charger.</param>
    /// <returns>A ChargeSession object with details about the last session, or null if not found.</returns>
    public async Task<ChargeSession?> GetLatestChargingSessionAsync(string chargerId)
    {
        await AuthenticateIfNeededAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var response = await _httpClient.GetAsync($"api/chargers/{chargerId}/sessions/latest");
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        var sessionElement = JsonSerializer.Deserialize<JsonElement>(responseBody);

        // Hentar ut 'kwh' frå responsen. Easee API returnerer dette som 'totalKiloWattHours'
        if (sessionElement.TryGetProperty("totalKiloWattHours", out var kwhElement))
        {
            return new ChargeSession { Kwh = kwhElement.GetDouble() };
        }

        return null;
    }
}