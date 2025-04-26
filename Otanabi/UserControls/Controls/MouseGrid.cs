using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Otanabi.UserControls;

public sealed class MouseGrid : Grid, IDisposable
{
    private readonly DispatcherQueue _dispatcherQueue;
    public InputCursor InputCursor
    {
        get => base.ProtectedCursor;
        set => base.ProtectedCursor = value;
    }
    private bool isPointerVisible = true;
    private readonly DispatcherTimer pointerHideTimer =
        new()
        {
            Interval = TimeSpan.FromSeconds(2)
        };

    public MouseGrid()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        PointerMoved += OnPointerMoved;
        PointerEntered += (s, e) => ShowPointer();
        pointerHideTimer.Tick += Timer_Tick;
    }

    private void OnPointerMoved(object? sender, object e)
    {
        if (!isPointerVisible)
        {
            ShowPointer();
        }
        else
        {
            pointerHideTimer.Stop();
            pointerHideTimer.Start();
        }
    }

    private void ShowPointer()
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            InputCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
            isPointerVisible = true;

        });

    }

    private void HidePointer()
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            isPointerVisible = false;
            if (InputCursor != null)
            {
                InputCursor.Dispose();
            }
            else
            {
                InputCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
                InputCursor.Dispose();
            }
        });
    }

    private void Timer_Tick(object? sender, object e)
    {
        HidePointer();
        pointerHideTimer.Stop();
    }

    public void Dispose()
    {
        InputCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
    }
}
