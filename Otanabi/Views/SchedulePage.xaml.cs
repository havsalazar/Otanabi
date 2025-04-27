using Microsoft.UI.Xaml.Controls;
using Otanabi.ViewModels;

namespace Otanabi.Views;

public sealed partial class SchedulePage : Page
{
    public ScheduleViewModel ViewModel { get; }

    public SchedulePage()
    {
        ViewModel = App.GetService<ScheduleViewModel>();
        InitializeComponent();
    }
}
