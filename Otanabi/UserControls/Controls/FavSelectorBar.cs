using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Otanabi.UserControls;
class FavSelectorBar : SelectorBar
{
    public static DependencyProperty ItemSourceProperty = DependencyProperty.Register("ItemSource", typeof(ObservableCollection<SelectorBarItem>), typeof(FavSelectorBar), null);

    public ObservableCollection<SelectorBarItem> ItemSource
    {
        get => (ObservableCollection<SelectorBarItem>)GetValue(ItemSourceProperty);
        set
        {
            SetValue(ItemSourceProperty, value);
            Items.Clear();
            foreach (var item in value)
            {
                Items.Add(item);
            }
        }
    }
}
