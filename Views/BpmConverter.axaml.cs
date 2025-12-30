using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace cmifloppy_linux.Views;

/// <summary>
/// BpmConverter window class
/// </summary>
public partial class BpmConverter : Window
{
    public BpmConverter()
    {
        InitializeComponent();

        // If BpmSlider value changes, update BpmText and SpeedText
        BpmSlider.PropertyChanged += (sender, e) =>
        {
            if (e.Property.Name == "Value")
            {
                // Update BpmText
                BpmText.Text = BpmSlider.Value.ToString("0");
                // Update SpeedText by calculating the speed from the BPM
                SpeedText.Text = Math.Round(314140.625d / BpmSlider.Value).ToString("0");
            }
        };
    }
}