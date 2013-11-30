using _4charm.Controls;
using _4charm.Resources;
using _4charm.ViewModels;
using Microsoft.Phone.Shell;
using System;
using System.Windows.Input;

namespace _4charm.Views
{
    public partial class NewThreadPage : BoundPage
    {
        private NewThreadPageViewModel _viewModel;

        public NewThreadPage()
        {
            InitializeComponent();
            InitializeApplicationBar();

            _viewModel = new NewThreadPageViewModel();
            DataContext = _viewModel;
        }

        private void InitializeApplicationBar()
        {
            ApplicationBarIconButton submit = new ApplicationBarIconButton(new Uri("Assets/Appbar/appbar.send.png", UriKind.Relative)) { Text = AppResources.ApplicationBar_Submit };
            submit.Click += async (sender, e) =>
            {
                submit.IsEnabled = false;
                NewThreadPageViewModel.NewThreadFocusResult result = await _viewModel.Submit();
                submit.IsEnabled = true;

                switch (result)
                {
                    case NewThreadPageViewModel.NewThreadFocusResult.Captcha:
                        CaptchaTextBox.Focus();
                        break;
                    case NewThreadPageViewModel.NewThreadFocusResult.Page:
                        Focus();
                        break;
                    default:
                        break;
                }
            };

            ApplicationBar = new ApplicationBar();
            ApplicationBar.Buttons.Add(submit);
        }

        private void SubjectTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CaptchaTextBox.Focus();
            }
        }

        private void NameTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                EmailTextBox.Focus();
            }
        }

        private void EmailTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CaptchaTextBox.Focus();
            }
        }

        private void CaptchaTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CommentBox.Focus();
            }
        }
    }
}