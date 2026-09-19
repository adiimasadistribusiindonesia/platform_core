using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Threading;

namespace Core.Adiimasa;

public partial class MainWindow : Window
{
    private const string AppUrl = "https://adiimasadistribusiindonesia.github.io/platform_core/";
    private readonly string _userDataFolder;
    private readonly DispatcherTimer _networkTimer;
    private bool _offline;

    public MainWindow()
    {
        InitializeComponent();

        _userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Adiimasa", "Core", "WebView2");

        _networkTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _networkTimer.Tick += NetworkTimer_Tick;

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(_userDataFolder);

            string? version = null;
            try
            {
                version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            }
            catch { }

            if (string.IsNullOrWhiteSpace(version))
            {
                ShowStartupError(
                    "Microsoft Edge WebView2 Runtime belum tersedia di komputer ini.\n\n" +
                    "Silakan jalankan Windows Update atau instal Microsoft Edge WebView2 Runtime, lalu buka Core Adiimasa kembali.");
                return;
            }

            var environment = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: _userDataFolder,
                options: null);

            await Browser.EnsureCoreWebView2Async(environment);
            ConfigureBrowser(Browser.CoreWebView2);
            Browser.CoreWebView2.Navigate(AppUrl);
        }
        catch (Exception ex)
        {
            ShowStartupError(
                "Core Adiimasa tidak dapat dijalankan.\n\n" +
                "Detail: " + ex.Message);
        }
    }

    private void ShowStartupError(string message)
    {
        Browser.Visibility = Visibility.Collapsed;
        OfflinePanel.Visibility = Visibility.Visible;

        MessageBox.Show(
            this,
            message,
            "Core Adiimasa",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void ConfigureBrowser(CoreWebView2 web)
    {
        web.Settings.AreDefaultContextMenusEnabled = true;
        web.Settings.AreDevToolsEnabled = false;
        web.Settings.IsStatusBarEnabled = false;
        web.Settings.AreBrowserAcceleratorKeysEnabled = true;
        web.ScriptDialogOpening += Web_ScriptDialogOpening;

        web.AddWebResourceRequestedFilter(
            "https://adiimasadistribusiindonesia.github.io/platform_core/*",
            CoreWebView2WebResourceContext.All);

        web.WebResourceRequested += Web_WebResourceRequested;
        web.NavigationCompleted += Web_NavigationCompleted;
        web.NewWindowRequested += Web_NewWindowRequested;
        web.DownloadStarting += Web_DownloadStarting;
        web.ProcessFailed += Web_ProcessFailed;
    }

    private void Web_ScriptDialogOpening(object? sender, CoreWebView2ScriptDialogOpeningEventArgs e)
    {
        e.SuppressDefaultDialog = true;

        Dispatcher.Invoke(() =>
        {
            var title = "Core Adiimasa";
            if (e.Kind == CoreWebView2ScriptDialogKind.Confirm)
            {
                var result = MessageBox.Show(
                    this,
                    e.Message,
                    title,
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.OK)
                    e.Accept();
            }
            else if (e.Kind == CoreWebView2ScriptDialogKind.Alert)
            {
                MessageBox.Show(
                    this,
                    e.Message,
                    title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                e.Accept();
            }
            else
            {
                // Prompt dialogs are not used by the Core web application.
                // Keep the default WebView2 behavior available if one is ever introduced.
                e.SuppressDefaultDialog = false;
            }
        });
    }

    private void Web_WebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        e.Request.Headers.SetHeader("Cache-Control", "no-cache");
    }

    private void Web_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess) ShowBrowser();
        else ShowOffline();
    }

    private void Web_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;
        if (!string.IsNullOrWhiteSpace(e.Uri))
            Browser.CoreWebView2.Navigate(e.Uri);
    }

    private void Web_ProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
    {
        Dispatcher.Invoke(() =>
            ShowStartupError("WebView2 mengalami kegagalan proses. Silakan tutup Core Adiimasa dan buka kembali."));
    }

    private void Web_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            var suggestedName = Path.GetFileName(e.ResultFilePath);
            if (string.IsNullOrWhiteSpace(suggestedName))
                suggestedName = "Core Adiimasa";

            var dialog = new SaveFileDialog
            {
                Title = "Simpan File Core Adiimasa",
                FileName = suggestedName,
                Filter = BuildFilter(suggestedName),
                AddExtension = true,
                OverwritePrompt = true
            };

            if (dialog.ShowDialog(this) == true)
            {
                e.ResultFilePath = dialog.FileName;
                e.Handled = true;
            }
            else
            {
                e.Cancel = true;
                e.Handled = true;
            }
        });
    }

    private static string BuildFilter(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".xlsx" => "Excel Workbook (*.xlsx)|*.xlsx|All files (*.*)|*.*",
            ".xls" => "Excel Workbook (*.xls)|*.xls|All files (*.*)|*.*",
            ".csv" => "CSV (*.csv)|*.csv|All files (*.*)|*.*",
            ".json" => "JSON Backup (*.json)|*.json|All files (*.*)|*.*",
            ".pdf" => "PDF (*.pdf)|*.pdf|All files (*.*)|*.*",
            _ => "All files (*.*)|*.*"
        };
    }

    private void RetryButton_Click(object sender, RoutedEventArgs e) => TryNavigate();

    private void TryNavigate()
    {
        if (Browser.CoreWebView2 is null) return;
        ShowLoading();
        try { Browser.CoreWebView2.Navigate(AppUrl); }
        catch { ShowOffline(); }
    }

    private void ShowLoading()
    {
        OfflinePanel.Visibility = Visibility.Collapsed;
        Browser.Visibility = Visibility.Visible;
    }

    private void ShowBrowser()
    {
        _offline = false;
        _networkTimer.Stop();
        OfflinePanel.Visibility = Visibility.Collapsed;
        Browser.Visibility = Visibility.Visible;
    }

    private void ShowOffline()
    {
        _offline = true;
        Browser.Visibility = Visibility.Collapsed;
        OfflinePanel.Visibility = Visibility.Visible;
        _networkTimer.Start();
    }

    private void NetworkTimer_Tick(object? sender, EventArgs e)
    {
        if (!_offline || !NetworkInterface.GetIsNetworkAvailable()) return;
        TryNavigate();
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        _networkTimer.Stop();
    }
}
