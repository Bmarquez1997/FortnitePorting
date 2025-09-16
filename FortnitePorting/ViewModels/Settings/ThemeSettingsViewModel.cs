using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.Styling;
using FortnitePorting.Framework;
using FortnitePorting.Shared.Extensions;
using Newtonsoft.Json;

namespace FortnitePorting.ViewModels.Settings;

public partial class ThemeSettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TransparencyHints))]
    private bool _useMica = false;
    public ObservableCollection<WindowTransparencyLevel> TransparencyHints => UseMica ? [WindowTransparencyLevel.Mica, WindowTransparencyLevel.AcrylicBlur] : [WindowTransparencyLevel.AcrylicBlur];
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(BackgroundBrush))] private Color _windowBackgroundColor = Color.Parse("#1C1C26");
    public SolidColorBrush BackgroundBrush => new(new Color(0xDB, WindowBackgroundColor.R, WindowBackgroundColor.G, WindowBackgroundColor.B));
    
    [ObservableProperty] private Color _styleAccentColor = Color.Parse("#8900FF");
    
    public bool IsWindows11 => Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Build >= 22000;

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
       
        switch (e.PropertyName)
        {
            case nameof(StyleAccentColor):
            {
                var faTheme = Avalonia.Application.Current?.Styles.OfType<FluentAvaloniaTheme>().FirstOrDefault();
                faTheme.CustomAccentColor = StyleAccentColor;
                break;
            }
        }
    }
}