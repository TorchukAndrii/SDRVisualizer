using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SDRVisualizer
{
    public partial class MainWindow : Window
    {
        private WriteableBitmap spectrumBitmap;
        private readonly List<int> spectrumData = new();
        private bool isRunning = false;
        private CancellationTokenSource cancellationTokenSource;
        private FFTDataGenerator _dataGenerator;

        public MainWindow()
        {
            InitializeComponent();
            _dataGenerator = new FFTDataGenerator();
            SizeChanged += OnSizeChanged;
            InitializeBitmaps();
        }

        private void InitializeBitmaps()
        {
            int width = (int)Math.Max(SpectrumImage.ActualWidth, 1);
            int height = (int)Math.Max(SpectrumImage.ActualHeight, 1);
            spectrumBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
            SpectrumImage.Source = spectrumBitmap;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            InitializeBitmaps();
            RenderSpectrum();
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
                await Dispatcher.InvokeAsync(RenderSpectrum);
                await Task.Delay(50, token);
            }
        }

        private void RenderSpectrum()
        {
            int width = (int)SpectrumImage.ActualWidth;
            int height = (int)SpectrumImage.ActualHeight;
            if (width == 0 || height == 0) return;

            spectrumBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
            SpectrumImage.Source = spectrumBitmap;
            spectrumBitmap.Lock();

            Int32Rect rect = new(0, 0, width, height);
            int stride = width * 4;
            byte[] pixels = new byte[stride * height];

            double gridPadding = 50;
            double gridWidth = width - 2 * gridPadding;
            double gridHeight = height - 2 * gridPadding;

            DrawingVisual visual = new();
            using (DrawingContext dc = visual.RenderOpen())
            {
                Pen gridPen = new(Brushes.LightGray, 0.5);
                Typeface typeface = new("Arial");
                Brush textBrush = Brushes.White;
                double fontSize = 10;

                for (int i = 0; i <= 20; i++)
                {
                    double x = gridPadding + i * (gridWidth / 20.0);
                    dc.DrawLine(gridPen, new Point(x, gridPadding), new Point(x, height - gridPadding));
                    FormattedText freqText = new((90 + i) + " MHz", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize, textBrush, 1.0);
                    dc.DrawText(freqText, new Point(x - freqText.Width / 2, height - gridPadding + 5));
                }

                for (int i = 0; i <= 10; i++)
                {
                    double y = gridPadding + i * (gridHeight / 10.0);
                    dc.DrawLine(gridPen, new Point(gridPadding, y), new Point(width - gridPadding, y));
                    FormattedText powerText = new((-120 + i * 10) + " dBm", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize, textBrush, 1.0);
                    dc.DrawText(powerText, new Point(gridPadding - powerText.Width - 5, y - powerText.Height / 2));
                }

                Pen spectrumPen = new(Brushes.Yellow, 1);
                double step = gridWidth / 1024.0;
                for (int i = 1; i < spectrumData.Count; i++)
                {
                    double x1 = gridPadding + (i - 1) * step;
                    double y1 = gridPadding + (gridHeight - ((spectrumData[i - 1] + 120) * gridHeight / 100));
                    double x2 = gridPadding + i * step;
                    double y2 = gridPadding + (gridHeight - ((spectrumData[i] + 120) * gridHeight / 100));
                    dc.DrawLine(spectrumPen, new Point(x1, y1), new Point(x2, y2));
                }
            }

            RenderTargetBitmap targetBitmap = new(width, height, 96, 96, PixelFormats.Pbgra32);
            targetBitmap.Render(visual);
            targetBitmap.CopyPixels(rect, pixels, stride, 0);
            spectrumBitmap.WritePixels(rect, pixels, stride, 0);
            spectrumBitmap.Unlock();
        }
    }
}