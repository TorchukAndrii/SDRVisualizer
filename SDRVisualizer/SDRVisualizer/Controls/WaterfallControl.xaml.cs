using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SDRVisualizer.Helpers;

namespace SDRVisualizer.Controls;

public partial class WaterfallControl : UserControl
{
    private readonly BitmapBufferPool _bufferPool = new();
    private readonly Queue<int[]> _data = new();
    private readonly GradientGenerator _gradient = new();
    private readonly RenderScheduler _renderScheduler;
    private WriteableBitmap _waterfallBitmap;
    private byte[] _waterfallPixels;

    public WaterfallControl()
    {
        InitializeComponent();
        _renderScheduler = new RenderScheduler(Dispatcher); // Initialize in constructor

        Loaded += (_, _) => InitializeBitmap();
        SizeChanged += (_, _) => InitializeBitmap();
    }

    public void AddSpectrumSnapshot(int[] row)
    {
        if (_data.Count >= VisualizerConstants.MaxHistory)
            _data.Dequeue();

        _data.Enqueue(row);
        _renderScheduler.EnqueueRender(RenderWaterfall);
    }

    private void InitializeBitmap()
    {
        var width = (int)Math.Max(ActualWidth, 1);
        var height = VisualizerConstants.MaxHistory;

        _waterfallBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
        WaterfallImage.Source = _waterfallBitmap;
        _waterfallPixels = _bufferPool.GetWaterfallBuffer(width, height);
        RenderWaterfall();
    }

    private void RenderWaterfall()
    {
        if (_data.Count == 0 || _waterfallBitmap == null)
            return;

        var width = _waterfallBitmap.PixelWidth;
        var height = _waterfallBitmap.PixelHeight;
        var stride = width * 4;

        _waterfallPixels = _bufferPool.GetWaterfallBuffer(width, height);

        var scaleY = height / (double)VisualizerConstants.MaxHistory;
        var scaleX = width / (double)VisualizerConstants.FFTSize;

        var rowIndex = 0;
        foreach (var row in _data.Reverse())
        {
            var yStart = (int)(rowIndex * scaleY);
            var yEnd = Math.Min((int)((rowIndex + 1) * scaleY), height);

            for (var y = yStart; y < yEnd; y++)
            for (var x = 0; x < width; x++)
            {
                var dataX = Math.Min((int)(x / scaleX), row.Length - 1);

                var normalized = (VisualizerConstants.MaxPowerDbm - row[dataX]) /
                                 (VisualizerConstants.MaxPowerDbm - VisualizerConstants.MinPowerDbm);

                var colorIndex = (int)((1 - normalized) * (VisualizerConstants.GradientResolution - 1));
                colorIndex = Math.Clamp(colorIndex, 0, VisualizerConstants.GradientResolution - 1);

                var argb = _gradient.GetGradientPixels()[colorIndex];
                var b = (byte)(argb & 0xFF);
                var g = (byte)((argb >> 8) & 0xFF);
                var r = (byte)((argb >> 16) & 0xFF);

                var index = y * stride + x * 4;
                _waterfallPixels[index + 0] = b;
                _waterfallPixels[index + 1] = g;
                _waterfallPixels[index + 2] = r;
                _waterfallPixels[index + 3] = 255;
            }

            rowIndex++;
            if (rowIndex >= VisualizerConstants.MaxHistory) break;
        }

        _waterfallBitmap.WritePixels(new Int32Rect(0, 0, width, height), _waterfallPixels, stride, 0);
    }
}