using Avalonia.Platform.Storage;

namespace cmifloppy_linux.Utilities;

/// <summary>
/// Provides file types for use with file pickers.
/// </summary>
public static class FileTypes {
    /// <summary>
    /// File type for disk images.
    /// </summary>
    public static FilePickerFileType DiskImage { get; } = new("Disk images")
    {
        Patterns = new[] { "*.img" },
    };

    /// <summary>
    /// File type for ImageDisk images.
    /// </summary>
    public static FilePickerFileType ImageDisk { get; } = new("ImageDisk images")
    {
        Patterns = new[] { "*.imd" },
    };

    /// <summary>
    /// File type for MFI images.
    /// </summary>
    public static FilePickerFileType MameFloppyImage { get; } = new("MAME floppy images")
    {
        Patterns = new[] { "*.mfi" },
    };

    /// <summary>
    /// File type for MFM floppy images.
    /// </summary>
    public static FilePickerFileType MfmFloppyImage { get; } = new("MFM floppy images")
    {
        Patterns = new[] { "*.mfm" },
    };

    /// <summary>
    /// File type for WAV files.
    /// </summary>
    public static FilePickerFileType WavFile { get; } = new("WAV files")
    {
        Patterns = new[] { "*.wav" },
    };

    /// <summary>
    /// File type for Voice Card files.
    /// </summary>
    public static FilePickerFileType VcFile { get; } = new("Voice card files")
    {
        Patterns = new[] { "*.vc" },
    };
}