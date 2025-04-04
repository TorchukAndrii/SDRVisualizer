using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SDRVisualizer;

public partial class MainWindow : Window
{
    private readonly int maxHistory = 200;
    private readonly List<int> spectrumData = new();
    private readonly Queue<int[]> waterfallData = new(); // Store last 200 frames
    private readonly FFTDataGenerator _dataGenerator;
    private CancellationTokenSource cancellationTokenSource;
    private bool isRunning;
    private WriteableBitmap spectrumBitmap;
    private WriteableBitmap waterfallBitmap;

    public MainWindow()
    {
        InitializeComponent();
        _dataGenerator = new FFTDataGenerator();
        SizeChanged += OnSizeChanged;
        Loaded += (s, e) => InitializeBitmaps();
    }

    private void InitializeBitmaps()
    {
        var width = (int)Math.Max(SpectrumImage.ActualWidth, 1);
        var height = (int)Math.Max(SpectrumImage.ActualHeight, 1);
        spectrumBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
        SpectrumImage.Source = spectrumBitmap;

        waterfallBitmap = new WriteableBitmap(width, maxHistory, 96, 96, PixelFormats.Pbgra32, null);
        WaterfallImage.Source = waterfallBitmap;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        InitializeBitmaps();
        RenderSpectrum();
        RenderWaterfall();
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        if (!isRunning)
        {
            isRunning = true;
            cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => GenerateData(cancellationTokenSource.Token));
        }
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        isRunning = false;
        cancellationTokenSource?.Cancel();
    }

    private async Task GenerateData(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            spectrumData.Clear();
            spectrumData.AddRange(_dataGenerator.GetRow());
            await Dispatcher.InvokeAsync(() =>
            {
                RenderSpectrum();
                UpdateWaterfall();
                RenderWaterfall();
            });
            await Task.Delay(50, token);
        }
    }

    private void RenderSpectrum()
    {
        var width = (int)SpectrumImage.ActualWidth;
        var height = (int)SpectrumImage.ActualHeight;
        if (width == 0 || height == 0) return;

        spectrumBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
        SpectrumImage.Source = spectrumBitmap;
        spectrumBitmap.Lock();

        Int32Rect rect = new(0, 0, width, height);
        var stride = width * 4;
        var pixels = new byte[stride * height];

        double gridPadding = 50;
        var gridWidth = width - 2 * gridPadding;
        var gridHeight = height - 2 * gridPadding;

        DrawingVisual visual = new();
        using (var dc = visual.RenderOpen())
        {
            Pen gridPen = new(Brushes.LightGray, 0.5);
            Typeface typeface = new("Arial");
            Brush textBrush = Brushes.White;
            double fontSize = 10;

            for (var i = 0; i <= 20; i++)
            {
                var x = gridPadding + i * (gridWidth / 20.0);
                dc.DrawLine(gridPen, new Point(x, gridPadding), new Point(x, height - gridPadding));
                FormattedText freqText = new(90 + i + " MHz", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    typeface, fontSize, textBrush, 1.0);
                dc.DrawText(freqText, new Point(x - freqText.Width / 2, height - gridPadding + 5));
            }

            for (var i = 0; i <= 10; i++)
            {
                var y = gridPadding + (10 - i) * (gridHeight / 10.0);
                dc.DrawLine(gridPen, new Point(gridPadding, y), new Point(width - gridPadding, y));
                FormattedText powerText = new(-120 + i * 10 + " dBm", CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight, typeface, fontSize, textBrush, 1.0);
                dc.DrawText(powerText, new Point(gridPadding - powerText.Width - 5, y - powerText.Height / 2));
            }

            Pen spectrumPen = new(Brushes.Yellow, 1);
            var step = gridWidth / 1024.0;
            for (var i = 1; i < spectrumData.Count; i++)
            {
                var x1 = gridPadding + (i - 1) * step;
                var y1 = gridPadding + (spectrumData[i - 1] + 120) * gridHeight / 100;
                var x2 = gridPadding + i * step;
                var y2 = gridPadding + (spectrumData[i] + 120) * gridHeight / 100;
                dc.DrawLine(spectrumPen, new Point(x1, y1), new Point(x2, y2));
            }
        }

        RenderTargetBitmap targetBitmap = new(width, height, 96, 96, PixelFormats.Pbgra32);
        targetBitmap.Render(visual);
        targetBitmap.CopyPixels(rect, pixels, stride, 0);
        spectrumBitmap.WritePixels(rect, pixels, stride, 0);
        spectrumBitmap.Unlock();
    }

    private void UpdateWaterfall()
    {
        if (waterfallData.Count >= maxHistory)
            waterfallData.Dequeue();

        waterfallData.Enqueue(spectrumData.ToArray());
    }

    private void RenderWaterfall()
    {
        EnsureWaterfallBitmap();
        if (waterfallBitmap == null || waterfallData.Count == 0)
            return;

        waterfallBitmap.Lock();

        int width = waterfallBitmap.PixelWidth;
        int height = waterfallBitmap.PixelHeight;
        int stride = width * 4;
        byte[] pixels = new byte[stride * height];

        int dataWidth = waterfallData.FirstOrDefault()?.Length ?? 1024;
        double scaleX = (double)width / dataWidth;
        double scaleY = (double)height / 200.0; // 200 rows fixed

        var dataList = waterfallData.ToList();
        for (int dataRowIndex = 0; dataRowIndex < dataList.Count; dataRowIndex++)
        {
            var row = dataList[dataList.Count - 1 - dataRowIndex]; // Reverse: latest on top

            // Compute vertical pixel range this row occupies
            int yStart = (int)(dataRowIndex * scaleY);
            int yEnd = (int)((dataRowIndex + 1) * scaleY);

            yEnd = Math.Min(yEnd, height); // Ensure we don't overflow

            for (int y = yStart; y < yEnd; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int dataX = Math.Min((int)(x / scaleX), row.Length - 1);
                    Color color = GetColorFromPower(row[dataX]);

                    int index = y * stride + x * 4;
                    if (index + 3 < pixels.Length)
                    {
                        pixels[index] = color.B;
                        pixels[index + 1] = color.G;
                        pixels[index + 2] = color.R;
                        pixels[index + 3] = 255;
                    }
                }
            }
        }

        Int32Rect rect = new(0, 0, width, height);
        waterfallBitmap.WritePixels(rect, pixels, stride, 0);
        waterfallBitmap.Unlock();
    }

    private void EnsureWaterfallBitmap()
    {
        var width = (int)Math.Max(WaterfallImage.ActualWidth, 1);
        var height = (int)Math.Max(WaterfallImage.ActualHeight, 1);

        if (waterfallBitmap == null || waterfallBitmap.PixelWidth != width || waterfallBitmap.PixelHeight != height)
        {
            waterfallBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
            WaterfallImage.Source = waterfallBitmap;
        }
    }

    private Color GetColorFromPower(int power)
    {
        power = Math.Max(-120, Math.Min(-20, power));

        var normalized = (power + 120.0) / 100.0;

        var gradient = new (double offset, Color color)[]
        {
            (0.0, Color.FromRgb(0, 0, 255)),
            (0.25, Color.FromRgb(0, 255, 255)),
            (0.5, Color.FromRgb(0, 255, 0)),
            (0.75, Color.FromRgb(255, 255, 0)),
            (1.0, Color.FromRgb(255, 0, 0))
        };

        for (var i = 0; i < gradient.Length - 1; i++)
        {
            var (offset1, color1) = gradient[i];
            var (offset2, color2) = gradient[i + 1];

            if (normalized >= offset1 && normalized <= offset2)
            {
                var ratio = (normalized - offset1) / (offset2 - offset1);
                var r = (byte)(color1.R + (color2.R - color1.R) * ratio);
                var g = (byte)(color1.G + (color2.G - color1.G) * ratio);
                var b = (byte)(color1.B + (color2.B - color1.B) * ratio);
                return Color.FromRgb(r, g, b);
            }
        }

        return gradient[0].color;
    }
}