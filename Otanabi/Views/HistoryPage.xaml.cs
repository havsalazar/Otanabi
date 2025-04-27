using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Otanabi.ViewModels;

namespace Otanabi.Views;

public sealed partial class HistoryPage : Page
{
    public HistoryViewModel ViewModel { get; }

    public HistoryPage()
    {
        ViewModel = App.GetService<HistoryViewModel>();
        InitializeComponent();
    }

    private void DeleteFromHistory(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            ViewModel.DeleteHistoryByIdCommand.Execute(button.Tag);
            CloseConfirmationPopUp(button);
        }
    }

    private void CancelDeleteHistory(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            CloseConfirmationPopUp(button);
        }
    }

    private void CloseConfirmationPopUp(Button button)
    {
        /* the structure is like this: (the x:Name is the name var does not represent the current x:Name attribute)
        <Popup x:Name="pp">
            <FlyoutPresenter x:Name="fp">
                <StackPanel x:Name="sp">
                    <StackPanel x:Name="sp2">
                        <Button x:Name="DeleteHistory" Click="DeleteFromHistory" Tag="{Binding Id}"/>
                        <Button x:Name="CancelDeleteHistory" Click="CancelDeleteHistory"/>
                    </StackPanel>
                </StackPanel>
            </FlyoutPresenter>
        </Popup>
         */

        if (
            button.Parent is StackPanel sp2
            && sp2.Parent is StackPanel sp
            && sp.Parent is FlyoutPresenter fp
            && fp.Parent is Popup pp
        )
        {
            pp.IsOpen = false;
        }
    }
}
