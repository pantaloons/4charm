using _4charm.Models;
using _4charm.Models.API;
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
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace _4charm.ViewModels
{
    class ReplyViewModel : ViewModelBase
    {
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

        private Thread _thread;
        private string _token;

        private static Regex ErrorRegex = new Regex("<span id=\"errmsg\" style=\"color: red;\">(Error: [^<]+)<");
        private static Regex SuccessRegex = new Regex("<title>Post successful!</title>");

        public ReplyViewModel(Thread thread)
        {
            _thread = thread;

            ReloadCaptcha = new ModelCommand(DoReloadCaptcha);
            CaptchaText = "";
            Comment = "";
        }

        private void DoReloadCaptcha()
        {
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
            }
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

        public async Task<bool> Submit()
        {
            Uri uri = new Uri("https://sys.4chan.org/" + _thread.Board.Name +"/post");
            Uri referrer = new Uri("http://boards.4chan.org/" + _thread.Board.Name + "/res/" + _thread.Number);
            try
            {
                HttpResponseMessage message = await RequestManager.Current.PostAsync(uri, referrer, new Dictionary<string, string>()
                {
                    {"MAX_FILE_SIZE", "3145728"},
                    {"mode", "regist"},
                    {"resto", _thread.Number + ""},
                    {"name", ""},
                    {"email", ""},
                    {"sub", ""},
                    {"com", Comment},
                    {"recaptcha_challenge_field", _token},
                    {"recaptcha_response_field", CaptchaText},
                    {"pwd", "11111111"}
                });

                string result = await message.Content.ReadAsStringAsync();
                Match m;
                if (SuccessRegex.IsMatch(result))
                {
                    return true;
                }
                else if ((m = ErrorRegex.Match(result)).Success)
                {
                    MessageBox.Show("Error: " + m.Groups[1].Value);
                    return false;
                }
                else
                {
                    throw new Exception();
                }
            }
            catch
            {
                // TODO: Remove
                MessageBox.Show("Error posting reply.");
                System.Diagnostics.Debugger.Break();
                return false;
            }
        }

        private BitmapImage _loading;
        public void LoadImage()
        {
            if (_loading != null) throw new Exception();
            _loading = new BitmapImage() { DecodePixelWidth = 100 };
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
