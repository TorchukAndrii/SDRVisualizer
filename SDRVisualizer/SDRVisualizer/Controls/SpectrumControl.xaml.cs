using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SDRVisualizer.Helpers;

namespace SDRVisualizer.Controls;

public partial class SpectrumControl : UserControl
{
    private readonly BitmapBufferPool _bufferPool = new();
    private RenderScheduler _renderScheduler;
    private WriteableBitmap _backgroundBitmap;
    private WriteableBitmap _spectrumBitmap;
    private List<int> _spectrumData = new();
    private byte[] _spectrumLinePixels;

    public SpectrumControl()
    {
        InitializeComponent();
        _renderScheduler = new RenderScheduler(Dispatcher);

        Loaded += (_, _) => InitializeBitmaps();
        SizeChanged += (_, _) => InitializeBitmaps();
    }

    public void UpdateSpectrumData(List<int> data)
    {
        _spectrumData = data;
        _renderScheduler.EnqueueRender(RenderSpectrum);
    }

    private void InitializeBitmaps()
    {
        var width = (int)Math.Max(ActualWidth, 1);
        var height = (int)Math.Max(ActualHeight, 1);

        _spectrumBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
        _backgroundBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
        _spectrumLinePixels = _bufferPool.GetSpectrumBuffer(width, height);

        SpectrumImage.Source = _spectrumBitmap;
        DrawBackground(_backgroundBitmap);
        RenderSpectrum();
    }

    private void DrawBackground(WriteableBitmap bitmap)
{
    DrawingVisual visual = new();
    using (var dc = visual.RenderOpen())
    {
        var width = bitmap.PixelWidth;
        var height = bitmap.PixelHeight;

        Pen gridPen = new(Brushes.LightGray, 0.5);
        Typeface typeface = new("Arial");
        Brush textBrush = Brushes.White;
        double fontSize = 10;
        double gridPadding = VisualizerConstants.GridPadding;
        var gridWidth = width - 2 * gridPadding;
        var gridHeight = height - 2 * gridPadding;

        // Draw vertical grid lines and frequency labels
        for (var i = 0; i <= 20; i++)
        {
            var x = gridPadding + i * (gridWidth / 20.0);
            dc.DrawLine(gridPen, new Point(x, gridPadding), new Point(x, height - gridPadding));
            
            // Calculate the frequency for each vertical line
            var frequency = VisualizerConstants.FrequencyStart + i * (VisualizerConstants.FrequencyEnd - VisualizerConstants.FrequencyStart) / 20.0;
            var freqText = new FormattedText($"{frequency:F1} MHz", CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, typeface, fontSize, textBrush, 1.0);
            dc.DrawText(freqText, new Point(x - freqText.Width / 2, height - gridPadding + 5));
        }

        // Draw horizontal grid lines and power labels
        for (var i = 0; i <= 10; i++)
        {
            var y = gridPadding + (10 - i) * (gridHeight / 10.0);
            dc.DrawLine(gridPen, new Point(gridPadding, y), new Point(width - gridPadding, y));
            
            // Calculate the power for each horizontal line
            var power = VisualizerConstants.MinPowerDbm + i * (VisualizerConstants.MaxPowerDbm - VisualizerConstants.MinPowerDbm) / 10.0;
            var powerText = new FormattedText($"{power:F1} dBm", CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, typeface, fontSize, textBrush, 1.0);
            dc.DrawText(powerText, new Point(gridPadding - powerText.Width - 5, y - powerText.Height / 2));
        }
    }

    // Render the visual to the background bitmap
    RenderTargetBitmap rtb = new(bitmap.PixelWidth, bitmap.PixelHeight, 96, 96, PixelFormats.Pbgra32);
    rtb.Render(visual);
    bitmap.Lock();
    rtb.CopyPixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight), bitmap.BackBuffer,
        bitmap.BackBufferStride * bitmap.PixelHeight, bitmap.BackBufferStride);
    bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
    bitmap.Unlock();
}


    private void RenderSpectrum()
    {
        var width = _spectrumBitmap.PixelWidth;
        var height = _spectrumBitmap.PixelHeight;
        _spectrumLinePixels = _bufferPool.GetSpectrumBuffer(width, height);
        DrawSpectrumLine(_spectrumLinePixels, width, height);

        _spectrumBitmap.Lock();
        _backgroundBitmap.CopyPixels(new Int32Rect(0, 0, width, height), _spectrumBitmap.BackBuffer,
            _spectrumBitmap.BackBufferStride * height, _spectrumBitmap.BackBufferStride);

        unsafe
        {
            var buffer = (byte*)_spectrumBitmap.BackBuffer.ToPointer();
            for (var i = 0; i < width * height * 4; i += 4)
                if (_spectrumLinePixels[i + 3] > 0)
                {
                    buffer[i + 0] = _spectrumLinePixels[i + 0];
                    buffer[i + 1] = _spectrumLinePixels[i + 1];
                    buffer[i + 2] = _spectrumLinePixels[i + 2];
                    buffer[i + 3] = 255;
                }
        }

        _spectrumBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
        _spectrumBitmap.Unlock();
    }

    private void DrawSpectrumLine(byte[] pixels, int width, int height)
    {
        var gridPadding = VisualizerConstants.GridPadding;  // Padding for the grid
        var gridWidth = width - 2 * gridPadding;
        var gridHeight = height - 2 * gridPadding;
        var step = gridWidth / VisualizerConstants.FFTSize;

        // Loop through the spectrum data and draw the spectrum line
        for (var i = 1; i < _spectrumData.Count; i++)
        {
            // Calculate the X and Y coordinates
            var x1 = gridPadding + (i - 1) * step;
            var y1 = gridPadding + (1 - (_spectrumData[i - 1] - VisualizerConstants.MinPowerDbm) /
                (VisualizerConstants.MaxPowerDbm - VisualizerConstants.MinPowerDbm)) * gridHeight;

            var x2 = gridPadding + i * step;
            var y2 = gridPadding + (1 - (_spectrumData[i] - VisualizerConstants.MinPowerDbm) /
                (VisualizerConstants.MaxPowerDbm - VisualizerConstants.MinPowerDbm)) * gridHeight;

            // Draw the line with the calculated positions
            DrawingHelpers.DrawLine(pixels, width, height, (int)x1, (int)y1, (int)x2, (int)y2, Colors.Yellow);
        }
    }

}