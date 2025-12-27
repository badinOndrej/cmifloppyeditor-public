using System.Threading.Tasks;

namespace cmifloppy_linux.Services;

/// <summary>
/// Interface for dialog services.
/// </summary>
public interface IDialogService
{
    Task<(string? diskName, string? diskOwner)> ShowNewImageDialog();
    Task<string?> ShowOpenImageDialog();
    Task<string?> ShowOpenImdImageDialog();
    Task<(string? path, bool overwrite)> ShowSaveImageDialog();
    Task ShowErrorDialog();
    Task ShowInvalidWavDialog();
    Task<(string? path, string? sampleName)> ShowOpenSampleDialog();
    Task<string?> ShowSaveSampleDialog();
    Task<string?> ShowSampleNameDialog();
    Task<string?> ShowSaveImdDialog();
    Task<string?> ShowSaveMfiDialog();
    Task<string?> ShowSaveMfmDialog();
    Task ShowInvalidSampleDialog();
}