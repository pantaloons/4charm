using _4charm.ViewModels;
using Microsoft.Phone.Controls;

namespace _4charm.Views
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        private SettingsPageViewModel _viewModel;

        public SettingsPage()
        {
            InitializeComponent();

            _viewModel = new SettingsPageViewModel();
            DataContext = _viewModel;
        }
    }
}