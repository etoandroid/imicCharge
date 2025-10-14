using Android.Net;
using imicCharge.APP.Services;
using System.Text;
using System.Text.Json;

namespace imicCharge.APP;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _apiService;
    private class LoginResponse
    {
        public string? tokenType { get; set; }
        public string? accessToken { get; set; }
        public int expiresIn { get; set; }
        public string? refreshToken { get; set; }
    }

    public LoginPage()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        StatusLabel.Text = "Loggar inn...";

        var loginData = new
        {
            email = EmailEntry.Text,
            password = PasswordEntry.Text
        };

        try
        {
            var json = JsonSerializer.Serialize(loginData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var client = _apiService.GetHttpClient();
            var response = await client.PostAsync("login", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // TODO: Store access token securely

                await Shell.Current.GoToAsync($"//{nameof(MainPage)}");
            }
            else
            {
                StatusLabel.Text = $"Feil: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Nettverksfeil: {ex.Message}";
        }
    }
}