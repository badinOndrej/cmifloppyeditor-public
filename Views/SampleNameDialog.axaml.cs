using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace cmifloppy_linux.Views;

/// <summary>
/// A dialog to set the new name of a sample.
/// </summary>
public partial class SampleNameDialog : Window
{
    /// <summary>
    /// The new name of the sample.
    /// </summary>
    public string SampleName { get; private set; } = "";
    /// <summary>
    /// True if the dialog was canceled.
    /// </summary>
    public bool IsCanceled { private set; get; } = true;

    public SampleNameDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// On Cancel button click, set IsCanceled to true and close the dialog.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        IsCanceled = true;
        Close();
    }

    /// <summary>
    /// On Create button click, set IsCanceled to false, set the new name of the sample and close the dialog.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CreateClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        IsCanceled = false;
        SampleName = SampleNameTextBox.Text ?? "";
        Close();
    }
}