using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.Models.Viewers;
using FortnitePorting.Services;

namespace FortnitePorting.WindowModels;

[Transient]
public partial class CurvePreviewWindowModel(SettingsService settings) : WindowModelBase
{
    [ObservableProperty] private SettingsService _settings = settings;

    [ObservableProperty] private ObservableCollection<CurveContainer> _curves = [];
    [ObservableProperty] private CurveContainer _selectedCurve;

}