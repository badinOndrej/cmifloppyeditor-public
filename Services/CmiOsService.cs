using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Logging;

namespace cmifloppy_linux.Services;

/// <summary>
/// Service for interacting with the cmios9 executable running in Wine.
/// </summary>
public class CmiOsService
{
    /// <summary>
    /// Path to the disk image file.
    /// </summary>
    private readonly string _diskImagePath;
    /// <summary>
    /// The process running the cmios9 executable.
    /// </summary>
    private Process? _wineProcess;
    /// <summary>
    /// The input stream to the process.
    /// </summary>
    private StreamWriter? _processInput;
    /// <summary>
    /// Buffer for storing the output of the process.
    /// </summary>
    private StringBuilder _outputBuffer;

    public CmiOsService(string diskImagePath)
    {
        // Validate the disk image path.
        if (string.IsNullOrEmpty(diskImagePath))
        {
            throw new ArgumentException("Disk image path cannot be null or empty.", nameof(diskImagePath));
        }

        _diskImagePath = diskImagePath;
        _outputBuffer = new StringBuilder();
    }

    /// <summary>
    /// Start cmios9 process
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task StartProcess()
    {
        if (_wineProcess != null && !_wineProcess.HasExited)
        {
            throw new InvalidOperationException("The process is already running.");
        }

        if(OperatingSystem.IsLinux())
            _wineProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "wine",
                    Arguments = $"\"./Files/cmios9.exe\" -q1 \"{_diskImagePath}\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };
        else
            _wineProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = $".\\Files\\cmios9.exe",
                    Arguments = $"-q1 \"{_diskImagePath}\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };


        _wineProcess.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                lock (_outputBuffer)
                {
                    _outputBuffer.AppendLine(args.Data);
                }
            }
        };

        _wineProcess.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                lock (_outputBuffer)
                {
                    _outputBuffer.AppendLine($"ERROR: {args.Data}");
                }
            }
        };

        _wineProcess.Start();
        _processInput = _wineProcess.StandardInput;

        try {
            _wineProcess.BeginOutputReadLine();
            _wineProcess.BeginErrorReadLine();

            await Task.Delay(1000);
            ReadOutput();
        } catch (InvalidOperationException ex) {
            _wineProcess.Kill();
            Logger.TryGet(LogEventLevel.Error, "CmiOsService")?.Log(this, "Error starting process: " + ex.Message + "\n" + ex.StackTrace);
        }
    }

    /// <summary>
    /// Send command to the cmios9 process
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task SendCommandAsync(string command)
    {
        if (_wineProcess == null || _wineProcess.HasExited)
        {
            throw new InvalidOperationException("The process is not running.");
        }

        if(_processInput is not null) {
            await _processInput.WriteLineAsync(command);
            await _processInput.FlushAsync();
        }
    }

    /// <summary>
    /// Read cmios9 process output
    /// </summary>
    /// <returns></returns>
    public string ReadOutput()
    {
        lock (_outputBuffer)
        {
            var output = _outputBuffer.ToString();
            _outputBuffer.Clear();
            return output;
        }
    }

    /// <summary>
    /// Perform and parse the disk image directory in cmios9
    /// </summary>
    /// <returns></returns>
    public async Task<List<String>> DiskDirectory() {
        await SendCommandAsync("dir");
        await Task.Delay(500);
        string output = ReadOutput();

        #if DEBUG
            Console.WriteLine(output);
        #endif

        try {
            MatchCollection matches;
            if(OperatingSystem.IsLinux())
                matches = Regex.Matches(output, @"fnr.*\n-+-\n([A-Za-z0-9\-\s.]*\n){0,100}-+-\n", RegexOptions.Multiline);
            else
                matches = Regex.Matches(output, @"fnr.*\r\n-+-\r\n([A-Za-z0-9\-\s.]*\r\n){0,100}-+-\r\n", RegexOptions.Multiline);
            if (matches.Count > 0) {
                string dirstring = matches.Last().Value;
                string[] dirlines;
                if(OperatingSystem.IsLinux())
                    dirlines = dirstring.Split("\n");
                else
                    dirlines = dirstring.Split("\r\n");
                return dirlines.Skip(2).Take(dirlines.Length - 4).Select(e => new string(e.Trim().TakeLast(11).ToArray()).Replace(" ", "")).ToList();
            }    
            return new();
        } catch (RegexParseException ex) {
            Logger.TryGet(LogEventLevel.Error, "CmiOsService")?.Log(this, "Error parsing regex: " + ex.Message + "\n" + ex.StackTrace);
            return new();
        }
    }

    /// <summary>
    /// Import sample to the disk image
    /// </summary>
    /// <param name="pathToSample"></param>
    /// <param name="sampleName"></param>
    /// <returns></returns>
    public async Task ImportSample(string pathToSample, string sampleName) {
        if(OperatingSystem.IsLinux()) {
            var transposedPathToSample = "Z:" + pathToSample.Replace("/", "\\");
            #if DEBUG
                Console.WriteLine(transposedPathToSample);
            #endif
            await SendCommandAsync($"wav2vc2 {transposedPathToSample} {sampleName}.VC");
        } else {
            await SendCommandAsync($"wav2vc2 {pathToSample} {sampleName}.VC");
        }
        await Task.Delay(500);
        #if DEBUG
            Console.WriteLine(ReadOutput());
        #endif
    }

    /// <summary>
    /// Export sample from the disk image
    /// </summary>
    /// <param name="sampleName"></param>
    /// <param name="pathToSave"></param>
    /// <returns></returns>
    public async Task ExportSample(string sampleName, string pathToSave) {
        if(OperatingSystem.IsLinux()) {
            var transposedPathToSave = "Z:" + pathToSave.Replace("/", "\\");
            await SendCommandAsync($"vc2wav {sampleName} {transposedPathToSave}");
        } else {
            await SendCommandAsync($"vc2wav {sampleName} {pathToSave}");
        }
        await Task.Delay(500);
        #if DEBUG
            Console.WriteLine(ReadOutput());
        #endif
    }

    public async Task ExportSampleAsVc(string sampleName, string pathToSave) {
        if(OperatingSystem.IsLinux()) {
            var transposedPathToSave = "Z:" + pathToSave.Replace("/", "\\");
            await SendCommandAsync($"export {sampleName}");
        } else {
            await SendCommandAsync($"export {sampleName}");
        }
        await Task.Delay(500);
        #if DEBUG
            Console.WriteLine(ReadOutput());
        #endif
        // file was exported to runtime directory, move it to desired location
        var exportedFileName = sampleName;
        if(exportedFileName.EndsWith(".VC") == false) {
            exportedFileName += ".VC";
        }
        var runtimePath = Path.Combine(Directory.GetCurrentDirectory(), exportedFileName);
        if(File.Exists(runtimePath)) {
            File.Move(runtimePath, pathToSave, true);
        }
    }

    /// <summary>
    /// Delete sample from the disk image
    /// </summary>
    /// <param name="sampleName"></param>
    /// <returns></returns>
    public async Task DeleteSample(string sampleName) {
        await SendCommandAsync($"rm {sampleName}");
        await Task.Delay(500);
        #if DEBUG
            Console.WriteLine(ReadOutput());
        #endif
    }

    /// <summary>
    /// Rename sample in the disk image
    /// </summary>
    /// <param name="sampleName"></param>
    /// <param name="newSampleName"></param>
    /// <returns></returns>
    public async Task RenameSample(string sampleName, string newSampleName) {
        await SendCommandAsync($"move {sampleName} {newSampleName}.VC");
        await Task.Delay(500);
        #if DEBUG
            Console.WriteLine(ReadOutput());
        #endif
    }

    /// <summary>
    /// Stop the cmios9 process
    /// </summary>
    public void StopProcess()
    {
        if (_wineProcess != null && !_wineProcess.HasExited)
        {
            _wineProcess.Kill();
            _wineProcess.WaitForExit();
        }

        _wineProcess?.Dispose();
        _wineProcess = null;
    }
}
