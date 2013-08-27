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

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            _viewModel.OnNavigatedTo();
        }
    }
}