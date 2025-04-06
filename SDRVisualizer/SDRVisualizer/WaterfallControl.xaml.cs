using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SDRVisualizer.Controls
{
    public partial class WaterfallControl : UserControl
    {
        private WriteableBitmap _waterfallBitmap;
        private byte[] _waterfallPixels;
        private Queue<int[]> _waterfallData = new();
        private int[] _gradientPixels;
        private const int MaxHistory = 200;

        public WaterfallControl()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                _gradientPixels = new GradientGenerator().GetGradientPixels();
                InitializeBitmap();
            };
            SizeChanged += (s, e) => RenderWaterfall();
        }

        public void UpdateWaterfall(int[] newRow)
        {
            if (newRow == null || newRow.Length == 0) return;

            if (_waterfallData.Count >= MaxHistory)
                _waterfallData.Dequeue();

            _waterfallData.Enqueue(newRow.ToArray());
            RenderWaterfall();
        }

        private void InitializeBitmap()
        {
            int width = Math.Max((int)ActualWidth, 1);
            int height = MaxHistory;

            _waterfallBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
            _waterfallPixels = new byte[width * height * 4];

            WaterfallImage.Source = _waterfallBitmap;
        }

        private void RenderWaterfall()
        {
            if (_waterfallBitmap == null || _gradientPixels == null || _waterfallData.Count == 0)
                return;

            int width = _waterfallBitmap.PixelWidth;
            int height = _waterfallBitmap.PixelHeight;
            int stride = width * 4;

            double scaleY = height / (double)MaxHistory;
            double scaleX = width / (double)(_waterfallData.Peek()?.Length ?? 1024);

            Array.Clear(_waterfallPixels, 0, _waterfallPixels.Length);

            int rowIndex = 0;
            foreach (var row in _waterfallData.Reverse())
            {
                int yStart = (int)(rowIndex * scaleY);
                int yEnd = Math.Min((int)((rowIndex + 1) * scaleY), height);

                for (int y = yStart; y < yEnd; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int dataX = Math.Min((int)(x / scaleX), row.Length - 1);
                        double power = row[dataX];
                        int powerIndex = (int)((1 - ((power + 120) / 100.0)) * 99);
                        powerIndex = Math.Clamp(powerIndex, 0, 99);

                        int argb = _gradientPixels[powerIndex];
                        byte b = (byte)(argb & 0xFF);
                        byte g = (byte)((argb >> 8) & 0xFF);
                        byte r = (byte)((argb >> 16) & 0xFF);

                        int index = y * stride + x * 4;
                        _waterfallPixels[index + 0] = b;
                        _waterfallPixels[index + 1] = g;
                        _waterfallPixels[index + 2] = r;
                        _waterfallPixels[index + 3] = 255;
                    }
                }

                rowIndex++;
                if (rowIndex >= MaxHistory) break;
            }

            _waterfallBitmap.WritePixels(new Int32Rect(0, 0, width, height), _waterfallPixels, stride, 0);
        }
    }
}
