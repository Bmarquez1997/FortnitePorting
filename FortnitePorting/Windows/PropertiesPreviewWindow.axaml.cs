using Avalonia.Interactivity;
using Avalonia.Media;
using AvaloniaEdit.Folding;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.Models;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.ViewModels;
using FortnitePorting.WindowModels;

namespace FortnitePorting.Windows;

public partial class PropertiesPreviewWindow : WindowBase<PropertiesPreviewWindowModel>
{
    
    public PropertiesPreviewWindow(string name, string json)
    {
        InitializeComponent();
        DataContext = WindowModel;
        Owner = App.Lifetime.MainWindow;

        WindowModel.AssetName = name;
        WindowModel.PropertiesJson = json;
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
        var brush = new SolidColorBrush(AppSettings.Current.Theme.BackgroundColor);
        var hover = new SolidColorBrush(AppSettings.Current.Theme.AccentColor);
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
        var window = new PropertiesPreviewWindow(name, json);
        window.Show();
        window.BringToTop();
    }
}