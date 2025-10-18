using imicCharge.APP.Services;

namespace imicCharge.APP;

[QueryProperty(nameof(CheckoutUrl), "CheckoutUrl")]
public partial class PaymentPage : ContentPage
{
    public string? CheckoutUrl { get; set; }

    public PaymentPage()
    {
        InitializeComponent();
        // Bruk Navigated, ikkje Navigating
        StripeWebView.Navigated += OnWebViewNavigated;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        StripeWebView.IsVisible = true; // Sørg for at den er synleg

        if (!string.IsNullOrEmpty(CheckoutUrl))
        {
            StripeWebView.Source = new UrlWebViewSource { Url = CheckoutUrl };
        }
        else
        {
            HandleMissingUrl();
        }
    }

    private async void HandleMissingUrl()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await PopupService.ShowAlertAsync("Feil", "Kunne ikkje laste betalingsside.", "OK");
            if (Shell.Current.CurrentState.Location.OriginalString != "..")
            {
                await Shell.Current.GoToAsync("..");
            }
        });
    }

    private void OnWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        string currentUrl = e.Url;
        System.Diagnostics.Debug.WriteLine($"WebView Navigated to: {currentUrl}, Result: {e.Result}");

        bool shouldNavigateBack = false;

        if (currentUrl.Contains("payment_success"))
        {
            // Sjekk resultatet for å vera sikker
            if (e.Result == WebNavigationResult.Success || e.Result == WebNavigationResult.Failure)
            {
                shouldNavigateBack = true;
            }
        }
        else if (currentUrl.Contains("payment_cancel"))
        {
            if (e.Result == WebNavigationResult.Success || e.Result == WebNavigationResult.Failure)
            {
                shouldNavigateBack = true;
            }
        }

        if (shouldNavigateBack)
        {
            // Skjul WebView umiddelbart
            StripeWebView.IsVisible = false;
            StripeWebView.Source = null;

            // Naviger tilbake til MainPage MED EIN GONG utan popup
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (Shell.Current.CurrentState.Location.OriginalString != "..")
                {
                    await Shell.Current.GoToAsync("..");
                }
                // Ingen popup her lenger
            });
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StripeWebView.Navigated -= OnWebViewNavigated;
        StripeWebView.Source = null; // Stopp lasting
    }
}