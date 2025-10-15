using imicCharge.APP.Services; // Legg til denne

namespace imicCharge.APP;

public partial class MainPage : ContentPage
{
    private readonly ApiService _apiService;

    public MainPage()
    {
        InitializeComponent();
        _apiService = new ApiService();
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
}