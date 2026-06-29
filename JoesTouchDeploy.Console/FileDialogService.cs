using System.Windows.Forms;

namespace JoesTouchDeploy.Console;

public class FileDialogService
{
    private string? _lastFolder;

    public string? SelectVtzProjectFile()
    {
        string? selectedFile = null;
        Exception? dialogException = null;

        var dialogThread = new Thread(() =>
        {
            try
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

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    selectedFile = dialog.FileName;
                    _lastFolder = Path.GetDirectoryName(dialog.FileName);
                }
            }
            catch (Exception exception)
            {
                dialogException = exception;
            }
        });

        dialogThread.SetApartmentState(ApartmentState.STA);
        dialogThread.Start();
        dialogThread.Join();

        if (dialogException != null)
        {
            throw dialogException;
        }

        return selectedFile;
    }
}
