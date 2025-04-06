using System.Windows.Media;

namespace SDRVisualizer.Helpers;

public static class DrawingHelpers
{
    public static void DrawLine(byte[] pixels, int width, int height, int x1, int y1, int x2, int y2, Color color)
    {
        int dx = Math.Abs(x2 - x1);
        int dy = Math.Abs(y2 - y1);
        int sx = x1 < x2 ? 1 : -1;
        int sy = y1 < y2 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            if (x1 >= 0 && x1 < width && y1 >= 0 && y1 < height)
            {
                int index = (y1 * width + x1) * 4;
                pixels[index + 0] = color.B;
                pixels[index + 1] = color.G;
                pixels[index + 2] = color.R;
                pixels[index + 3] = 255; // Alpha
            }

            if (x1 == x2 && y1 == y2)
                break;

            int e2 = err * 2;
            if (e2 > -dy)
            {
                err -= dy;
                x1 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y1 += sy;
            }
        }
    }
}