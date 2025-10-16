using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace imicCharge.APP.Services;

public class LoginResponse
{
    public string? TokenType { get; set; }
    public string? AccessToken { get; set; }
    public int ExpiresIn { get; set; }
    public string? RefreshToken { get; set; }
}

public class StopChargingResponse
{
    public string? Message { get; set; }
    public decimal NewBalance { get; set; }
}

public class ApiService
{
    private readonly string _baseUrl;

    public ApiService()
    {
#if DEBUG
        // Denne logikken set riktig adresse basert på kva plattform du testar på.
        if (DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            // Brukar localhost for Windows-appen, sidan den køyrer på same maskin som API-et.
            _baseUrl = "https://localhost:7165";
        }
        else if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            // Brukar spesielle adresser for Android-emulator vs. fysisk telefon.
            _baseUrl = DeviceInfo.DeviceType == DeviceType.Virtual
                ? "https://10.0.2.2:7165"
                : "https://192.168.1.49:7165";
        }
        else
        {
            // Fallback for andre plattformer som iOS eller Mac.
            _baseUrl = "https://192.168.1.49:7165";
        }
#else
        // I produksjon vil du byte ut denne med den faktiske adressa til API-et.
        _baseUrl = "https://din-framtidige-produksjons-url.com";
#endif
    }

    /// <summary>
    /// Attempts to log in a user with the provided credentials.
    /// </summary>
    public async Task<LoginResponse?> LoginAsync(string email, string password)
    {
        var client = GetHttpClient();
        var loginData = new { email, password };

        try
        {
            var response = await client.PostAsJsonAsync("login", loginData);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<LoginResponse>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// Attempts to register a new user.
    /// </summary>
    public async Task<bool> RegisterAsync(string email, string password)
    {
        var client = GetHttpClient();
        var registerData = new { email, password };

        try
        {
            var response = await client.PostAsJsonAsync("register", registerData);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Registration error: {ex.Message}");
        }
        return false;
    }

    /// <summary>
    /// Retrieves the account balance for the currently authenticated user.
    /// </summary>
    public async Task<decimal?> GetAccountBalanceAsync()
    {
        var client = GetHttpClient();
        var token = await SecureStorage.GetAsync("access_token");
        if (string.IsNullOrEmpty(token)) return null;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await client.GetAsync("api/Payment/get-account-balance");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (json.TryGetProperty("accountBalance", out var balanceElement))
                {
                    return balanceElement.GetDecimal();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching balance: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Sends a request to the API to start a charging session.
    /// </summary>
    public async Task<bool> StartChargingAsync(string chargerId)
    {
        var client = GetHttpClient();
        var token = await SecureStorage.GetAsync("access_token");
        if (string.IsNullOrEmpty(token)) return false;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var chargeRequest = new { chargerId };

        try
        {
            var response = await client.PostAsJsonAsync("api/Charge/start", chargeRequest);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting charge: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sends a request to the API to stop a charging session.
    /// </summary>
    public async Task<StopChargingResponse?> StopChargingAsync(string chargerId)
    {
        var client = GetHttpClient();
        var token = await SecureStorage.GetAsync("access_token");
        if (string.IsNullOrEmpty(token)) return null;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var chargeRequest = new { chargerId };

        try
        {
            var response = await client.PostAsJsonAsync("api/Charge/stop", chargeRequest);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<StopChargingResponse>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping charge: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// Asks the API for a Stripe Checkout session URL.
    /// </summary>
    public async Task<string?> CreateCheckoutSessionAsync(decimal amount)
    {
        var client = GetHttpClient();
        var token = await SecureStorage.GetAsync("access_token");
        if (string.IsNullOrEmpty(token)) return null;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var paymentRequest = new { amount };

        try
        {
            var response = await client.PostAsJsonAsync("api/Payment/create-checkout-session", paymentRequest);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (json.TryGetProperty("url", out var urlElement))
                {
                    return urlElement.GetString();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating checkout session: {ex.Message}");
        }
        return null;
    }

    public HttpClient GetHttpClient()
    {
#if DEBUG
        // This code runs only in debug mode and accepts self-signed certificates for local development.
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
            {
                if (cert != null && cert.Issuer.Equals("CN=localhost", StringComparison.Ordinal))
                    return true;

                return sslPolicyErrors == System.Net.Security.SslPolicyErrors.None;
            }
        };
        return new HttpClient(handler) { BaseAddress = new Uri(_baseUrl) };
#else
        // Use standard HttpClient in release mode.
        return new HttpClient { BaseAddress = new Uri(_baseUrl) };
#endif
    }
}
