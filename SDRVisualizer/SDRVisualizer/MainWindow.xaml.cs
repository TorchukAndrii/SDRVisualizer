using System.Windows;
using Timer = System.Timers.Timer;

namespace SDRVisualizer;

public partial class MainWindow : Window
{
    private readonly FFTDataGenerator _fftDataGenerator;
    private Timer updateTimer;

    public MainWindow()
    {
        _fftDataGenerator = new FFTDataGenerator();

        InitializeComponent();
        InitializeTimer();
    }

    private void InitializeTimer()
    {
        updateTimer = new Timer(50); // Update every 50ms (20 FPS)
        updateTimer.Elapsed += (s, e) => Dispatcher.Invoke(UpdateVisualization);
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        updateTimer.Start();
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        updateTimer.Stop();
    }

    private void UpdateVisualization()
    {
        var newData = _fftDataGenerator.GetRow();
        SpectrumControl.UpdateSpectrumData(newData);
        WaterfallControl.AddSpectrumSnapshot(newData);
    }
}