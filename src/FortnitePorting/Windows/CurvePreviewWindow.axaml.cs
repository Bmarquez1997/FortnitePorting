using System;
using System.Linq;
using Avalonia.Controls;
using CUE4Parse.UE4.Objects.Engine.Curves;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Framework;
using FortnitePorting.Models.Viewers;
using FortnitePorting.WindowModels;

namespace FortnitePorting.Windows;

public partial class CurvePreviewWindow : WindowBase<CurvePreviewWindowModel>
{
    public static CurvePreviewWindow? Instance;
    
    public CurvePreviewWindow()
    {
        InitializeComponent();
        DataContext = WindowModel;
        Owner = App.Lifetime.MainWindow;
    }

    public static void Preview(string name, UCurveLinearColor curve)
    {
        if (Instance is null)
        {
            Instance = new CurvePreviewWindow();
            Instance.Show();
        }
        
        Instance.BringToTop();

        if (Instance.WindowModel.Curves.FirstOrDefault(curve => curve.CurveName.Equals(name)) is { } existing)
        {
            Instance.WindowModel.SelectedCurve = existing;
            return;
        }

        var container = new CurveContainer
        {
            CurveName = name,
            Curve = curve
        };
        
        container.Update();
        
        Instance.WindowModel.Curves.Add(container);
        Instance.WindowModel.SelectedCurve = container;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        Instance = null;
    }
    
    private void OnTabClosed(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Item is not CurveContainer curve) return;

        WindowModel.Curves.Remove(curve);

        if (WindowModel.Curves.Count == 0)
        {
            Close();
        }
    }
}