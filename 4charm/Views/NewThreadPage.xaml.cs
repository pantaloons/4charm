using _4charm.Controls;
using _4charm.Resources;
using _4charm.ViewModels;
using Microsoft.Phone.Shell;
using System;
using System.Windows;
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

            _viewModel.ElementFocused += ElementFocused;
        }

        private void InitializeApplicationBar()
        {
            ApplicationBarIconButton submit = new ApplicationBarIconButton(new Uri("Assets/Appbar/appbar.send.png", UriKind.Relative)) { Text = AppResources.ApplicationBar_Submit };
            submit.Click += async (sender, e) =>
            {
                submit.IsEnabled = false;
                Focus();
                await _viewModel.Submit();
                submit.IsEnabled = true;
            };

            ApplicationBar = new ApplicationBar();
            ApplicationBar.Buttons.Add(submit);
        }

        private void ElementFocused(object sender, NewThreadPageViewModel.NewThreadFocusResult e)
        {
            switch (e)
            {
                case NewThreadPageViewModel.NewThreadFocusResult.Captcha:
                    Focus();
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        CaptchaTextBox.Focus();
                    });
                    break;
                case NewThreadPageViewModel.NewThreadFocusResult.Comment:
                    CommentBox.Focus();
                    break;
                case NewThreadPageViewModel.NewThreadFocusResult.Page:
                    Focus();
                    break;
                default:
                    break;
            }
        }

        private void SubjectTextBoxNewThread_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CaptchaTextBox.Focus();
            }
        }

        private void SubjectTextBoxReply_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                NameTextBox.Focus();
            }
        }

        private void NameTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                EmailTextBox.Focus();
                RootScroller.ScrollToVerticalOffset(double.MaxValue);
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