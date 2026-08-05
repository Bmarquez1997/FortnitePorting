using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Exporting.Models;
using FortnitePorting.ViewModels;

namespace FortnitePorting.ViewModels.Settings;

public partial class FolderSettingsViewModel : BaseExportSettings
{
    [ObservableProperty] private bool _openFoldersOnExport;

    public override ExportSettings ToExportSettings()
    {
        var settings = base.ToExportSettings();
        settings.OpenFoldersOnExport = OpenFoldersOnExport;
        return settings;
    }
}
