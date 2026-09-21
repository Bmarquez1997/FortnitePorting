using System.Collections.Generic;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Assets.Exports.Texture;

namespace FortnitePorting.Models.Viewers;

public partial class CurveAtlasContainer : ObservableObject
{
    [ObservableProperty] private UCurveLinearColorAtlas _atlas;
    [ObservableProperty] private string _atlasName = string.Empty;
    
    [ObservableProperty] private List<CurveContainer> _curves = [];
    [ObservableProperty] private CurveContainer _selectedCurve;

    [ObservableProperty] private bool _showRedChannel = true;
    [ObservableProperty] private bool _showGreenChannel = true;
    [ObservableProperty] private bool _showBlueChannel = true;
    [ObservableProperty] private bool _showAlphaChannel = true;
    
    public void Update()
    {
        ShowRedChannel = true;
        ShowGreenChannel = true;
        ShowBlueChannel = true;
        ShowAlphaChannel = true;

        AddCurveContainers();
    }

    private void AddCurveContainers()
    {
        var index = 0;
        foreach (var curve in Atlas.GradientCurves)
        {
            var container = new CurveContainer
            {
                CurveName = curve.Name,
                CurveIndex = index++,
                Curve = curve
            };
            container.Update();
            Curves.Add(container);
        }
    }

    private void UpdateCurves()
    {
        foreach (var curve in Curves)
        {
            curve.ShowRedChannel = ShowRedChannel;
            curve.ShowGreenChannel = ShowGreenChannel;
            curve.ShowBlueChannel = ShowBlueChannel;
            curve.ShowAlphaChannel = ShowAlphaChannel;
            curve.UpdateCurve();
        }
    }
    
    
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        
        switch (e.PropertyName)
        {
            case nameof(ShowRedChannel):
            case nameof(ShowGreenChannel):
            case nameof(ShowBlueChannel):
            case nameof(ShowAlphaChannel):
            {
                UpdateCurves();
                break;
            }
        }
    }
}