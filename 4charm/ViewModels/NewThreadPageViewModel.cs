using _4charm.Models;
using _4charm.Models.API;
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
using System.Windows.Media.Imaging;

namespace _4charm.ViewModels
{
    class NewThreadPageViewModel : ViewModelBase
    {
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

        public BitmapImage CaptchaImage
        {
            get { return GetProperty<BitmapImage>(); }
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

        private Action _imageChanged;

        private string _token;
        private byte[] _imageData;

        public NewThreadPageViewModel(Action imageChanged)
        {
            _imageChanged = imageChanged;

            ReloadCaptcha = new ModelCommand(() => Load());
            AddImage = new ModelCommand(() => DoAddImage());
            MoreDetails = new ModelCommand(() => HasMoreDetails = true);

            Comment = "";
            CaptchaText = "";
            Subject = "";
            Name = "";
            Email = "";
        }

        public void OnNavigatedTo(string boardName)
        {
            Board = boardName;
            ThreadIDRegex = new Regex("<meta http-equiv=\"refresh\" content=\"1;URL=http://boards\\.4chan\\.org/" + Board + "/res/(\\d+)\">");

            PageTitle = "NEW THREAD - /" + boardName + "/";
            FileName = "choose file";

            Load();
        }

        public async void Load()
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

            UnloadImage();
            LoadImage();
        }

        public async Task<string> GetCaptcha()
        {
            string page = await RequestManager.Current.GetStringAsync(new Uri("http://www.google.com/recaptcha/api/challenge?k=6Ldp2bsSAAAAAAJ5uyx_lx34lJeEpTLVkP5k04qc"));

            int start = page.IndexOf("{");
            int end = page.LastIndexOf("}");

            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(page.Substring(start, end - start + 1))))
            {
                DataContractJsonSerializer dcjs = new DataContractJsonSerializer(typeof(APICaptcha));

                APICaptcha captcha = dcjs.ReadObject(ms) as APICaptcha;

                return captcha.Challenge;
            }
        }

        public async Task<SubmitResult> Submit()
        {
            IsPosting = true;
            SubmitResult result = await SubmitInternal();
            IsPosting = false;
            return result;
        }

        private static Regex ErrorRegex = new Regex("<span id=\"errmsg\" style=\"color: red;\">(Error: [^<]+)<");
        private static Regex SuccessRegex = new Regex("<title>Post successful!</title>");
        private Regex ThreadIDRegex;
        private async Task<SubmitResult> SubmitInternal()
        {
            if (string.IsNullOrEmpty(CaptchaText)) return new SubmitResult() { ResultType = SubmitResultType.EmptyCaptchaError };
            else if (!HasImage) return new SubmitResult() { ResultType = SubmitResultType.NoImageError };

            Uri uri = new Uri("https://sys.4chan.org/" + Board + "/post");
            Uri referrer = new Uri("http://boards.4chan.org/" + Board);

            try
            {
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

                HttpResponseMessage message = await RequestManager.Current.PostAsync(uri, referrer, formFields, FileName, _imageData);

                string result = await message.Content.ReadAsStringAsync();
                Match m;
                if (SuccessRegex.IsMatch(result))
                {
                    ulong thread = 0;
                    if((m = ThreadIDRegex.Match(result)).Success)
                    {
                        ulong.TryParse(m.Groups[1].Value, out thread);
                    }
                    return new SubmitResult() { ResultType = SubmitResultType.Success, ThreadID = thread };
                }
                else if ((m = ErrorRegex.Match(result)).Success)
                {
                    if (m.Groups[1].Value.Contains("You forgot to solve the CAPTCHA"))
                    {
                        return new SubmitResult() { ResultType = SubmitResultType.EmptyCaptchaError };
                    }
                    else if (m.Groups[1].Value.Contains("You seem to have mistyped the CAPTCHA"))
                    {
                        return new SubmitResult() { ResultType = SubmitResultType.WrongCatpchaError };
                    }
                    else
                    {
                        return new SubmitResult() { ResultType = SubmitResultType.KnownError, ErrorMessage = m.Groups[1].Value };
                    }
                }
                else
                {
                    return new SubmitResult() { ResultType = SubmitResultType.UnknownError };
                }
            }
            catch
            {
                return new SubmitResult() { ResultType = SubmitResultType.UnknownError };
            }
        }

        private BitmapImage _loading;
        public void LoadImage()
        {
            //if (_loading != null) throw new Exception();
            _loading = new BitmapImage();
            _loading.ImageOpened += ImageLoaded;
            _loading.CreateOptions = BitmapCreateOptions.BackgroundCreation;
            _loading.UriSource = RequestManager.Current.EnforceHTTPS(new Uri("http://www.google.com/recaptcha/api/image?c=" + _token));
        }

        private void ImageLoaded(object sender, RoutedEventArgs e)
        {
            _loading.ImageOpened -= ImageLoaded;
            CaptchaImage = _loading;
        }

        public void UnloadImage()
        {
            if (_loading != null)
            {
                _loading.ImageOpened -= ImageLoaded;
                _loading.UriSource = null;
                _loading = null;
            }

            if (CaptchaImage != null)
            {
                CaptchaImage.UriSource = null;
                CaptchaImage = null;
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

            _imageChanged();
        }
    }
}
