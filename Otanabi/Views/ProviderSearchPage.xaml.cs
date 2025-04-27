using Microsoft.UI.Xaml.Controls;
using Otanabi.ViewModels;

namespace Otanabi.Views;

public sealed partial class ProviderSearchPage : Page
{
    public ProviderSearchViewModel ViewModel { get; }

    public ProviderSearchPage()
    {
        ViewModel = App.GetService<ProviderSearchViewModel>();
        InitializeComponent();
    }
}
