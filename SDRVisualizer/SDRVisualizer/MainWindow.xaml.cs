using System;
using System.Collections.Generic;
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
        private readonly int width = 1024;
        private readonly int height = 200;
        private WriteableBitmap waterfallBitmap;
        private readonly List<int> spectrumData = new();
        private readonly List<int[]> waterfallData = new();
        private bool isRunning = false;
        private CancellationTokenSource cancellationTokenSource;
        private Random rand = new();

        public MainWindow()
        {
            InitializeComponent();
            waterfallBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Rgb24, null);
            //WaterfallImage.Source = waterfallBitmap;
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
            double phaseShift = 0;
            while (!token.IsCancellationRequested)
            {
                /*spectrumData.Clear();
                int prev = rand.Next(-120, -20);
                for (int i = 0; i < width; i++)
                {
                    double frequency = 90 + (20.0 * i / width);
                    double signal = -60 + 30 * Math.Sin(2 * Math.PI * (frequency + phaseShift) / 10);
                    double noise = rand.NextDouble() * 10 - 5;
                    double power = Math.Max(-120, Math.Min(-20, signal + noise));
                    spectrumData.Add((int)power);
                }
                phaseShift += 0.2;*/
                if (spectrumData.Count == 0)
                {
                    for (int i = 0; i < width; i++)
                    {
                        double frequency = 90 + (20.0 * i / width);
                        double signal = -60 + 30 * Math.Sin(2 * Math.PI * (frequency + phaseShift) / 10);
                        double noise = rand.NextDouble() * 10 - 5;
                        double power = Math.Max(-120, Math.Min(-20, signal + noise));
                        spectrumData.Add((int)power);
                    }
                }
                await Dispatcher.InvokeAsync(() =>
                {
                    RenderSpectrum();
                    //UpdateWaterfall();
                });
                await Task.Delay(50, token);
            }
        }

        private void RenderSpectrum()
        {
            DrawingVisual visual = new();
            using (DrawingContext dc = visual.RenderOpen())
            {
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, width, height));
                Pen spectrumPen = new Pen(Brushes.Yellow, 1);
                for (int i = 1; i < spectrumData.Count; i++)
                {
                    double x1 = (i - 1);
                    double y1 = height - ((spectrumData[i - 1] + 120) * height / 100);
                    double x2 = i;
                    double y2 = height - ((spectrumData[i] + 120) * height / 100);
                    dc.DrawLine(spectrumPen, new Point(x1, y1), new Point(x2, y2));
                }
            }

            RenderTargetBitmap bitmap = new(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            SpectrumImage.Source = bitmap;
        }

        /*private void UpdateWaterfall()
        {
            if (waterfallData.Count >= height)
            {
                waterfallData.RemoveAt(0);
            }
            waterfallData.Add(spectrumData.ToArray());

            int stride = width * 3;
            byte[] pixels = new byte[height * stride];
            for (int y = 0; y < waterfallData.Count; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte intensity = (byte)((spectrumData[x] + 120) * 255 / 100);
                    pixels[y * stride + x * 3 + 0] = (byte)(255 - intensity);
                    pixels[y * stride + x * 3 + 1] = intensity;
                    pixels[y * stride + x * 3 + 2] = (byte)(intensity / 2);
                }
            }

            waterfallBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
        }*/
    }
}