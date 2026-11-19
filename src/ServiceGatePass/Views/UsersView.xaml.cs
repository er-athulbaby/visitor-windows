using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ServiceGatePass.Data;
using ServiceGatePass.Services;

namespace ServiceGatePass.Views;

public partial class UsersView : UserControl
{
    public UsersView()
    {
        InitializeComponent();
        Load();
    }

    void Load()
    {
        var selectedId = (UsersGrid.SelectedItem as User)?.Id;
        using var db = App.OpenDb();
        var users = new UserService(db).All();
        UsersGrid.ItemsSource = users;
        UsersGrid.SelectedItem = users.FirstOrDefault(u => u.Id == selectedId);
    }

    void Msg(string text, bool error)
    {
        MessageText.Text = text;
        MessageText.Foreground = (Brush)FindResource(error ? "ErrorBrush" : "SuccessBrush");
    }

    bool Act(Action<UserService> action)
    {
        try
        {
            using (var db = App.OpenDb()) action(new UserService(db));
            Load();
            Msg(L.T("Done."), false);
            return true;
        }
        catch (GateException ex)
        {
            Msg(ex.Message, true);
            return false;
        }
    }

    void WithSelected(Action<UserService, User> action)
    {
        if (UsersGrid.SelectedItem is not User user) { Msg(L.T("Select a user first."), true); return; }
        Act(s => action(s, user));
    }

    void Add_Click(object sender, RoutedEventArgs e)
    {
        if (Act(s => s.Create(UsernameBox.Text, DisplayNameBox.Text, PasswordInput.Password, (Role)RoleBox.SelectedIndex)))
        {
            UsernameBox.Text = DisplayNameBox.Text = "";
            PasswordInput.Clear();
        }
    }

    void Reset_Click(object sender, RoutedEventArgs e)
    {
        WithSelected((s, u) => s.ResetPassword(u.Id, PasswordInput.Password));
        PasswordInput.Clear();
    }

    void Toggle_Click(object sender, RoutedEventArgs e) => WithSelected((s, u) => s.SetActive(u.Id, !u.IsActive));
    void MakeAdmin_Click(object sender, RoutedEventArgs e) => WithSelected((s, u) => s.SetRole(u.Id, Role.Admin));
    void MakeGate_Click(object sender, RoutedEventArgs e) => WithSelected((s, u) => s.SetRole(u.Id, Role.Gate));
}
