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
        TopUpButton.Clicked += OnTopUpClicked;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await UpdateBalanceDisplay();
        await LoadChargers();
    }

    private async Task LoadChargers()
    {
        var chargers = await _apiService.GetChargersAsync();
        if (chargers != null)
        {
            ChargersView.ItemsSource = chargers;
        }
        else
        {
            ChargeStatusLabel.Text = "Kunne ikkje hente ladarar.";
        }
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

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        SecureStorage.Remove("access_token");
        await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
    }

    private async void OnStartChargeClicked(object? sender, EventArgs e)
    {
        var selectedCharger = ChargersView.SelectedItem as EaseeCharger;

        if (selectedCharger == null)
        {
            ChargeStatusLabel.Text = "Du må velje ein ladar frå lista.";
            return;
        }

        if (string.IsNullOrEmpty(selectedCharger.Id))
        {
            ChargeStatusLabel.Text = "Den valde ladaren har ingen gyldig ID.";
            return;
        }

        ChargeStatusLabel.Text = "Sender førespurnad...";
        var success = await _apiService.StartChargingAsync(selectedCharger.Id);

        if (success)
        {
            await Shell.Current.GoToAsync($"{nameof(ChargingPage)}?ChargerId={selectedCharger.Id}");
        }
        else
        {
            ChargeStatusLabel.Text = "Kunne ikkje starte lading.";
        }
    }

    private async void OnTopUpClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(TopUpPage));
    }

}