using _4charm.Models;
using _4charm.Models.API;
using Microsoft.Phone.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace _4charm.ViewModels
{
    enum SubmitResultType
    {
        Success,
        EmptyCaptchaError,
        WrongCatpchaError,
        EmptyCommentError,
        NoImageError,
        KnownError,
        UnknownError
    };

    class SubmitResult
    {
        public SubmitResultType ResultType { get; set; }
        public string ErrorMessage { get; set; }
        public ulong ThreadID { get; set; }
    };

    class ReplyViewModel : ViewModelBase
    {
        public bool IsPosting
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public Brush Background
        {
            get { return _thread.Board.Brush; }
        }

        public Brush ReplyBackBrush
        {
            get { return _thread.Board.ReplyBackBrush; }
        }

        public Brush ReplyForeBrush
        {
            get { return _thread.Board.ReplyForeBrush; }
        }

        public string Comment
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public string CaptchaText
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public string Name
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public string Email
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public string Subject
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public string FileName
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public bool HasImage
        {
            get { return GetProperty<bool>(); }
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

        public ICommand SelectImage
        {
            get { return GetProperty<ICommand>(); }
            set { SetProperty(value); }
        }

        private Thread _thread;
        private string _token;
        private byte[] _imageData;

        private static Regex ErrorRegex = new Regex("<span id=\"errmsg\" style=\"color: red;\">(Error: [^<]+)<");
        private static Regex SuccessRegex = new Regex("<title>Post successful!</title>");

        public ReplyViewModel(Thread thread)
        {
            _thread = thread;

            ReloadCaptcha = new ModelCommand(DoReloadCaptcha);
            SelectImage = new ModelCommand(DoSelectImage);

            CaptchaText = "";
            Comment = "";
            Name = "";
            Email = "";
            Subject = "";
        }

        private void DoReloadCaptcha()
        {
            Load();
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
                FileName = "";
            }
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
            
            using(MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(page.Substring(start, end - start + 1))))
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

        private async Task<SubmitResult> SubmitInternal()
        {
            if (string.IsNullOrEmpty(Comment) && !HasImage) return new SubmitResult() { ResultType = SubmitResultType.EmptyCommentError };
            else if (string.IsNullOrEmpty(CaptchaText)) return new SubmitResult() { ResultType = SubmitResultType.EmptyCaptchaError };

            Uri uri = new Uri("https://sys.4chan.org/" + _thread.Board.Name +"/post");
            Uri referrer = new Uri("http://boards.4chan.org/" + _thread.Board.Name + "/res/" + _thread.Number);
            try
            {
                Dictionary<string, string> formFields = new Dictionary<string, string>()
                {
                    {"MAX_FILE_SIZE", "3145728"},
                    {"mode", "regist"},
                    {"resto", _thread.Number + ""},
                    {"name", Name},
                    {"email", Email},
                    {"sub", Subject},
                    {"com", Comment},
                    {"recaptcha_challenge_field", _token},
                    {"recaptcha_response_field", CaptchaText}
                };

                HttpResponseMessage message;
                if (HasImage)
                {
                    message = await RequestManager.Current.PostAsync(uri, referrer, formFields, FileName, _imageData);
                }
                else
                {
                    message = await RequestManager.Current.PostAsync(uri, referrer, formFields);
                }

                string result = await message.Content.ReadAsStringAsync();
                Match m;
                if (SuccessRegex.IsMatch(result))
                {
                    ResetFields();
                    return new SubmitResult() { ResultType = SubmitResultType.Success };
                }
                else if ((m = ErrorRegex.Match(result)).Success)
                {
                    if (m.Groups[1].Value.Contains("You forgot to solve the CAPTCHA"))
                    {
                        return new SubmitResult() { ResultType = SubmitResultType.EmptyCaptchaError };
                    }
                    else if(m.Groups[1].Value.Contains("You seem to have mistyped the CAPTCHA"))
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

        private void ResetFields()
        {
            Comment = "";
            CaptchaText = "";
            Name = "";
            Email = "";
            Subject = "";
            FileName = "";
            HasImage = false;
        }

        private BitmapImage _loading;
        public void LoadImage()
        {
            //if (_loading != null) throw new Exception();
            _loading = new BitmapImage() { DecodePixelWidth = 480 };
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
    }
}
