using _4charm.Models;
using _4charm.Resources;
using _4charm.Views;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace _4charm.ViewModels
{
    class NewThreadPageViewModel : ReplyPageViewModelBase
    {
        public enum NewThreadFocusResult
        {
            None,
            Captcha,
            Comment,
            Page
        };

        public event EventHandler<NewThreadFocusResult> ElementFocused;

        public bool HasMoreDetails
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public bool IsNewThread
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public string PageTitle
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public ICommand MoreDetails
        {
            get { return GetProperty<ICommand>(); }
            set { SetProperty(value); }
        }

        private ulong _threadID;
        private Regex _threadIDRegex;

        public NewThreadPageViewModel()
        {
            MoreDetails = new ModelCommand(() => HasMoreDetails = true);
            ReloadCaptcha = new ModelCommand(async () =>
            {
                CaptchaText = "";
                ElementFocused(this, NewThreadFocusResult.Captcha);
                await DoLoadCaptcha();
            });
        }

        public override void Initialize(IDictionary<string, string> arguments, NavigationEventArgs e)
        {
            base.Initialize(arguments, e);

            if (arguments.ContainsKey("thread"))
            {
                IsNewThread = false;

                _threadID = ulong.Parse(arguments["thread"]);
                _token = arguments["token"];
                CaptchaText = arguments["captcha"];
                Comment = arguments["comment"];

                if (!string.IsNullOrEmpty(_token))
                {
                    CaptchaUri = RequestManager.Current.EnforceHTTPS(new Uri("http://www.google.com/recaptcha/api/image?c=" + _token));
                }
                else
                {
                    CaptchaText = "";
                    DoLoadCaptcha().ContinueWith(task =>
                    {
                        throw task.Exception;
                    }, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted);
                }

                Thread t = ThreadCache.Current.EnforceBoard(Board).EnforceThread(_threadID);
                PageTitle = string.Format(AppResources.NewThreadPage_NewPostPageTitle, Board, t.Subject ?? t.Number + "");
            }
            else
            {
                PageTitle = string.Format(AppResources.NewThreadPage_NewThreadPageTitle, Board);
                IsNewThread = true;

                DoLoadCaptcha().ContinueWith(result =>
                {
                    throw result.Exception;
                }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
            }

            _threadIDRegex = new Regex("<meta http-equiv=\"refresh\" content=\"1;URL=http://boards\\.4chan\\.org/" + Board + "/res/(\\d+)\">");
        }

        public override void SaveState(IDictionary<string, object> state)
        {
            base.SaveState(state);

            state["HasMoreDetails"] = HasMoreDetails;
        }

        public override void RestoreState(IDictionary<string, object> state)
        {
            base.RestoreState(state);

            if (state.ContainsKey("HasMoreDetails"))
            {
                HasMoreDetails = (bool)state["HasMoreDetails"];
            }
        }

        protected override void OnSubmitSuccess(string result)
        {
            if (_threadID == 0)
            {
                OnNewThreadSubmitSuccess(result);
            }
            else
            {
                OnNewPostSubmitSuccess();
            }
        }

        private void OnNewThreadSubmitSuccess(string result)
        {
            Match m;
            ulong thread = 0;
            if ((m = _threadIDRegex.Match(result)).Success)
            {
                ulong.TryParse(m.Groups[1].Value, out thread);
            }

            ThreadsPageViewModel.ForceReload = true;

            GoBack();
            if (thread != 0)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    Navigate(new Uri(String.Format("/Views/PostsPage.xaml?board={0}&thread={1}",
                        Uri.EscapeUriString(Board), Uri.EscapeUriString(thread + "")), UriKind.Relative));
                });
            }
        }

        private void OnNewPostSubmitSuccess()
        {
            PostsPageViewModel.ForceReload = true;
            GoBack();
        }

        public async Task Submit()
        {
            SubmitResultType result = await SubmitInternal(_threadID);

            switch (result)
            {
                case SubmitResultType.EmptyCaptchaError:
                case SubmitResultType.WrongCatpchaError:
                    ElementFocused(this, NewThreadFocusResult.Captcha);
                    break;
                case SubmitResultType.EmptyCommentError:
                    ElementFocused(this, NewThreadFocusResult.Comment);
                    break;
                case SubmitResultType.NoImageError:
                    ElementFocused(this, NewThreadFocusResult.Page);
                    break;
                case SubmitResultType.Success:
                case SubmitResultType.KnownError:
                case SubmitResultType.UnknownError:
                default:
                    break;
            }
        }
    }
}
