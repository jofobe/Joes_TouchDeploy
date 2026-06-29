namespace JoesTouchDeploy.App;

public class FileDialogService
{
    private string? _lastFolder;

    public string? SelectVtzProjectFile(IWin32Window owner)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select VTZ Project File",
            Filter = "VTZ Project Files (*.vtz)|*.vtz|All Files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
            RestoreDirectory = false
        };

        if (!string.IsNullOrWhiteSpace(_lastFolder) && Directory.Exists(_lastFolder))
        {
            dialog.InitialDirectory = _lastFolder;
        }

        if (dialog.ShowDialog(owner) != DialogResult.OK)
        {
            return null;
        }

        _lastFolder = Path.GetDirectoryName(dialog.FileName);

        return dialog.FileName;
    }
}
