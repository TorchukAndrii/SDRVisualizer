using System.Collections.Concurrent;
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
        // Cancel any ongoing render task before scheduling a new one
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();

        var token = _cancellationTokenSource.Token;

        // Queue the render action to be executed in the dispatcher
        _dispatcher.BeginInvoke(() =>
        {
            try
            {
                if (!token.IsCancellationRequested)
                {
                    renderAction();
                }
            }
            catch (TaskCanceledException)
            {
                // Handle task cancellation gracefully if needed
            }
        });
    }

    public void CancelAll()
    {
        _cancellationTokenSource?.Cancel();
    }
}