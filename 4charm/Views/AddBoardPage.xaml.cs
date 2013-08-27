using _4charm.Models;
using _4charm.ViewModels;
using Microsoft.Phone.Controls;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace _4charm.Views
{
    public partial class AddBoardPage : PhoneApplicationPage
    {
        private AddBoardPageViewModel _viewModel;

        public AddBoardPage()
        {
            InitializeComponent();

            _viewModel = new AddBoardPageViewModel();
            DataContext = _viewModel;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            if (e.NavigationMode == NavigationMode.Back)
            {
                if (_viewModel.Board != null) _viewModel.Board.UnloadImage();
            }
        }

        private void NameChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            int pos = tb.SelectionStart;
            tb.Text = Regex.Replace(((TextBox)sender).Text.ToLower(), "[^a-z0-9]", "");
            tb.SelectionStart = pos;

            _viewModel.TextChanged(tb.Text);
        }

        private void NameKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Focus();
            }
        }
    }
}