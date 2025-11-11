using System;
using System.ComponentModel;
using System.Linq;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvaloniaEdit.Folding;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models;
using FortnitePorting.WindowModels;
using PropertiesContainer = FortnitePorting.Models.Viewers.PropertiesContainer;

namespace FortnitePorting.Windows;

public partial class PropertiesPreviewWindow : WindowBase<PropertiesPreviewWindowModel>
{
    public static PropertiesPreviewWindow? Instance;

    private FoldingManager? _foldingManager;
    private JsonFoldingStrategy _foldingStrategy = new();
    private FoldingMargin? _foldingMargin;

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
        EnsureFoldingManager();
        
        if (WindowModel is INotifyPropertyChanged inpc)
        {
            inpc.PropertyChanged += OnWindowModelPropertyChanged;
        }
    }
    
    private void EnsureFoldingManager()
    {
        if (_foldingManager is not null)
        {
            FoldingManager.Uninstall(_foldingManager);
            _foldingManager = null;
        }

        _foldingManager = FoldingManager.Install(Editor.TextArea);
        _foldingStrategy.UpdateFoldings(_foldingManager, Editor.Document);
        ApplyMarginForManager(_foldingManager);
    }

    private void ApplyMarginForManager(FoldingManager manager)
    {
        var brush = new SolidColorBrush(AppSettings.Theme.WindowBackgroundColor);
        var hover = new SolidColorBrush(AppSettings.Theme.StyleAccentColor);
        
        if (_foldingMargin is not null)
        {
            if (Editor.TextArea.LeftMargins.Contains(_foldingMargin))
                Editor.TextArea.LeftMargins.Remove(_foldingMargin);
            _foldingMargin = null;
        }

        _foldingMargin = new FoldingMargin
        {
            FoldingManager = manager,
            FoldingMarkerBrush = brush,
            FoldingMarkerBackgroundBrush = brush,
            SelectedFoldingMarkerBrush = hover,
            SelectedFoldingMarkerBackgroundBrush = hover
        };
        
        Editor.TextArea.LeftMargins.Add(_foldingMargin);
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
        
        if (_foldingManager is not null)
        {
            FoldingManager.Uninstall(_foldingManager);
            _foldingManager = null;
        }

        if (WindowModel is INotifyPropertyChanged inpc)
        {
            inpc.PropertyChanged -= OnWindowModelPropertyChanged;
        }

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

    private void OnWindowModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WindowModel.SelectedAsset))
        {
            EnsureFoldingManager();
        }
    }
}