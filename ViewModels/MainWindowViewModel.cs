namespace cmifloppy_linux.ViewModels;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using cmifloppy_linux.Services;

/// <summary>
/// View model for the main window.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>
    /// Dialog service to show dialogs.
    /// </summary>
    private readonly IDialogService _dialogService;

    public MainWindowViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    /// <summary>
    /// Index of the selected item in the disk directory.
    /// </summary>
    private int _directoryIndex = -1;
    public int DirectoryIndex { get => _directoryIndex; set { _directoryIndex = value; } }

    /// <summary>
    /// Path to the current disk image.
    /// </summary>
    private string _currentDiskImage = "N/A";
    public string CurrentDiskImage { get => _currentDiskImage; set {_currentDiskImage = value; OnPropertyChanged(); } }

    /// <summary>
    /// Indicates whether the controls are enabled.
    /// </summary>
    private bool _isEnabledControls = false;
    public bool IsEnabledControls { get => _isEnabledControls; set { _isEnabledControls = value; OnPropertyChanged(); } }

    /// <summary>
    /// List of samples in the disk directory.
    /// </summary>
    private List<string> _diskDirectory = new();
    public List<string> DiskDirectory { get => _diskDirectory; set { _diskDirectory = value; OnPropertyChanged(); } }

    /// <summary>
    /// CmiOSservice to interact with the cmios9.
    /// </summary>
    private CmiOsService? _cmiOsService;

    /// <summary>
    /// Command to quit the application.
    /// </summary>
    /// <param name="message"></param>
    public void QuitCommand(object? message) {
        if (_cmiOsService is not null) {
            _cmiOsService.StopProcess();
        }
        Environment.Exit(0);
    }

    /// <summary>
    /// Command to create a new disk image.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task CreateImageCommand(object? message) {
        if (_cmiOsService is not null) {
            _cmiOsService.StopProcess();
            _cmiOsService = null;
        }

        // Show the save image dialog
        var (path, overwrite) = await _dialogService.ShowSaveImageDialog();
        // If the path is not null, create a new disk image
        if(path is not null) {
            var (diskName, diskOwner) = await _dialogService.ShowNewImageDialog();

            if(diskName is not null && diskOwner is not null) {
                CurrentDiskImage = path;

                if(File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}Files/empty.img")) {
                    if(overwrite) {
                        File.Delete(path);
                    }
                    File.Copy($"{AppDomain.CurrentDomain.BaseDirectory}Files/empty.img", path);
                    SetDiskInfo(diskName, diskOwner);

                    // Start the cmios9 process
                    _cmiOsService = new CmiOsService(path);
                    await _cmiOsService.StartProcess();

                    // List the directory
                    await ListDirectory();
                    // Enable the controls
                    IsEnabledControls = true;
                } else {
                    await _dialogService.ShowErrorDialog();
                    Environment.Exit(1);
                }
            }
        }
    }

    /// <summary>
    /// Command to open an existing disk image.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task OpenImageCommand(object? message) {
        var path = await _dialogService.ShowOpenImageDialog();
        if(path is not null) {
            if (_cmiOsService is not null) {
                _cmiOsService.StopProcess();
                _cmiOsService = null;
            }

            _cmiOsService = new CmiOsService(path);
            await _cmiOsService.StartProcess();
            await ListDirectory();

            CurrentDiskImage = path;
            IsEnabledControls = true;
        } // else open image dialog was cancelled
    }

    /// <summary>
    /// Command to import a sample to the disk image.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task ImportSampleCommand(object? message) {
        var (path, sampleName) = await _dialogService.ShowOpenSampleDialog();
        if(path is not null && sampleName is not null) {
            if(_cmiOsService is not null) {
                // Check if the WAV file is mono and 8-bit unsigned
                if(IsMono8BitWav(path)) {
                    // Import the sample
                    await _cmiOsService.ImportSample(path, sampleName);
                    await ListDirectory();
                } else {
                    await _dialogService.ShowInvalidWavDialog();
                }
            }
        }
    }

    /// <summary>
    /// Command to export a sample from the disk image.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task ExportSampleCommand(object? message) {
        if(_cmiOsService is not null) {
            var sampleName = _directoryIndex >= 0 && _directoryIndex < _diskDirectory.Count ? _diskDirectory[_directoryIndex] : null;
            if(sampleName is not null) {
                if(sampleName.EndsWith("VC")) {
                    var path = await _dialogService.ShowSaveSampleDialog();
                    if(path is not null) {
                        if(path.EndsWith(".vc")) {
                            // exporting as native voice card file format
                            await _cmiOsService.ExportSampleAsVc(sampleName, path);
                        } else {
                            // exporting as WAV
                            await _cmiOsService.ExportSample(sampleName, path);
                        }
                    }
                } else {
                    await _dialogService.ShowInvalidSampleDialog();
                }
            }
        }
    }

    /// <summary>
    /// Command to delete a sample from the disk image.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task DeleteSampleCommand(object? message) {
        if(_cmiOsService is not null) {
            var sampleName = _directoryIndex >= 0 && _directoryIndex < _diskDirectory.Count ? _diskDirectory[_directoryIndex] : null;
            if(sampleName is not null) {
                await _cmiOsService.DeleteSample(sampleName);
                await ListDirectory();
            }
        }
    }

    /// <summary>
    /// Command to rename a sample in the disk image.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task RenameSampleCommand(object? message) {
        if(_cmiOsService is not null) {
            var sampleName = _directoryIndex >= 0 && _directoryIndex < _diskDirectory.Count ? _diskDirectory[_directoryIndex] : null;
            if(sampleName is not null) {
                var newSampleName = await _dialogService.ShowSampleNameDialog();
                if(newSampleName is not null) {
                    await _cmiOsService.RenameSample(sampleName, newSampleName);
                    await ListDirectory();
                }
            }
        }
    }

    /// <summary>
    /// Command to convert a disk image to IMD format.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task ConvertDiskToImdCommand(object? message) {
        if(_cmiOsService is not null) {
            _cmiOsService.StopProcess();
            _cmiOsService = null;
        }

        var source = CurrentDiskImage;
        // Show the save IMD dialog
        var target = await _dialogService.ShowSaveImdDialog();

        // Convert the disk image to IMD format
        if((source is not null || source != "") && target is not null) {
            try {
                Process convertProcess;
                if(OperatingSystem.IsLinux())
                    convertProcess = new Process {
                        StartInfo = {
                            FileName = "wine",
                            Arguments = $"\"./Files/bin2imd.exe\" \"{source}\" \"{target}\" /2 DM=0 SS=128 SM=1-26 N=77", // 2 sides, 128B sectors, 26 sectors per track, 77 tracks
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };
                else {
                    convertProcess = new Process {
                        StartInfo = {
                            FileName = $".\\Files\\bin2imd.exe",
                            Arguments = $"\"{source}\" \"{target}\" /2 DM=0 SS=128 SM=1-26 N=77", // 2 sides, 128B sectors, 26 sectors per track, 77 tracks
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };
                }
                convertProcess.Start();
                await convertProcess.WaitForExitAsync();
            } catch(Exception) {
                await _dialogService.ShowErrorDialog();
                Environment.Exit(1);
            }
        }

        _cmiOsService = new CmiOsService(CurrentDiskImage);
        await _cmiOsService.StartProcess();
        await ListDirectory();
    }

    /// <summary>
    /// Command to convert a disk image to MFI format.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task ConvertDiskToMfiCommand(object? message) {
        if(_cmiOsService is not null) {
            _cmiOsService.StopProcess();
            _cmiOsService = null;
        }

        var source = CurrentDiskImage;
        // Show the save MFI dialog
        var target = await _dialogService.ShowSaveMfiDialog();

        // Convert the disk image to MFI format
        if((source is not null || source != "") && target is not null) {
            try {
                Process convertProcess;
                if(OperatingSystem.IsLinux()) {
                    // Convert the disk image to IMD format first
                    convertProcess = new Process {
                        StartInfo = {
                            FileName = "wine",
                            Arguments = $"\"./Files/bin2imd.exe\" \"{source}\" \"/tmp/temp.imd\" /2 DM=0 SS=128 SM=1-26 N=77", // 2 sides, 128B sectors, 26 sectors per track, 77 tracks
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };
                    convertProcess.Start();
                    await convertProcess.WaitForExitAsync();
                    // Convert the IMD file to MFI format
                    convertProcess = new Process {
                        StartInfo = {
                            FileName = "wine",
                            Arguments = $"\"./Files/floptool.exe\" flopconvert imd mfi \"/tmp/temp.imd\" \"{target}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };
                    convertProcess.Start();
                    await convertProcess.WaitForExitAsync();
                    // Delete the temporary IMD file
                    File.Delete("/tmp/temp.imd");
                } else {
                    // Convert the disk image to IMD format first
                    convertProcess = new Process {
                        StartInfo = {
                            FileName = $".\\Files\\bin2imd.exe",
                            Arguments = $"\"{source}\" \".\\temp.imd\" /2 DM=0 SS=128 SM=1-26 N=77", // 2 sides, 128B sectors, 26 sectors per track, 77 tracks
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };
                    convertProcess.Start();
                    await convertProcess.WaitForExitAsync();
                    // Convert the IMD file to MFI format
                    convertProcess = new Process {
                        StartInfo = {
                            FileName = $".\\Files\\floptool.exe",
                            Arguments = $"flopconvert imd mfi .\\temp.imd \"{target}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };
                    convertProcess.Start();
                    await convertProcess.WaitForExitAsync();
                    // Delete the temporary IMD file
                    File.Delete(".\\temp.imd");
                }
            } catch(Exception) {
                await _dialogService.ShowErrorDialog();
                Environment.Exit(1);
            }
        }

        _cmiOsService = new CmiOsService(CurrentDiskImage);
        await _cmiOsService.StartProcess();
        await ListDirectory();
    }

    /// <summary>
    /// Command to convert a disk image to MFM format.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task ConvertDiskToMfmCommand(object? message) {
        if(_cmiOsService is not null) {
            _cmiOsService.StopProcess();
            _cmiOsService = null;
        }

        var source = CurrentDiskImage;
        // Show the save MFM dialog
        var target = await _dialogService.ShowSaveMfmDialog();

        // Convert the disk image to MFI format
        if((source is not null || source != "") && target is not null) {
            try {
                Process convertProcess;
                if(OperatingSystem.IsLinux()) {
                    // Convert the disk image to IMD format first
                    convertProcess = new Process {
                        StartInfo = {
                            FileName = "wine",
                            Arguments = $"\"./Files/bin2imd.exe\" \"{source}\" \"/tmp/temp.imd\" /2 DM=0 SS=128 SM=1-26 N=77", // 2 sides, 128B sectors, 26 sectors per track, 77 tracks
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };
                    convertProcess.Start();
                    await convertProcess.WaitForExitAsync();
                    // Convert the IMD file to MFM format
                    convertProcess = new Process {
                        StartInfo = {
                            FileName = "wine",
                            Arguments = $"\"./Files/floptool.exe\" flopconvert imd mfm \"/tmp/temp.imd\" \"{target}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };
                    convertProcess.Start();
                    await convertProcess.WaitForExitAsync();
                    // Delete the temporary IMD file
                    File.Delete("/tmp/temp.imd");
                } else {
                    // Convert the disk image to IMD format first
                    convertProcess = new Process {
                        StartInfo = {
                            FileName = $".\\Files\\bin2imd.exe",
                            Arguments = $"\"{source}\" \".\\temp.imd\" /2 DM=0 SS=128 SM=1-26 N=77", // 2 sides, 128B sectors, 26 sectors per track, 77 tracks
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };
                    convertProcess.Start();
                    await convertProcess.WaitForExitAsync();
                    // Convert the IMD file to MFM format
                    convertProcess = new Process {
                        StartInfo = {
                            FileName = $".\\Files\\floptool.exe",
                            Arguments = $"flopconvert imd mfm .\\temp.imd \"{target}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };
                    convertProcess.Start();
                    await convertProcess.WaitForExitAsync();
                    // Delete the temporary IMD file
                    File.Delete(".\\temp.imd");
                }
            } catch(Exception) {
                await _dialogService.ShowErrorDialog();
                Environment.Exit(1);
            }
        }

        _cmiOsService = new CmiOsService(CurrentDiskImage);
        await _cmiOsService.StartProcess();
        await ListDirectory();
    }

    /// <summary>
    /// Checks if the specified WAV file is mono and 8-bit unsigned.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    public bool IsMono8BitWav(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The specified file does not exist.", path);
        }

        using (var reader = new BinaryReader(File.OpenRead(path)))
        {
            // Read the RIFF header
            var riff = new string(reader.ReadChars(4));
            if (riff != "RIFF")
            {
                return false;
            }

            reader.ReadInt32(); // File size

            var wave = new string(reader.ReadChars(4));
            if (wave != "WAVE")
            {
                return false;
            }

            // Read the fmt chunk
            var fmt = new string(reader.ReadChars(4));
            if (fmt != "fmt ")
            {
                return false;
            }

            reader.ReadInt32(); // Subchunk1Size
            var audioFormat = reader.ReadInt16();
            var numChannels = reader.ReadInt16();
            var sampleRate = reader.ReadInt32();
            reader.ReadInt32(); // ByteRate
            reader.ReadInt16(); // BlockAlign
            var bitsPerSample = reader.ReadInt16();

            // Check if the WAV file is mono and 8-bit unsigned
            return audioFormat == 1 && numChannels == 1 && bitsPerSample == 8;
        }
    }

    /// <summary>
    /// Lists the directory of the disk image.
    /// </summary>
    /// <returns></returns>
    private async Task ListDirectory() {
        if(_cmiOsService is not null) {
            DiskDirectory = await _cmiOsService.DiskDirectory();
        }
    }

    /// <summary>
    /// Sets the disk name and owner in the disk image.
    /// </summary>
    /// <param name="diskName"></param>
    /// <param name="diskOwner"></param>
    private void SetDiskInfo(string diskName, string diskOwner) {
        // Disk name: 8 bytes starting at byte 0
        // Disk owner: 20 bytes starting at byte 18
        if(File.Exists(CurrentDiskImage)) {
            // read image file
            byte[] diskData = File.ReadAllBytes(CurrentDiskImage);
            // encode disk name and owner
            var diskNameBytes = Encoding.ASCII.GetBytes(diskName);
            var diskOwnerBytes = Encoding.ASCII.GetBytes(diskOwner);
            // copy disk name and owner to disk data
            Array.Copy(diskNameBytes, 0, diskData, 0, Math.Min(diskNameBytes.Length, 8));
            Array.Copy(diskOwnerBytes, 0, diskData, 18, Math.Min(diskOwnerBytes.Length, 20));
            // write disk data back to image file
            File.WriteAllBytes(CurrentDiskImage, diskData);
        }
    }
}
