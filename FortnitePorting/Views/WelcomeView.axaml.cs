using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class WelcomeView : ViewBase<WelcomeViewModel>
{
    public WelcomeView()
    {
        InitializeComponent();
    }
}