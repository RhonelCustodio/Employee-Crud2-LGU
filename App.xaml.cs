using System.IO;
using System.Windows;
using EmployeeCrud.Data;

namespace EmployeeCrud;

public partial class App : Application
{
    private EmployeeRepository _employees = null!;
    private AdminRepository _admins = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Keep the application running while switching windows.
        ShutdownMode =
            System.Windows.ShutdownMode.OnExplicitShutdown;

        try
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "EmployeeCrud");

            Directory.CreateDirectory(folder);

            string databasePath = Path.Combine(
                folder,
                "employees.db");

            _employees = new EmployeeRepository(databasePath);
            _admins = new AdminRepository(databasePath);

            _employees.Initialize();
            _admins.Initialize();

            ShowLoginWindow();
        }
        catch (Exception ex)
        {
            ShowApplicationError(ex);
        }
    }

    private void ShowLoginWindow()
    {
        ShutdownMode =
            System.Windows.ShutdownMode.OnExplicitShutdown;

        // Always create a fresh login window.
        // Previous username/password fields are not reused.
        var loginWindow = new LoginWindow(_admins);

        MainWindow = loginWindow;

        bool? loggedIn = loginWindow.ShowDialog();

        if (loggedIn != true)
        {
            // Closing login without signing in exits the app.
            Shutdown();
            return;
        }

        // Create a fresh CRUD window only after successful login.
        var employeeWindow = new MainWindow(_employees);

        MainWindow = employeeWindow;

        ShutdownMode =
            System.Windows.ShutdownMode.OnMainWindowClose;

        employeeWindow.Show();
    }

    public void Logout()
    {
        try
        {
            // Change this BEFORE closing the main window.
            // Otherwise WPF would shut down the application.
            ShutdownMode =
                System.Windows.ShutdownMode.OnExplicitShutdown;

            // Close the CRUD window and discard its form state.
            MainWindow?.Close();

            // Require authentication again.
            ShowLoginWindow();
        }
        catch (Exception ex)
        {
            ShowApplicationError(ex);
        }
    }

    private void ShowApplicationError(Exception ex)
    {
        MessageBox.Show(
            $"The application could not continue.\n\n{ex.Message}",
            "Application error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        Shutdown(1);
    }
}