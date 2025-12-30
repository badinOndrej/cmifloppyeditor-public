using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace cmifloppy_linux.Views;

/// <summary>
/// A dialog for creating a new disk image.
/// </summary>
public partial class NewImageDialog : Window
{
    /// <summary>
    /// The name of the disk image.
    /// </summary>
    public string DiskName { private set; get; } = "";
    /// <summary>
    /// The owner of the disk image.
    /// </summary>
    public string DiskOwner { private set; get; } = "";
    /// <summary>
    /// Whether the dialog was canceled.
    /// </summary>
    public bool IsCanceled { private set; get; } = true;

    public NewImageDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// On cancel, set the dialog as canceled and close it.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        IsCanceled = true;
        Close();
    }

    /// <summary>
    /// On create, set the disk name and owner and close the dialog.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CreateClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        IsCanceled = false;
        DiskName = DiskNameTextBox.Text ?? "";
        DiskOwner = DiskOwnerTextBox.Text ?? "";
        Close();
    }
}