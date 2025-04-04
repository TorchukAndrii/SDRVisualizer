using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SDRVisualizer;

public partial class MainWindow : Window
{
    private readonly int[] _gradientPixels;
    private readonly int maxHistory = 200;
    private readonly List<int> spectrumData = new();
    private readonly Queue<int[]> waterfallData = new();
    private readonly FFTDataGenerator _dataGenerator;
    private CancellationTokenSource cancellationTokenSource;
    private bool isRunning;

    private WriteableBitmap spectrumBitmap;
    private WriteableBitmap spectrumBackgroundBitmap;
    private WriteableBitmap waterfallBitmap;
    private byte[] spectrumLinePixels;
    private byte[] waterfallPixels;
    
    private DispatcherTimer _performTimer;
    public MainWindow()
    {
        InitializeComponent();
        
        _dataGenerator = new FFTDataGenerator();
        _gradientPixels = new GradientGenerator().GetGradientPixels();
        InitializeTimer();

        SizeChanged += OnSizeChanged;
        Loaded += (s, e) => InitializeBitmaps();
    }

    private void InitializeTimer()
    {
        _performTimer = new DispatcherTimer();
        _performTimer.Tick += performTimer_Tick;
        _performTimer.Interval = new TimeSpan(0, 0, 0, 0, 20);

    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        InitializeBitmaps();
        RenderSpectrum();
        RenderWaterfall();
    }

    private void InitializeBitmaps()
    {
        
        int width = (int)Math.Max(SpectrumImage.ActualWidth, 1);
        int height = (int)Math.Max(SpectrumImage.ActualHeight, 1);
        spectrumBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
        SpectrumImage.Source = spectrumBitmap;

        spectrumBackgroundBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
        DrawSpectrumBackground(spectrumBackgroundBitmap);

        spectrumLinePixels = new byte[width * height * 4];

        waterfallBitmap = new WriteableBitmap(width, maxHistory, 96, 96, PixelFormats.Pbgra32, null);
        WaterfallImage.Source = waterfallBitmap;
        waterfallPixels = new byte[width * maxHistory * 4];
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        _performTimer.Start();
        /*if (!isRunning)
        {
            isRunning = true;
            cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => RunRenderLoop(cancellationTokenSource.Token));
        }*/
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        _performTimer.Stop();
        isRunning = false;
        cancellationTokenSource?.Cancel();
    }

