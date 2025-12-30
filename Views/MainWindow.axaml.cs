using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Logging;
using cmifloppy_linux.ViewModels;

namespace cmifloppy_linux.Views;

public partial class MainWindow : Window
{
    /// <summary>
    /// Check if Wine is installed
    /// </summary>
    /// <returns></returns>
    public bool IsWineInstalled()
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "wine",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return !string.IsNullOrEmpty(output) && output.StartsWith("wine");
        }
        catch
        {
            return false;
        }
    }

    public MainWindow()
    {
        InitializeComponent();

        Closed += (sender, e) => {
            if(DataContext is MainWindowViewModel vm) vm.QuitCommand(null);
        };

        BpmSpeedMenuItem.Click += OnBpmSpeedClicked;

        if(OperatingSystem.IsLinux())
            if (!IsWineInstalled()) // If Wine is not installed
            {
                // Show error message and close the application
                Logger.TryGet(LogEventLevel.Fatal, "MainWindow")?.Log(this, "Wine is not installed. Please install Wine and try again.");
                System.Environment.Exit(1);
            }
    }

    public void OnBpmSpeedClicked(object? sender, RoutedEventArgs e)
    {
        var bpmConverter = new BpmConverter();
        bpmConverter.Show();
    }
}
