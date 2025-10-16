using imicCharge.APP.Services;

namespace imicCharge.APP;

public partial class TopUpPage : ContentPage
{
    private readonly ApiService _apiService;

    public TopUpPage()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    private async void OnPayButtonClicked(object sender, EventArgs e)
    {
        if (!decimal.TryParse(AmountEntry.Text, out var amount) || amount <= 0)
        {
            StatusLabel.Text = "Ver venleg og skriv inn eit gyldig beløp.";
            return;
        }

        StatusLabel.Text = "Gjer klar betaling...";
        var checkoutUrl = await _apiService.CreateCheckoutSessionAsync(amount);

        if (string.IsNullOrEmpty(checkoutUrl))
        {
            StatusLabel.Text = "Kunne ikkje starte betaling. Prøv igjen seinare.";
            return;
        }

        // Send URL-en til PaymentPage
        await Shell.Current.GoToAsync(nameof(PaymentPage), new Dictionary<string, object>
    {
        { "CheckoutUrl", checkoutUrl }
    });
    }

}