using System.Drawing;

namespace SDRVisualizer.Helpers;

public class GradientGenerator
{
    private readonly int[] _gradientPixels;

    public GradientGenerator()
    {
        _gradientPixels = GenerateGradient();
    }

    public int[] GetGradientPixels()
    {
        return _gradientPixels;
    }

    private int[] GenerateGradient()
    {
        const int gradientSize = VisualizerConstants.GradientResolution;
        var gradient = new int[gradientSize];

        // Define the colors for the gradient stops
        var colors = new[]
        {
            Color.FromArgb(255, 0, 0, 255), // 0% - Dark Blue (#0000ff)
            Color.FromArgb(255, 0, 255, 255), // 25% - Cyan (#00ffff)
            Color.FromArgb(255, 0, 255, 0), // 50% - Green (#00ff00)
            Color.FromArgb(255, 255, 255, 0), // 75% - Yellow (#ffff00)
            Color.FromArgb(255, 255, 0, 0) // 100% - Red (#ff0000)
        };

        // Calculate the gradient stops (from 0 to 1)
        for (var i = 0; i < gradientSize; i++)
        {
            var t = i / (double)(gradientSize - 1); // Normalize the index to [0, 1]

            // Determine the gradient stop based on the value of t
            var color = GetInterpolatedColor(t, colors);

            // Store the color in ARGB format
            gradient[i] = (color.R << 16) | (color.G << 8) | color.B; // ARGB format
        }

        return gradient;
    }

    private Color GetInterpolatedColor(double t, Color[] colors)
    {
        // Find which two gradient stops the value t falls between
        var lowerIndex = (int)(t * (colors.Length - 1));
        var upperIndex = Math.Min(lowerIndex + 1, colors.Length - 1);

        var lowerColor = colors[lowerIndex];
        var upperColor = colors[upperIndex];

        // Interpolate between the two colors
        var factor = (t - lowerIndex / (double)(colors.Length - 1)) / (1.0 / (colors.Length - 1));
        var r = (byte)(lowerColor.R + factor * (upperColor.R - lowerColor.R));
        var g = (byte)(lowerColor.G + factor * (upperColor.G - lowerColor.G));
        var b = (byte)(lowerColor.B + factor * (upperColor.B - lowerColor.B));

        return Color.FromArgb(255, r, g, b); // Return the interpolated color
    }
}