using System.Windows;
using EmployeeCrud.Data;
using Microsoft.Data.Sqlite;

namespace EmployeeCrud;

public partial class LoginWindow : Window
{
    private readonly AdminRepository _admins;
    private bool _setupMode;
    private bool _busy;

    public LoginWindow(AdminRepository admins)
    {
        _admins = admins;

        InitializeComponent();

        _setupMode = !_admins.HasAdmin();
        UpdateMode();

        Loaded += (_, _) => UsernameBox.Focus();

        // Prevent the dialog from closing while an authentication
        // operation is still completing.
        Closing += (_, e) =>
        {
            if (_busy)
                e.Cancel = true;
        };
    }

    private void UpdateMode()
    {
        HeadingText.Text = _setupMode
            ? "Create your admin"
            : "Admin login";

        DescriptionText.Text = _setupMode
            ? "First-time setup: choose a username and a password " +
              "of at least 12 characters."
            : "Sign in to manage employees.";

        ConfirmPanel.Visibility = _setupMode
            ? Visibility.Visible
            : Visibility.Collapsed;

        SubmitButton.Content = _setupMode
            ? "Create admin account"
            : "Log in";
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;

        SubmitButton.IsEnabled = !busy;
        UsernameBox.IsEnabled = !busy;
        PasswordInput.IsEnabled = !busy;
        ConfirmPasswordInput.IsEnabled = !busy;
    }

    private async void Submit_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_busy)
            return;

        MessageText.Text = "";

        // Read WPF controls on the UI thread before using Task.Run.
        string username = UsernameBox.Text.Trim();
        string password = PasswordInput.Password;

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrEmpty(password))
        {
            MessageText.Text =
                "Enter your username and password.";
            return;
        }

        if (_setupMode &&
            password != ConfirmPasswordInput.Password)
        {
            MessageText.Text = "The passwords do not match.";
            return;
        }

        SetBusy(true);

        try
        {
            if (_setupMode)
            {
                await Task.Run(() =>
                    _admins.CreateAdmin(username, password));

                _setupMode = false;
                UpdateMode();

                PasswordInput.Clear();
                ConfirmPasswordInput.Clear();

                MessageText.Text =
                    "Admin account created. Log in to continue.";
            }
            else
            {
                bool valid = await Task.Run(() =>
                    _admins.ValidateLogin(username, password));

                if (!valid)
                {
                    // A small delay discourages rapid repeated attempts.
                    // This is not a replacement for a real lockout policy.
                    await Task.Delay(1000);

                    PasswordInput.Clear();
                    MessageText.Text =
                        "Invalid username or password.";
                    return;
                }

                PasswordInput.Clear();

                // Setting DialogResult closes a modal WPF window.
                SetBusy(false);
                DialogResult = true;
            }
        }
        catch (ArgumentException ex)
        {
            MessageText.Text = ex.Message;
        }
        catch (SqliteException)
        {
            MessageText.Text =
                "The account database could not be updated or read. " +
                "Close other app instances and restart the application.";
        }
        catch (Exception)
        {
            MessageText.Text =
                "Sign-in could not be completed. Please try again.";
        }
        finally
        {
            SetBusy(false);
        }
    }
}