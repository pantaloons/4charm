using _4charm.ViewModels;
using Microsoft.Phone.Controls;

namespace _4charm.Views
{
    public partial class AboutPage : PhoneApplicationPage
    {
        private AboutPageViewModel _viewModel;

        public AboutPage()
        {
            InitializeComponent();

            _viewModel = new AboutPageViewModel();
            DataContext = _viewModel;
        }
    }
}