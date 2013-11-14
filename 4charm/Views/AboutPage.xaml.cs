using _4charm.Controls;
using _4charm.ViewModels;

namespace _4charm.Views
{
    public partial class AboutPage : BoundPage
    {
        public AboutPage()
        {
            InitializeComponent();

            DataContext = new AboutPageViewModel();
        }
    }
}