namespace imicCharge.APP;

[QueryProperty(nameof(CheckoutUrl), "CheckoutUrl")]
public partial class PaymentPage : ContentPage
{
    public string CheckoutUrl { get; set; }

    public PaymentPage()
    {
        InitializeComponent();
        StripeWebView.Navigated += OnWebViewNavigated;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (!string.IsNullOrEmpty(CheckoutUrl))
        {
            StripeWebView.Source = CheckoutUrl;
        }
        else
        {
            DisplayAlert("Feil", "Kunne ikkje laste betalingsside.", "OK");
            Shell.Current.GoToAsync("..");
        }
    }

    private async void OnWebViewNavigated(object sender, WebNavigatedEventArgs e)
    {
        if (e.Url.Contains("payment_success"))
        {
            await DisplayAlert("Suksess", "Betaling fullført! Saldoen din vil bli oppdatert om kort tid.", "OK");
            await Shell.Current.GoToAsync("../..");
        }
        else if (e.Url.Contains("payment_cancel"))
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}