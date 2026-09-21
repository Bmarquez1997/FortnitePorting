using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Framework;
using FortnitePorting.Models.Viewers;
using FortnitePorting.WindowModels;

namespace FortnitePorting.Windows;

public partial class CurveAtlasPreviewWindow : WindowBase<CurveAtlasPreviewWindowModel>
{
    public static CurveAtlasPreviewWindow? Instance;
    
    public CurveAtlasPreviewWindow()
    {
        InitializeComponent();
        DataContext = WindowModel;
        Owner = App.Lifetime.MainWindow;
    }

    public static void Preview(string name, UCurveLinearColorAtlas atlas)
    {
        if (Instance is null)
        {
            Instance = new CurveAtlasPreviewWindow();
            Instance.Show();
        }
        
        Instance.BringToTop();

        if (Instance.WindowModel.CurveAtlases.FirstOrDefault(curve => curve.AtlasName.Equals(name)) is { } existing)
        {
            Instance.WindowModel.SelectedAtlas = existing;
            return;
        }

        var container = new CurveAtlasContainer
        {
            AtlasName = name,
            Atlas = atlas
        };
        
        container.Update();
        
        Instance.WindowModel.CurveAtlases.Add(container);
        Instance.WindowModel.SelectedAtlas = container;
    }
    
    private void OnCurvePressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.ClickCount != 2) return;
        if (sender is not Control control) return;
        if (control.DataContext is not CurveContainer curve) return;

        CurvePreviewWindow.Preview(curve.CurveName, curve.Curve);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        Instance = null;
    }
    
    private void OnTabClosed(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Item is not CurveAtlasContainer atlas) return;

        WindowModel.CurveAtlases.Remove(atlas);

        if (WindowModel.CurveAtlases.Count == 0)
        {
            Close();
        }
    }
}