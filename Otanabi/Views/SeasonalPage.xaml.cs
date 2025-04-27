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

    private void Page_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.LoadedViewCommand.Execute(SelectorSeason);
    }

    private void YearCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (object.ReferenceEquals(sender, YearCB))
        {
            //ViewModel.SelectedYear = (int)YearCB.SelectedItem;
        }
    }
}
