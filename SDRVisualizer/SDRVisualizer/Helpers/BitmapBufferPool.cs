namespace SDRVisualizer.Helpers;

public class BitmapBufferPool
{
    public byte[] GetSpectrumBuffer(int width, int height)
    {
        return new byte[width * height * 4]; // Pbgra32, 4 bytes per pixel
    }

    public byte[] GetWaterfallBuffer(int width, int height)
    {
        return new byte[width * height * 4]; // Pbgra32, 4 bytes per pixel
    }
}