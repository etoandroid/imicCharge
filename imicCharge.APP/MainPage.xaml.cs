namespace imicCharge.APP;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        // TODO: Clear stored token

        await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
    }
}