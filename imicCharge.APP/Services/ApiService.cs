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

public class ApiService
{
    private readonly string _baseUrl;

    public ApiService()
    {
#if DEBUG
        _baseUrl = DeviceInfo.DeviceType == DeviceType.Virtual
            ? "https://10.0.2.2:7165" // Android Emulator
            : "https://192.168.1.49:7165"; // Physical device or windows app
    #else
        _baseUrl = "https://production_url.com";
    #endif
    }

    /// <summary>
    /// Attempts to log in a user with the provided credentials.
    /// </summary>
    /// <returns>A LoginResponse object if successful, otherwise null.</returns>
    public async Task<LoginResponse?> LoginAsync(string email, string password)
    {
        var client = GetHttpClient();
        var loginData = new { email, password };

        try
        {
            var response = await client.PostAsJsonAsync("login", loginData); // Bruker PostAsJsonAsync for enklare kode
            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                return loginResponse;
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
    /// <returns>True if registration is successful, otherwise false.</returns>
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
         /// <returns>The account balance as a decimal, or null if the request fails.</returns>
    public async Task<decimal?> GetAccountBalanceAsync()
    {
        var client = GetHttpClient();
        var token = await SecureStorage.GetAsync("access_token");
        if (string.IsNullOrEmpty(token))
        {
            return null; // No token. Can't make authenticated request
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await client.GetAsync("api/Payment/get-account-balance");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var json = JsonSerializer.Deserialize<JsonElement>(content);
                if (json.TryGetProperty("accountBalance", out var balanceElement))
                {
                    return balanceElement.GetDecimal();
                }
            }
        }
        catch (Exception ex)
        {
            // Log the exception (e.g., using Console.WriteLine for now)
            Console.WriteLine($"Error fetching balance: {ex.Message}");
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
                // Viss sertifikatet er utstedt til 'localhost', godta det.
                if (cert != null && cert.Issuer.Equals("CN=localhost", StringComparison.Ordinal))
                    return true;

                // Elles, krev standard validering.
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