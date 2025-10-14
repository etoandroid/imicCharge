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