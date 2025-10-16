namespace imicCharge.APP;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        Routing.RegisterRoute(nameof(TopUpPage), typeof(TopUpPage));
        Routing.RegisterRoute(nameof(PaymentPage), typeof(PaymentPage));
    }
}