using imicCharge.APP.Services;

namespace imicCharge.APP;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _apiService;

    public LoginPage()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        StatusLabel.Text = "Loggar inn...";
        var email = EmailEntry.Text;
        var password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            StatusLabel.Text = "E-post og passord kan ikkje vere tomme.";
            return;
        }

        var loginResponse = await _apiService.LoginAsync(email, password);

        if (loginResponse?.AccessToken != null)
        {
            await SecureStorage.SetAsync("access_token", loginResponse.AccessToken);

            if (Application.Current?.Windows[0] != null)
            {
                Application.Current.Windows[0].Page = new AppShell();
            }
        }
        else
        {
            StatusLabel.Text = "Innlogging feila. Sjekk e-post og passord.";
        }
    }

    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        StatusLabel.Text = "Registrerer...";
        var email = EmailEntry.Text;
        var password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            StatusLabel.Text = "E-post og passord kan ikkje vere tomme.";
            return;
        }

        bool success = await _apiService.RegisterAsync(email, password);

        if (success)
        {
            StatusLabel.Text = "Brukar registrert. Ver venleg og logg inn.";
        }
        else
        {
            StatusLabel.Text = "Registrering feila. Prøv ei anna e-postadresse.";
        }
    }
}
