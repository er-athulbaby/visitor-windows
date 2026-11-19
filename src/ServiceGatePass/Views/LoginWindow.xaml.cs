using System.Globalization;
using System.Windows;
using ServiceGatePass.Data;
using ServiceGatePass.Services;

namespace ServiceGatePass.Views;

public partial class LoginWindow : Window
{
    public User? User { get; private set; }

    public LoginWindow()
    {
        InitializeComponent();
        FlowDirection = L.IsArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        using var db = App.OpenDb();
        var settings = new SettingsStore(db);
        HotelNameText.Text = $"{settings.Get(SettingsStore.HotelName)} · {L.T("Service gate")}".ToUpper(CultureInfo.CurrentCulture);
        LogoImage.Source = App.LoadImage(settings.Get(SettingsStore.LogoPath));
        LogoTile.Visibility = LogoImage.Source == null ? Visibility.Collapsed : Visibility.Visible;
        BrandMark.Visibility = LogoImage.Source == null ? Visibility.Visible : Visibility.Collapsed;
        InsideNowText.Text = db.Visits.Count(v => v.CheckOutAt == null).ToString();
        VisitsTodayText.Text = db.Visits.Count(v => v.CheckInAt >= DateTime.Today).ToString();
        Loaded += (_, _) => UsernameBox.Focus();
    }

    void SignIn_Click(object sender, RoutedEventArgs e)
    {
        using var db = App.OpenDb();
        User = new UserService(db).Authenticate(UsernameBox.Text, PasswordInput.Password);
        if (User == null)
        {
            ErrorText.Text = L.T("Wrong username or password.");
            PasswordInput.Clear();
            PasswordInput.Focus();
            return;
        }
        DialogResult = true;
    }
}
