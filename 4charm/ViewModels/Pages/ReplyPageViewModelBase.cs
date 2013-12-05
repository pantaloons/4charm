using _4charm.Models;
using _4charm.Models.API;
using _4charm.Resources;
using Microsoft.Phone.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public abstract class ReplyPageViewModelBase : PageViewModelBase
    {
        public enum SubmitResultType
        {
            Success,
            EmptyCaptchaError,
            WrongCatpchaError,
            EmptyCommentError,
            NoImageError,
            KnownError,
            UnknownError
        };

        private static readonly Regex ErrorRegex = new Regex("<span id=\"errmsg\" style=\"color: red;\">(Error: [^<]+)<");
        private static readonly Regex SuccessRegex = new Regex("<title>Post successful!</title>");

        public bool IsPosting
        {
            get { return GetProperty<bool>(); }
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

        public string Board
        {
            get { return GetProperty<string>(); }
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

        public bool IsCommentError
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public ICommand ReloadCaptcha
        {
            get { return GetProperty<ICommand>(); }
            set { SetProperty(value); }
        }

        public ICommand SelectImage
        {
            get { return GetProperty<ICommand>(); }
            set { SetProperty(value); }
        }

        private ulong _post;
        private bool _isNewThread;
        private string _token;
        private byte[] _imageData;

        public ReplyPageViewModelBase(bool isNewThread)
        {
            _isNewThread = isNewThread;
            ReloadCaptcha = new ModelCommand(async () => await DoLoadCaptcha());
            SelectImage = new ModelCommand(() => DoSelectImage());
        }

        public override void Initialize(IDictionary<string, string> arguments, NavigationEventArgs e)
        {
            base.Initialize(arguments, e);

            Comment = "";
            CaptchaText = "";
            Subject = "";
            Name = "";
            Email = "";

            Board = arguments["board"];

            FileName = AppResources.NewThreadPage_ChooseFile;

            DoLoadCaptcha().ContinueWith(result =>
            {
                throw result.Exception;
            }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }

        public override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);

            if (IsPosting)
            {
                e.Cancel = true;
            }
        }

        public async Task DoLoadCaptcha()
        {
            try
            {
                _token = await GetCaptcha();
            }
            catch
            {
                MessageBox.Show(AppResources.NewThreadPage_CaptchaCorrupt);
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

        private void DoSelectImage()
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
                FileName = AppResources.NewThreadPage_ChooseFile;
            }
        }

        protected async Task<SubmitResultType> SubmitInternal(ulong threadID = 0)
        {
            IsPosting = true;
            SubmitResultType result = await SubmitInternalAsync(threadID);
            IsPosting = false;
            return result;
        }

        private async Task<SubmitResultType> SubmitInternalAsync(ulong threadID)
        {
            IsCaptchaError = false;
            IsImageError = false;
            IsCommentError = false;

            if (!_isNewThread && string.IsNullOrEmpty(Comment) && !HasImage)
            {
                // Posts into an existing thread require either an image or comment
                IsCommentError = true;
                if (string.IsNullOrEmpty(CaptchaText))
                {
                    IsCaptchaError = true;
                }
                return SubmitResultType.EmptyCommentError;
            }

            if (string.IsNullOrEmpty(CaptchaText))
            {
                IsCaptchaError = true;
                if (_isNewThread && !HasImage)
                {
                    IsImageError = true;
                }

                Task ignored = DoLoadCaptcha().ContinueWith(t =>
                {
                    throw t.Exception;
                }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

                return SubmitResultType.EmptyCaptchaError;
            }
            else if (_isNewThread && !HasImage)
            {
                IsImageError = true;

                return SubmitResultType.NoImageError;
            }

            Uri uri = new Uri("https://sys.4chan.org/" + Board + "/post");
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

            if (_isNewThread)
            {
                formFields["resto"] = threadID + "";
            }

            string result;
            try
            {
                HttpResponseMessage message;
                if (HasImage)
                {
                    message = await RequestManager.Current.PostAsync(uri, formFields, FileName, _imageData);
                }
                else
                {
                    message = await RequestManager.Current.PostAsync(uri, formFields);
                }

                result = await message.Content.ReadAsStringAsync();
            }
            catch
            {
                MessageBox.Show(AppResources.NewThreadPage_UnknownError);
                return SubmitResultType.UnknownError;
            }

            Match m;
            if (SuccessRegex.IsMatch(result))
            {
                OnSubmitSuccess(result);
                return SubmitResultType.Success;
            }
            else if ((m = ErrorRegex.Match(result)).Success)
            {
                if (m.Groups[1].Value.Contains("You forgot to solve the CAPTCHA"))
                {
                    IsCaptchaError = true;
                    return SubmitResultType.EmptyCaptchaError;
                }
                else if (m.Groups[1].Value.Contains("You seem to have mistyped the CAPTCHA"))
                {
                    CaptchaText = "";
                    IsCaptchaError = true;
                    return SubmitResultType.WrongCatpchaError;
                }
                else
                {
                    MessageBox.Show(m.Groups[1].Value);
                    return SubmitResultType.KnownError;
                }
            }
            else
            {
                MessageBox.Show(AppResources.NewThreadPage_UnknownError);
                return SubmitResultType.UnknownError;
            }
        }

        protected abstract void OnSubmitSuccess(string result);
    }
}
