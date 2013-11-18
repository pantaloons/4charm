using _4charm.Controls;
using _4charm.ViewModels;
using Microsoft.Phone.Controls;
using System.Windows;
using System.Windows.Input;

namespace _4charm.Views
{
    public partial class AddBoardPage : BoundPage
    {
        public AddBoardPage()
        {
            InitializeComponent();

            DataContext = new AddBoardPageViewModel();
        }

        private void NameKeyDown(object sender, KeyEventArgs e)
        {
            // Clear focus to hide the SIP when enter pressed
            if (e.Key == Key.Enter)
            {
                Focus();
            }
        }

        private void ContextMenuOpened(object sender, System.Windows.RoutedEventArgs e)
        {
            var menu = (ContextMenu)sender;
            var owner = (FrameworkElement)menu.Owner;
            if (owner.DataContext != menu.DataContext) menu.DataContext = owner.DataContext;
        }
    }
}