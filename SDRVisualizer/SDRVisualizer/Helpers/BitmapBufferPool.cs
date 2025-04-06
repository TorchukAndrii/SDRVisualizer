namespace SDRVisualizer.Helpers;

public class BitmapBufferPool
{
    private const int DefaultWidth = 1024;
    private const int DefaultHeight = 512;
    private readonly int _bufferSize;
        
    public BitmapBufferPool()
    {
        _bufferSize = DefaultWidth * DefaultHeight * 4; // 4 bytes per pixel (Pbgra32)
    }

    public byte[] GetSpectrumBuffer(int width, int height)
    {
        return new byte[width * height * 4]; // Pbgra32, 4 bytes per pixel
    }

    public byte[] GetWaterfallBuffer(int width, int height)
    {
        return new byte[width * height * 4]; // Pbgra32, 4 bytes per pixel
    }
}