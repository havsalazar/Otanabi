using Microsoft.UI.Xaml.Controls;
using Otanabi.ViewModels;

namespace Otanabi.Views;

public sealed partial class SeasonalPage : Page
{
    public SeasonalViewModel ViewModel { get; }

    public SeasonalPage()
    {
        ViewModel = App.GetService<SeasonalViewModel>();
        InitializeComponent();
    }
}
