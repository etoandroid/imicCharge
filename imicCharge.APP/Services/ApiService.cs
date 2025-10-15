using System.Net.Http.Headers;
using System.Text.Json;

namespace imicCharge.APP.Services;

public class ApiService
{
    private readonly string _baseUrl;

    public ApiService()
    {
        _baseUrl = DeviceInfo.Platform == DevicePlatform.Android
            ? "https://10.0.2.2:7092"
            : "https://localhost:7092";
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
        // This handler is for development only to bypass SSL certificate validation on Android
        var unsafeHandler = new HttpClientHandler();
        unsafeHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
        {
            if (cert != null && cert.Issuer.Equals("CN=localhost", StringComparison.Ordinal))
                return true;
            return sslPolicyErrors == System.Net.Security.SslPolicyErrors.None;
        };
        return new HttpClient(unsafeHandler) { BaseAddress = new Uri(_baseUrl) };
#else
        // In production, use a standard handler
        return new HttpClient { BaseAddress = new Uri(_baseUrl) };
#endif
    }
}