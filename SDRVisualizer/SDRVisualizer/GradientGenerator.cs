using System.Windows.Media;

namespace SDRVisualizer;

public class GradientGenerator
{
    public int[] GetGradientPixels()
    {
        int size = 100;
        int[] _gradientPixels = new int[size];
        
        GradientStopCollection gradientStops = new GradientStopCollection
        {
            new GradientStop((Color)ColorConverter.ConvertFromString("#0000FF"), 0.0), 
            new GradientStop((Color)ColorConverter.ConvertFromString("#00FFFF"), 0.25),
            new GradientStop((Color)ColorConverter.ConvertFromString("#00FF00"), 0.5), 
            new GradientStop((Color)ColorConverter.ConvertFromString("#FFFF00"), 0.75),
            new GradientStop((Color)ColorConverter.ConvertFromString("#FF0000"), 1.0)  
        };
        
        for (int i = 0; i < size; i++)
        {
            float position = i / (float)(size - 1);
            _gradientPixels[i] = GetInterpolatedColor(gradientStops, position);
        }

        return _gradientPixels;
    }

    private int GetInterpolatedColor(GradientStopCollection stops, float position)
    {
        GradientStop start = stops[0], end = stops[^1];
        
        if (position <= 0) return ConvertColorToArgb(start.Color);
        if (position >= 1) return ConvertColorToArgb(end.Color);
        
        foreach (var stop in stops)
        {
            if (stop.Offset <= position) start = stop;
            if (stop.Offset >= position)
            {
                end = stop;
                break;
            }
        }
        
        float factor = (position - (float)start.Offset) / ((float)end.Offset - (float)start.Offset);
        byte r = (byte)(start.Color.R + (end.Color.R - start.Color.R) * factor);
        byte g = (byte)(start.Color.G + (end.Color.G - start.Color.G) * factor);
        byte b = (byte)(start.Color.B + (end.Color.B - start.Color.B) * factor);

        return (255 << 24) | (r << 16) | (g << 8) | b;
    }

    private int ConvertColorToArgb(Color color)
    {
        return (255 << 24) | (color.R << 16) | (color.G << 8) | color.B;
    }
}