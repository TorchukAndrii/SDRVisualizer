using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SDRVisualizer.Controls
{
    public partial class SpectrumControl : UserControl
    {
        private WriteableBitmap spectrumBitmap;
        private WriteableBitmap backgroundBitmap;
        private byte[] spectrumPixels;

        private List<int> spectrumData = new();

        public SpectrumControl()
        {
            InitializeComponent();
            Loaded += (s, e) => InitializeBitmap();
            SizeChanged += (s, e) => RenderSpectrum();
        }

        public void UpdateSpectrum(int[] newData)
        {
            spectrumData.Clear();
            spectrumData.AddRange(newData);
            RenderSpectrum();
        }
        private void InitializeBitmap()
        {
            int width = Math.Max((int)ActualWidth, 1);
            int height = Math.Max((int)ActualHeight, 1);

            spectrumBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
            backgroundBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
            spectrumPixels = new byte[width * height * 4];

            DrawBackground(backgroundBitmap);
            SpectrumImage.Source = spectrumBitmap;
        }

        private void RenderSpectrum()
        {
            if (spectrumBitmap == null) return;

            int width = spectrumBitmap.PixelWidth;
            int height = spectrumBitmap.PixelHeight;

            Array.Clear(spectrumPixels, 0, spectrumPixels.Length);
            DrawSpectrumLine(spectrumPixels, width, height);

            spectrumBitmap.Lock();
            backgroundBitmap.CopyPixels(new Int32Rect(0, 0, width, height), spectrumBitmap.BackBuffer, spectrumBitmap.BackBufferStride * height, spectrumBitmap.BackBufferStride);

            unsafe
            {
                byte* buffer = (byte*)spectrumBitmap.BackBuffer.ToPointer();
                for (int i = 0; i < width * height * 4; i += 4)
                {
                    if (spectrumPixels[i + 3] > 0)
                    {
                        buffer[i + 0] = spectrumPixels[i + 0];
                        buffer[i + 1] = spectrumPixels[i + 1];
                        buffer[i + 2] = spectrumPixels[i + 2];
                        buffer[i + 3] = 255;
                    }
                }
            }

            spectrumBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            spectrumBitmap.Unlock();
        }

        private void DrawBackground(WriteableBitmap bitmap)
        {
            DrawingVisual visual = new();
            using (DrawingContext dc = visual.RenderOpen())
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

                for (int i = 0; i <= 20; i++)
                {
                    double x = gridPadding + i * (gridWidth / 20.0);
                    dc.DrawLine(gridPen, new Point(x, gridPadding), new Point(x, height - gridPadding));
                    var freqText = new FormattedText($"{90 + i} MHz", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize, textBrush, 1.0);
                    dc.DrawText(freqText, new Point(x - freqText.Width / 2, height - gridPadding + 5));
                }

                for (int i = 0; i <= 10; i++)
                {
                    double y = gridPadding + (10 - i) * (gridHeight / 10.0);
                    dc.DrawLine(gridPen, new Point(gridPadding, y), new Point(width - gridPadding, y));
                    var powerText = new FormattedText($"{-120 + i * 10} dBm", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize, textBrush, 1.0);
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
                double y1 = gridPadding + (1 - (spectrumData[i - 1] + 120) / 100.0) * gridHeight;
                double x2 = gridPadding + i * step;
                double y2 = gridPadding + (1 - (spectrumData[i] + 120) / 100.0) * gridHeight;
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
    }
}