    private async Task RunRenderLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                UpdateSpectrum();
                RenderSpectrum();
                UpdateWaterfall();
                RenderWaterfall();
            });
            await Task.Delay(50, token);
        }
    }
    private void performTimer_Tick(object sender, EventArgs e)
    {
        this.Perform(false);
    }
    private void Perform(bool force)
    {
        this.DrawLayers();
        //base.Invalidate();
    }
    private void DrawLayers()
    {
        UpdateSpectrum();
        RenderSpectrum();
        UpdateWaterfall();
        RenderWaterfall();
    }
    private void RenderSpectrum()
    {
        int width = spectrumBitmap.PixelWidth;
        int height = spectrumBitmap.PixelHeight;

        Array.Clear(spectrumLinePixels, 0, spectrumLinePixels.Length);
        DrawSpectrumLine(spectrumLinePixels, width, height);

        spectrumBitmap.Lock();

        // Copy background
        spectrumBackgroundBitmap.CopyPixels(new Int32Rect(0, 0, width, height), spectrumBitmap.BackBuffer, spectrumBitmap.BackBufferStride * height, spectrumBitmap.BackBufferStride);

        unsafe
        {
            byte* buffer = (byte*)spectrumBitmap.BackBuffer.ToPointer();
            for (int i = 0; i < width * height * 4; i += 4)
            {
                byte a = spectrumLinePixels[i + 3];
                if (a > 0)
                {
                    buffer[i + 0] = spectrumLinePixels[i + 0];
                    buffer[i + 1] = spectrumLinePixels[i + 1];
                    buffer[i + 2] = spectrumLinePixels[i + 2];
                    buffer[i + 3] = 255;
                }
            }
        }

        spectrumBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
        spectrumBitmap.Unlock();
    }

    private void DrawSpectrumBackground(WriteableBitmap bitmap)
    {
        DrawingVisual visual = new();
        using (var dc = visual.RenderOpen())
        {
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;
            Pen gridPen = new(Brushes.LightGray, 0.5);
            Typeface typeface = new("Arial");
            Brush textBrush = Brushes.White;
            double fontSize = 10;
            double gridPadding = 50;
            double gridWidth = width - 2 * gridPadding;
            double gridHeight = height - 2 * gridPadding;

            for (var i = 0; i <= 20; i++)
            {
                var x = gridPadding + i * (gridWidth / 20.0);
                dc.DrawLine(gridPen, new Point(x, gridPadding), new Point(x, height - gridPadding));
                var freqText = new FormattedText(90 + i + " MHz", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    typeface, fontSize, textBrush, 1.0);
                dc.DrawText(freqText, new Point(x - freqText.Width / 2, height - gridPadding + 5));
            }

            for (var i = 0; i <= 10; i++)
            {
                var y = gridPadding + (10 - i) * (gridHeight / 10.0);
                dc.DrawLine(gridPen, new Point(gridPadding, y), new Point(width - gridPadding, y));
                var powerText = new FormattedText(-120 + i * 10 + " dBm", CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight, typeface, fontSize, textBrush, 1.0);
                dc.DrawText(powerText, new Point(gridPadding - powerText.Width - 5, y - powerText.Height / 2));
            }
        }

        RenderTargetBitmap rtb = new(bitmap.PixelWidth, bitmap.PixelHeight, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(visual);
        bitmap.Lock();
        rtb.CopyPixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight), bitmap.BackBuffer, bitmap.BackBufferStride * bitmap.PixelHeight, bitmap.BackBufferStride);
        bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
        bitmap.Unlock();
    }

    private void DrawSpectrumLine(byte[] pixels, int width, int height)
    {
        double gridPadding = 50;
        double gridWidth = width - 2 * gridPadding;
        double gridHeight = height - 2 * gridPadding;
        double step = gridWidth / 1024.0;

        for (int i = 1; i < spectrumData.Count; i++)
        {
            double x1 = gridPadding + (i - 1) * step;
            double y1 = gridPadding + (1 - (spectrumData[i - 1] + 120) / 100.0) * gridHeight;  // Reverse Y-axis
            double x2 = gridPadding + i * step;
            double y2 = gridPadding + (1 - (spectrumData[i] + 120) / 100.0) * gridHeight;  // Reverse Y-axis

            DrawLine(pixels, width, height, (int)x1, (int)y1, (int)x2, (int)y2, Colors.Yellow);
        }
    }


    private void DrawLine(byte[] pixels, int width, int height, int x0, int y0, int x1, int y1, Color color)
    {
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;

        while (true)
        {
            if (x0 >= 0 && x0 < width && y0 >= 0 && y0 < height)
            {
                int index = (y0 * width + x0) * 4;
                pixels[index + 0] = color.B;
                pixels[index + 1] = color.G;
                pixels[index + 2] = color.R;
                pixels[index + 3] = 255;
            }

            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    private void UpdateSpectrum()
    {
        spectrumData.Clear();
        spectrumData.AddRange(_dataGenerator.GetRow());
    }
    private void UpdateWaterfall()
    {
        if (waterfallData.Count >= maxHistory)
            waterfallData.Dequeue();

        waterfallData.Enqueue(spectrumData.ToArray());
    }

    private void RenderWaterfall()
    {
        if (waterfallBitmap == null || waterfallData.Count == 0) return;

        int width = waterfallBitmap.PixelWidth;
        int height = waterfallBitmap.PixelHeight;
        int stride = width * 4;

        if (waterfallPixels.Length != stride * height)
            waterfallPixels = new byte[stride * height];

        double scaleY = height / (double)maxHistory;
        double scaleX = width / (double)(waterfallData.Peek()?.Length ?? 1024);
        

        int rowIndex = 0;
        foreach (var row in waterfallData.Reverse())
        {
            int yStart = (int)(rowIndex * scaleY);
            int yEnd = Math.Min((int)((rowIndex + 1) * scaleY), height);

            for (int y = yStart; y < yEnd; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int dataX = Math.Min((int)(x / scaleX), row.Length - 1);
                
                    // Normalize power value to [1, 100) range
                    double powerValue = row[dataX];  
                    int powerIndex = (int)((1 - ((powerValue + 120) / 100.0)) * 99);
                    powerIndex = Math.Clamp(powerIndex, 0, 99);  
                    int argb = _gradientPixels[powerIndex];

                    byte b = (byte)(argb & 0xFF);
                    byte g = (byte)((argb >> 8) & 0xFF);
                    byte r = (byte)((argb >> 16) & 0xFF);

                    int index = y * stride + x * 4;
                    waterfallPixels[index + 0] = b;
                    waterfallPixels[index + 1] = g;
                    waterfallPixels[index + 2] = r;
                    waterfallPixels[index + 3] = 255; // Alpha
                }
            }

            rowIndex++;
            if (rowIndex >= maxHistory) break;
        }

        Int32Rect rect = new(0, 0, width, height);
        waterfallBitmap.WritePixels(rect, waterfallPixels, stride, 0);
    }

    private Color GetColorFromPower(int power)
    {
        power = Math.Max(-120, Math.Min(-20, power));
        double normalized = (power + 120.0) / 100.0;

        var gradient = new[]
        {
            (0.0, Color.FromRgb(0, 0, 255)),
            (0.25, Color.FromRgb(0, 255, 255)),
            (0.5, Color.FromRgb(0, 255, 0)),
            (0.75, Color.FromRgb(255, 255, 0)),
            (1.0, Color.FromRgb(255, 0, 0))
        };

        for (int i = 0; i < gradient.Length - 1; i++)
        {
            var (offset1, color1) = gradient[i];
            var (offset2, color2) = gradient[i + 1];

            if (normalized >= offset1 && normalized <= offset2)
            {
                double ratio = (normalized - offset1) / (offset2 - offset1);
                byte r = (byte)(color1.R + (color2.R - color1.R) * ratio);
                byte g = (byte)(color1.G + (color2.G - color1.G) * ratio);
                byte b = (byte)(color1.B + (color2.B - color1.B) * ratio);
                return Color.FromRgb(r, g, b);
            }
        }

        return gradient[0].Item2;
    }
}