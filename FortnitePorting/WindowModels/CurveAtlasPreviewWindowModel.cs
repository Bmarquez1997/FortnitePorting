using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.Models.Viewers;
using FortnitePorting.Services;

namespace FortnitePorting.WindowModels;

[Transient]
public partial class CurveAtlasPreviewWindowModel(SettingsService settings) : WindowModelBase
{
    [ObservableProperty] private SettingsService _settings = settings;

    [ObservableProperty] private ObservableCollection<CurveAtlasContainer> _curveAtlases = [];
    [ObservableProperty] private CurveAtlasContainer _selectedAtlas;

}