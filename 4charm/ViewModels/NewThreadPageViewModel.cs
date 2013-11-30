using _4charm.Models;
using _4charm.Models.API;
using _4charm.Views;
using Microsoft.Phone.Tasks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace _4charm.ViewModels
{
    class NewThreadPageViewModel : PageViewModelBase
    {
        public enum NewThreadFocusResult
        {
            None,
            Captcha,
            Page
        };

        private static readonly Regex ErrorRegex = new Regex("<span id=\"errmsg\" style=\"color: red;\">(Error: [^<]+)<");

        private static readonly Regex SuccessRegex = new Regex("<title>Post successful!</title>");

        public bool HasMoreDetails
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public bool IsPosting
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public string PageTitle
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public string CaptchaText
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public Uri CaptchaUri
        {
            get { return GetProperty<Uri>(); }
            set { SetProperty(value); }
        }

        public string Comment
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public string Subject
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public string Board
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public string Name
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public bool HasImage
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public string FileName
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public string Email
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public bool IsCaptchaError
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public bool IsImageError
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public ICommand ReloadCaptcha
        {
            get { return GetProperty<ICommand>(); }
            set { SetProperty(value); }
        }

        public ICommand AddImage
        {
            get { return GetProperty<ICommand>(); }
            set { SetProperty(value); }
        }

        public ICommand MoreDetails
        {
            get { return GetProperty<ICommand>(); }
            set { SetProperty(value); }
        }

        private string _token;
        private byte[] _imageData;
        private Regex _threadIDRegex;

        public NewThreadPageViewModel()
        {
            ReloadCaptcha = new ModelCommand(async () => await LoadCaptcha());
            AddImage = new ModelCommand(() => DoAddImage());
            MoreDetails = new ModelCommand(() => HasMoreDetails = true);
        }

        public override void Initialize(IDictionary<string, string> arguments, NavigationEventArgs e)
        {
            Comment = "";
            CaptchaText = "";
            Subject = "";
            Name = "";
            Email = "";

            Board = arguments["board"];
            _threadIDRegex = new Regex("<meta http-equiv=\"refresh\" content=\"1;URL=http://boards\\.4chan\\.org/" + Board + "/res/(\\d+)\">");

            PageTitle = "NEW THREAD - /" + Board + "/";
            FileName = "choose file";

            LoadCaptcha().ContinueWith(result =>
            {
                throw result.Exception;
            }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }

        public override void OnBackKeyPress(CancelEventArgs e)
        {
            base.OnBackKeyPress(e);

            if (IsPosting)
            {
                e.Cancel = true;
            }
        }

        public async Task LoadCaptcha()
        {
            try
            {
                _token = await GetCaptcha();
            }
            catch
            {
                MessageBox.Show("Captcha data is corrupt.");
                return;
            }

            CaptchaText = "";
            CaptchaUri = RequestManager.Current.EnforceHTTPS(new Uri("http://www.google.com/recaptcha/api/image?c=" + _token));
        }

        public async Task<string> GetCaptcha()
        {
            Uri uri = RequestManager.Current.EnforceHTTPS(new Uri("http://www.google.com/recaptcha/api/challenge?k=6Ldp2bsSAAAAAAJ5uyx_lx34lJeEpTLVkP5k04qc"));
            string page = await RequestManager.Current.GetStringAsync(uri);

            int start = page.IndexOf("{");
            int end = page.LastIndexOf("}");

            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(page.Substring(start, end - start + 1))))
            {
                DataContractJsonSerializer dcjs = new DataContractJsonSerializer(typeof(APICaptcha));

                APICaptcha captcha = await dcjs.ReadObjectAsync<APICaptcha>(ms);

                return captcha.Challenge;
            }
        }

        public async Task<NewThreadFocusResult> Submit()
        {
            IsPosting = true;
            NewThreadFocusResult result = await SubmitInternal();
            IsPosting = false;
            return result;
        }

        private async Task<NewThreadFocusResult> SubmitInternal()
        {
            IsCaptchaError = false;
            IsImageError = false;

            if (string.IsNullOrEmpty(CaptchaText))
            {
                IsCaptchaError = true;
                if (!HasImage)
                {
                    IsImageError = true;
                }

                Task ignored = LoadCaptcha().ContinueWith(t =>
                {
                    throw t.Exception;
                }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

                return NewThreadFocusResult.Captcha;
            }
            else if (!HasImage)
            {
                IsImageError = true;

                return NewThreadFocusResult.Page;
            }

            Uri uri = new Uri("https://sys.4chan.org/" + Board + "/post");
            Uri referrer = new Uri("http://boards.4chan.org/" + Board);
            Dictionary<string, string> formFields = new Dictionary<string, string>()
            {
                {"MAX_FILE_SIZE", "3145728"},
                {"mode", "regist"},
                {"name", Name},
                {"email", Email},
                {"sub", Subject},
                {"com", Comment},
                {"recaptcha_challenge_field", _token},
                {"recaptcha_response_field", CaptchaText}
            };

            string result;
            try
            {
                HttpResponseMessage message = await RequestManager.Current.PostAsync(uri, referrer, formFields, FileName, _imageData);
                result = await message.Content.ReadAsStringAsync();
            }
            catch
            {
                MessageBox.Show("Unknown error encountered submitting thread.");
                return NewThreadFocusResult.None;
            }

            Match m;
            if (SuccessRegex.IsMatch(result))
            {
                ulong thread = 0;
                if((m = _threadIDRegex.Match(result)).Success)
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

                return NewThreadFocusResult.None;
            }
            else if ((m = ErrorRegex.Match(result)).Success)
            {
                if (m.Groups[1].Value.Contains("You forgot to solve the CAPTCHA"))
                {
                    IsCaptchaError = true;
                    return NewThreadFocusResult.Captcha;
                }
                else if (m.Groups[1].Value.Contains("You seem to have mistyped the CAPTCHA"))
                {
                    CaptchaText = "";
                    IsCaptchaError = true;
                    return NewThreadFocusResult.Captcha;
                }
                else
                {
                    MessageBox.Show(m.Groups[1].Value);
                    return NewThreadFocusResult.None;
                }
            }
            else
            {
                MessageBox.Show("Unknown error encountered submitting thread.");
                return NewThreadFocusResult.None;
            }
        }

        private void DoAddImage()
        {
            PhotoChooserTask task = new PhotoChooserTask() { ShowCamera = true };
            task.Completed += ImageSelected;
            task.Show();
        }

        private async void ImageSelected(object sender, PhotoResult e)
        {
            ((PhotoChooserTask)sender).Completed -= ImageSelected;

            if (e.TaskResult == TaskResult.OK)
            {
                _imageData = new byte[e.ChosenPhoto.Length];
                await e.ChosenPhoto.ReadAsync(_imageData, 0, (int)e.ChosenPhoto.Length);

                HasImage = true;
                FileName = Path.GetFileName(e.OriginalFileName);
            }
            else if (e.TaskResult == TaskResult.Cancel)
            {
                _imageData = null;

                HasImage = false;
                FileName = "choose file";
            }
        }
    }
}
