using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine.Curves;

namespace FortnitePorting.Models.Viewers;

public partial class CurveContainer : ObservableObject
{
    [ObservableProperty] private UCurveLinearColor _curve;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CurveDisplayName))] private string _curveName = string.Empty;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CurveDisplayName))] private int _curveIndex = 0;
    
    public string CurveDisplayName => $"{CurveIndex} - {CurveName}";
    
    
    [ObservableProperty] private List<CurveFrame> _originalCurveFrames = [];
    [ObservableProperty] private LinearGradientBrush _curveBrush;

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

        ExtractFrames();
        CreateBrush();
    }

    private void ExtractFrames()
    {
        HashSet<float> frameTimes = [0, 1];
        foreach (var curveDef in Curve.FloatCurves)
        {
            frameTimes.AddRange(curveDef.Keys.Select(key => key.Time).ToList());
        }

        foreach (var time in frameTimes.ToList().Order())
        {
            OriginalCurveFrames.Add(new CurveFrame(time, Curve.GetLinearColorValue(time)));
        }
    }

    private void CreateBrush()
    {
        CurveBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
            GradientStops = CalculateFrames()
        };
    }
    
    public void UpdateCurve()
    {
        CurveBrush.GradientStops = CalculateFrames();
    }

    private GradientStops CalculateFrames()
    {
        var stops = new GradientStops();

        foreach (var frame in OriginalCurveFrames)
        {
            stops.Add(new GradientStop(frame.GetColor(ShowRedChannel, ShowGreenChannel, ShowBlueChannel, ShowAlphaChannel), frame.Time));
        }
        
        return stops;
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
                UpdateCurve();
                break;
            }
        }
    }
}

public class CurveFrame
{
    public float Time;
    public FLinearColor ColorValue;

    public CurveFrame(float time, FLinearColor color)
    {
        Time = time;
        ColorValue = color;
    }

    public Color GetColor(bool red, bool green, bool blue, bool alpha)
    {
        var newColor = new FLinearColor()
        {
            R = red ? ColorValue.R : 0,
            G = green ? ColorValue.G : 0,
            B = blue ? ColorValue.B : 0,
            A = alpha ? ColorValue.A : 0,
        };
        return Color.Parse($"#{newColor.ToFColor(false).Hex}");
    }
}