using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Otanabi.Behaviors;

public class CursorBehavior : BehaviorBase<Control>
{
    public static readonly DependencyProperty CursorProperty = DependencyProperty.Register(
        "Cursor",
        typeof(InputSystemCursorShape),
        typeof(CursorBehavior),
        new PropertyMetadata(InputSystemCursorShape.Arrow)
    );
    public InputSystemCursorShape Cursor
    {
        get { return (InputSystemCursorShape)GetValue(CursorProperty); }
        set
        {
            SetValue(CursorProperty, value);
            SetCursor();
        }
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.Loaded += AssociatedObject_Loaded;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.Loaded -= AssociatedObject_Loaded;
    }

    private bool _loaded;

    private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
    {
        _loaded = true;
        SetCursor();
    }

    private void SetCursor()
    {
        if (!_loaded)
            return;
        //  AssociatedObject.ChangeCursor(InputSystemCursor.Create(Cursor));
    }
}
