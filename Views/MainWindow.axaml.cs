using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.Platform.Storage;
using cmifloppy_linux.Services;
using cmifloppy_linux.Utilities;
using cmifloppy_linux.ViewModels;
using MsBox.Avalonia;

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

/// <summary>
/// Dialog service for the main view
/// </summary>
public class MainViewDialogService : IDialogService
{
    /// <summary>
    /// Show a dialog to create a new disk image
    /// </summary>
    /// <returns></returns>
    public async Task<(string? diskName, string? diskOwner)> ShowNewImageDialog()
    {
        var dialog = new NewImageDialog();
        await dialog.ShowDialog((Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow!);
        
        if(dialog.IsCanceled) return (null, null);

        return (dialog.DiskName, dialog.DiskOwner);
    }

    /// <summary>
    /// Show a dialog to open a disk image
    /// </summary>
    /// <returns></returns>
    public async Task<string?> ShowOpenImageDialog()
    {
        // Get top level from the current control. Alternatively, you can use Window reference instead.
        var topLevel = TopLevel.GetTopLevel((Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow!);

        // Start async operation to open the dialog.
        var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Disk Image",
            AllowMultiple = false,
            FileTypeFilter = new[] { FileTypes.DiskImage }
        });

        if (files.Count >= 1)
        {
            return files[0].Path.AbsolutePath;
        }

        return null;
    }

    /// <summary>
    /// Show a dialog to save a disk image
    /// </summary>
    /// <returns></returns>
    public async Task<(string? path, bool overwrite)> ShowSaveImageDialog()
    {
        // Get top level from the current control. Alternatively, you can use Window reference instead.
        var topLevel = TopLevel.GetTopLevel((Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow!);

        // Start async operation to open the dialog.
        var file = await topLevel!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Disk Image",
            DefaultExtension = "img",
            FileTypeChoices = new[] { FileTypes.DiskImage }
        });

        return (file?.Path.AbsolutePath, file is not null ? File.Exists(file.Path.AbsolutePath) : false);
    }

    /// <summary>
    /// Show a serious error dialog
    /// </summary>
    /// <returns></returns>
    public async Task ShowErrorDialog() {
        await MessageBoxManager.GetMessageBoxStandard("Error", "A serious error occurred. The application will now close.", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowWindowDialogAsync((Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow!);
    }

    /// <summary>
    /// Show an error dialog regarding invalid WAV files
    /// </summary>
    /// <returns></returns>
    public async Task ShowInvalidWavDialog() {
        await MessageBoxManager.GetMessageBoxStandard("Error", "Only use 1ch unsigned 8bit WAV files.", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowWindowDialogAsync((Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow!);
    }

    /// <summary>
    /// Show open sample dialog
    /// </summary>
    /// <returns></returns>
    public async Task<(string? path, string? sampleName)> ShowOpenSampleDialog() {
        var topLevel = TopLevel.GetTopLevel((Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow!);

        var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Sample",
            AllowMultiple = false,
            FileTypeFilter = new[] { FileTypes.WavFile }
        });

        if (files.Count >= 1)
        {
            SampleNameDialog snd = new SampleNameDialog();
            await snd.ShowDialog((Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow!);
            if(!snd.IsCanceled) return (files[0].Path.AbsolutePath, snd.SampleName);
        }

        return (null, null);
    }

    /// <summary>
    /// Show save sample dialog
    /// </summary>
    /// <returns></returns>
    public async Task<string?> ShowSaveSampleDialog() {
        var topLevel = TopLevel.GetTopLevel((Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow!);

        var file = await topLevel!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Sample",
            DefaultExtension = "wav",
            FileTypeChoices = new[] { FileTypes.WavFile, FileTypes.VcFile }
        });

        return file?.Path.AbsolutePath;
    }

    /// <summary>
    /// Show sample name dialog
    /// </summary>
    /// <returns></returns>
    public async Task<string?> ShowSampleNameDialog() {
        SampleNameDialog snd = new SampleNameDialog();
        await snd.ShowDialog((Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow!);
        if(!snd.IsCanceled) return snd.SampleName;
        return null;
    }

    /// <summary>
    /// Show save IMD dialog
    /// </summary>
    /// <returns></returns>
    public async Task<string?> ShowSaveImdDialog() {
        var topLevel = TopLevel.GetTopLevel((Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow!);

        var file = await topLevel!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save IMD",
            DefaultExtension = "imd",
            FileTypeChoices = new[] { FileTypes.ImageDisk }
        });

        return file?.Path.AbsolutePath;
    }

    public async Task<string?> ShowSaveMfiDialog() {
        var topLevel = TopLevel.GetTopLevel((Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow!);

        var file = await topLevel!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save MFI",
            DefaultExtension = "mfi",
            FileTypeChoices = new[] { FileTypes.MameFloppyImage }
        });

        return file?.Path.AbsolutePath;
    }

    public async Task<string?> ShowSaveMfmDialog() {
        var topLevel = TopLevel.GetTopLevel((Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow!);

        var file = await topLevel!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save MFM",
            DefaultExtension = "mfm",
            FileTypeChoices = new[] { FileTypes.MfmFloppyImage }
        });

        return file?.Path.AbsolutePath;
    }

    /// <summary>
    /// Show invalid sample dialog
    /// </summary>
    /// <returns></returns>
    public async Task ShowInvalidSampleDialog() {
        await MessageBoxManager.GetMessageBoxStandard("Error", "The selected file is not an exportable sample.", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowWindowDialogAsync((Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow!);
    }
}