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

        PopupService.Register(AlertView);
        
        // Set the user's email address in the UI
        AccountName.Text = await SecureStorage.GetAsync("user_email");

        await UpdateBalanceDisplay();
        await LoadChargers();
    }

    private async Task LoadChargers()
    {
        var chargers = await _apiService.GetChargersAsync();
        if (chargers != null)
        {
            // Set ItemsSource for the Picker
            ChargerPicker.ItemsSource = chargers;
        }
        else
        {
            // Using the PopupService for consistency
            await PopupService.ShowAlertAsync("Feil", "Kunne ikkje hente ladarar.");
        }
    }

    private decimal _currentBalance = 0;

    private async Task UpdateBalanceDisplay()
    {
        var balance = await _apiService.GetAccountBalanceAsync();
        if (balance.HasValue)
        {
            _currentBalance = balance.Value; // Store the balance in private field
            BalanceLabel.Text = $"{_currentBalance:C}"; // Format as currency/NOK ("kr 123,45")
    }
        else
        {
            BalanceLabel.Text = "Feil";
        }
    }

    private void OnLogoutClicked(object? sender, EventArgs e)
    {
        SecureStorage.Remove("access_token");
        if (Application.Current?.Windows.FirstOrDefault() is Window window)
        {
            window.Page = new LoginPage();
        }
    }

    private async void OnStartChargeClicked(object? sender, EventArgs e)
    {
        // Get selected item from the Picker
        var selectedCharger = ChargerPicker.SelectedItem as EaseeCharger;

        // Validation 1: Check if a charger is selected
        if (selectedCharger == null)
        {
            await PopupService.ShowAlertAsync("Ingen ladar vald", "Du må velje ein ladar frå lista.");
            return;
        }

        // Validation 2: Check if balance is sufficient
        if (_currentBalance < 20)
        {
            await PopupService.ShowAlertAsync("For lita saldo", "Saldoen din er under kr 20. Fyll på kontoen før du startar lading.");
            return;
        }

        if (string.IsNullOrEmpty(selectedCharger.Id))
        {
            await PopupService.ShowAlertAsync("Feil", "Den valde ladaren har ingen gyldig ID.");
            return;
        }

        ChargeStatusLabel.Text = "Sender førespurnad...";
        var success = await _apiService.StartChargingAsync(selectedCharger.Id);

        if (success)
        {
            // Clear status text on success before navigating
            ChargeStatusLabel.Text = string.Empty;
            await Shell.Current.GoToAsync($"{nameof(ChargingPage)}?ChargerId={selectedCharger.Id}");
        }
        else
        {
            ChargeStatusLabel.Text = string.Empty; // Clear status text
            await PopupService.ShowAlertAsync("Feil", "Kunne ikkje starte lading. Sjekk saldo og prøv igjen.");
        }
    }

    private async void OnTopUpClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(TopUpPage));
    }
}