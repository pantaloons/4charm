using _4charm.Models;
using _4charm.ViewModels;
using Microsoft.Phone.Controls;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Linq;
using System.Windows;

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

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            _viewModel.OnNavigatedTo();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            _viewModel.OnNavigatedFrom(e);
        }

        private void NameChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            int pos = tb.SelectionStart;
            tb.Text = Regex.Replace(((TextBox)sender).Text.ToLower(), "[^a-z0-9]", "");
            tb.SelectionStart = pos;

            _viewModel.TextUpdated(tb.Text);
        }

        private void NameKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                this.Focus();
            }
        }

        private void AddBoard_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Complete();
        }
    }
}