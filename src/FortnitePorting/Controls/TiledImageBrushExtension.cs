using System;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace FortnitePorting.Controls;

/**
 * ImageBrush that fills area with a tiled image, preserving the image's resolution and aspect ratio
 */
public class TiledImageBrushExtension : MarkupExtension
{
    public IImage Source { get; set; }
    public double Opacity { get; set; } = 1;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (Source == null)
            throw new InvalidOperationException("TiledImageBrushExtension: Source image must be set");
        
        var brush = new DrawingBrush
        {
            Opacity = Opacity,
            TileMode = TileMode.Tile,
            Stretch = Stretch.None,
            DestinationRect = new RelativeRect(0, 0, Source.Size.Width, Source.Size.Height, RelativeUnit.Absolute),
            Drawing = new ImageDrawing
            {
                ImageSource = Source,
                Rect = new Rect(0, 0, Source.Size.Width, Source.Size.Height)
            }
        };
        
        return brush;
    }
}