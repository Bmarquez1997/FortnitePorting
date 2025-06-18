using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvaloniaEdit.Folding;
using Avalonia.VisualTree;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.ViewModels;
using FortnitePorting.WindowModels;
using PropertiesContainer = FortnitePorting.Models.Viewers.PropertiesContainer;

namespace FortnitePorting.Windows;

public partial class PropertiesPreviewWindow : WindowBase<PropertiesPreviewWindowModel>
{
    public static PropertiesPreviewWindow? Instance;
    
    public PropertiesPreviewWindow()
    {
        InitializeComponent();
        DataContext = WindowModel;
        Owner = App.Lifetime.MainWindow;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        Editor.TextArea.TextView.BackgroundRenderers.Add(new IndentGuideLinesRenderer(Editor));
        
        var foldingManager = FoldingManager.Install(Editor.TextArea);
        var foldingStrategy = new JsonFoldingStrategy();
        foldingStrategy.UpdateFoldings(foldingManager, Editor.Document);
        
        SetMarginColors(foldingManager);
    }

    //TODO: How does this get applied?
    private void SetMarginColors(FoldingManager manager)
    {
        var brush = new SolidColorBrush(AppSettings.Theme.BackgroundColor);
        var hover = new SolidColorBrush(AppSettings.Theme.AccentColor);
        var margin = new FoldingMargin
        {
            FoldingManager = manager,
            FoldingMarkerBrush = brush,
            FoldingMarkerBackgroundBrush = brush,
            SelectedFoldingMarkerBrush = hover,
            SelectedFoldingMarkerBackgroundBrush = hover
        };
    }

    public static void Preview(string name, string json)
    {
        if (Instance is null)
        {
            Instance = new PropertiesPreviewWindow();
            Instance.Show();
            Instance.BringToTop();
        }

        if (Instance.WindowModel.Assets.FirstOrDefault(asset => asset.AssetName.Equals(name)) is { } existing)
        {
            Instance.WindowModel.SelectedAsset = existing;
            return;
        }

        var container = new PropertiesContainer
        {
            AssetName = name,
            PropertiesData = json
        };
        
        Instance.WindowModel.Assets.Add(container);
        Instance.WindowModel.SelectedAsset = container;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        Instance = null;
    }

    private void OnTabClosed(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Item is not PropertiesContainer properties) return;

        WindowModel.Assets.Remove(properties);

        if (WindowModel.Assets.Count == 0)
        {
            Close();
        }
    }
}