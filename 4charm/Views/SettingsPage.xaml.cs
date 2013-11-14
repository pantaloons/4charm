using _4charm.Controls;
using _4charm.ViewModels;

namespace _4charm.Views
{
    public partial class SettingsPage : BoundPage
    {
        public SettingsPage()
        {
            InitializeComponent();

            DataContext = new SettingsPageViewModel();
        }
    }
}