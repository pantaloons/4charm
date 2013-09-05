using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using _4charm.Resources;
using _4charm.ViewModels;
using System.Windows.Input;
using System.Threading.Tasks;

namespace _4charm.Views
{
    public partial class NewThreadPage : PhoneApplicationPage
    {
        private NewThreadPageViewModel _viewModel;

        public NewThreadPage()
        {
            InitializeComponent();
            InitializeApplicationBar();

            _viewModel = new NewThreadPageViewModel(ImageChanged);
            DataContext = _viewModel;
        }

        private void InitializeApplicationBar()
        {
            ApplicationBarIconButton submit = new ApplicationBarIconButton(new Uri("Assets/Appbar/appbar.send.png", UriKind.Relative)) { Text = AppResources.ApplicationBar_Submit };
            submit.Click += async (sender, e) =>
            {
                submit.IsEnabled = false;
                SubmitResult result = await _viewModel.Submit();
                submit.IsEnabled = true;

                switch (result.ResultType)
                {
                    case SubmitResultType.Success:
                        ThreadsPage.ForceReload = true;
                        NavigationService.GoBack();
                        if (result.ThreadID != 0)
                        {
                            Dispatcher.BeginInvoke(() =>
                            {
                                NavigationService.Navigate(new Uri(String.Format("/Views/PostsPage.xaml?board={0}&thread={1}",
                                    Uri.EscapeUriString(_viewModel.Board), Uri.EscapeUriString(result.ThreadID + "")), UriKind.Relative));
                            });
                        }
                        break;
                    case SubmitResultType.EmptyCaptchaError:
                    case SubmitResultType.WrongCatpchaError:
                        HighlightCaptchaStoryboard.Begin();
                        CaptchaTextBox.Focus();
                        CaptchaTextBox.Text = "";
                        if (result.ResultType == SubmitResultType.WrongCatpchaError) _viewModel.ReloadCaptcha.Execute(null);
                        break;
                    case SubmitResultType.NoImageError:
                        Focus();
                        HighlightCaptchaStoryboard.Stop();
                        HighlightImageStoryboard.Stop();
                        HighlightImageStoryboard.Begin();
                        break;
                    case SubmitResultType.KnownError:
                        MessageBox.Show(result.ErrorMessage);
                        break;
                    
                    case SubmitResultType.EmptyCommentError:
                    case SubmitResultType.UnknownError:
                        MessageBox.Show("Unknown error encountered submitting thread.");
                        break;
                }
            };

            ApplicationBar = new ApplicationBar();
            ApplicationBar.Buttons.Add(submit);
        }

        private bool _initialized;
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (!_initialized)
            {
                string boardName = NavigationContext.QueryString["board"];
                _viewModel.OnNavigatedTo(boardName);

                _initialized = true;
            }
        }

        private void ImageChanged()
        {
            HighlightImageStoryboard.Stop();
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);

            if (_viewModel.IsPosting)
            {
                e.Cancel = true;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ((TextBox)sender).GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }

        private void SubjectTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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