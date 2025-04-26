using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Otanabi.UserControls;

public sealed class CustomNavigationView : NavigationView
{
    public static DependencyProperty IsFullScreenProperty = DependencyProperty.Register(
        "IsFullScreen",
        typeof(bool),
        typeof(CustomNavigationView),
        null
    );

    public bool IsFullScreen
    {
        get => (bool)GetValue(IsFullScreenProperty);
        set
        {
            SetValue(IsFullScreenProperty, value);

            if (value)
            {
                IsPaneVisible = false;
                VisualStateManager.GoToState(this, "FullScreen", true);
            }
            else
            {
                IsPaneVisible = true;
                VisualStateManager.GoToState(this, "NotFullScreen", true);
            }
        }
    }
}
