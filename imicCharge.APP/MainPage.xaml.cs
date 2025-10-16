using imicCharge.APP.Services;

namespace imicCharge.APP;

public partial class MainPage : ContentPage
{
    private readonly ApiService _apiService;

    public MainPage()
    {
        InitializeComponent();
        _apiService = new ApiService();

        StartChargeButton.Clicked += OnStartChargeClicked;
        StopChargeButton.Clicked += OnStopChargeClicked;
        TopUpButton.Clicked += OnTopUpClicked;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await UpdateBalanceDisplay();
    }

    private async Task UpdateBalanceDisplay()
    {
        var balance = await _apiService.GetAccountBalanceAsync();
        if (balance.HasValue)
        {
            BalanceLabel.Text = $"{balance.Value:C}"; // Format as currency ("kr 123,45")
        }
        else
        {
            BalanceLabel.Text = "Feil";
            // TODO: Consider showing improved error message or redirect user to login page
        }
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        SecureStorage.Remove("access_token");
        await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
    }

    private async void OnStartChargeClicked(object sender, EventArgs e)
    {
        var chargerId = ChargerIdEntry.Text;
        if (string.IsNullOrWhiteSpace(chargerId))
        {
            await DisplayAlert("Feil", "Du må skrive inn ein ladar-ID.", "OK");
            return;
        }

        var success = await _apiService.StartChargingAsync(chargerId);

        if (success)
        {
            await DisplayAlert("Suksess", $"Førespurnad om å starte lading på ladar {chargerId} er sendt.", "OK");
        }
        else
        {
            await DisplayAlert("Feil", "Kunne ikkje starte lading. Sjekk saldo og prøv igjen.", "OK");
        }
    }

    private async void OnStopChargeClicked(object sender, EventArgs e)
    {
        var chargerId = ChargerIdEntry.Text;
        if (string.IsNullOrWhiteSpace(chargerId))
        {
            await DisplayAlert("Feil", "Du må skrive inn ein ladar-ID.", "OK");
            return;
        }

        var response = await _apiService.StopChargingAsync(chargerId);

        if (response != null)
        {
            // Show message from API response and update visible account balance
            await DisplayAlert("Lading stoppa", response.Message, "OK");
            BalanceLabel.Text = $"{response.NewBalance:C}";
        }
        else
        {
            await DisplayAlert("Feil", "Kunne ikkje stoppe lading.", "OK");
        }
    }

    private async void OnTopUpClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(TopUpPage));
    }

}