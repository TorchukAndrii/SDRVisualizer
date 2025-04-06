using System.Windows.Threading;

namespace SDRVisualizer.Helpers;

public class RenderScheduler
{
    private readonly Dispatcher _dispatcher;
    private CancellationTokenSource _cancellationTokenSource;

    public RenderScheduler(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public void EnqueueRender(Action renderAction)
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();

        var token = _cancellationTokenSource.Token;

        _dispatcher.BeginInvoke(() =>
        {
            try
            {
                if (!token.IsCancellationRequested) renderAction();
            }
            catch (TaskCanceledException)
            {
            }
        });
    }

    public void CancelAll()
    {
        _cancellationTokenSource?.Cancel();
    }
}