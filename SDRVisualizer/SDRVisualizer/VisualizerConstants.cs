namespace SDRVisualizer;

public static class VisualizerConstants
{
    public const int FFTSize = 1024; // Size of the FFT (1024 points)
    public const int MaxHistory = 200; // Max history of the spectrum data
    public const double MinPowerDbm = -120.0;
    public const double MaxPowerDbm = -20.0;
    public const double FrequencyStart = 90.0;
    public const double FrequencyEnd = 110.0;

    //UI
    public const double GridPadding = 60.0;
    public const int GradientResolution = 256; // Gradient resolution (number of colors)
}