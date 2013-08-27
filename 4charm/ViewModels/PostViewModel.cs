using _4charm.Models;
using _4charm.Models.API;
using HtmlAgilityPack;
using Microsoft.Phone.Controls;
using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace _4charm.ViewModels
{
    public class PostViewModel : ViewModelBase, IThumbnailedImage, IDisplayableImage, IComparable<PostViewModel>
    {
        public BitmapImage Image
        {
            get { return GetProperty<BitmapImage>(); }
            set { SetProperty(value); }
        }

        public bool IsWatchlisted
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }
        public bool IsSticky
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }
        public bool IsClosed
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }
        public bool IsMod
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }
        public bool IsAdmin
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }
        public bool IsDeveloper
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }
        public bool FileDeleted
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }
        public bool HasImage
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public string Subject
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }
        public string AuthorName
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }
        public string SimpleComment
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }
        public HtmlDocument HtmlComment
        {
            get { return GetProperty<HtmlDocument>(); }
            set { SetProperty(value); }
        }
        public string PrettyTime
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }
        public string CounterText
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }
        public string TruncatedCounterText
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public APIPost.CapCodes CapCode
        {
            get { return GetProperty<APIPost.CapCodes>(); }
            set { SetProperty(value); }
        }

        public double ThumbWidth
        {
            get { return GetProperty<double>(); }
            set { SetProperty(value); }
        }
        public double ThumbHeight
        {
            get { return GetProperty<double>(); }
            set { SetProperty(value); }
        }
        public Thickness ThumbMargin
        {
            get { return GetProperty<Thickness>(); }
            set { SetProperty(value); }
        }
        public Thickness ThumbMargin2
        {
            get { return GetProperty<Thickness>(); }
            set { SetProperty(value); }
        }

        public ulong RenamedFileName
        {
            get { return GetProperty<ulong>(); }
            set { SetProperty(value); }
        }
        public uint ImageWidth
        {
            get { return GetProperty<uint>(); }
            set { SetProperty(value); }
        }
        public uint ImageHeight
        {
            get { return GetProperty<uint>(); }
            set { SetProperty(value); }
        }

        public Brush Background
        {
            get { return _post.Background; }
        }

        public ulong Number
        {
            get { return GetProperty<ulong>(); }
            set { SetProperty(value); }
        }
        public string LongNumber
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public ICommand ThreadNavigated
        {
            get { return GetProperty<ICommand>(); }
            set { SetProperty(value); }
        }

        public ICommand ImageNavigated
        {
            get { return GetProperty<ICommand>(); }
            set { SetProperty(value); }
        }

        public ICommand NumberTapped
        {
            get { return GetProperty<ICommand>(); }
            set { SetProperty(value); }
        }

        public ICommand TextCopied
        {
            get { return GetProperty<ICommand>(); }
            set { SetProperty(value); }
        }

        public ICommand ViewQuotes
        {
            get { return GetProperty<ICommand>(); }
            set { SetProperty(value); }
        }

        public bool IsGIF
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public Uri ThumbnailSrc
        {
            get { return GetProperty<Uri>(); }
            set { SetProperty(value); }
        }

        public Uri ImageSrc
        {
            get { return GetProperty<Uri>(); }
            set { SetProperty(value); }
        }

        internal Post _post;
        private Action<ulong> _quoteTapped;

        public PostViewModel(Post p, Action<ulong> quoteTapped)
        {
            _post = p;
            _quoteTapped = quoteTapped;

            IsSticky = p.IsSticky;
            IsClosed = p.IsClosed;
            IsMod = p.IsMod;
            IsAdmin = p.IsAdmin;
            IsDeveloper = p.IsDeveloper;
            FileDeleted = p.FileDeleted;

            Subject = p.Subject;
            AuthorName = p.DisplayName;
            HtmlComment = new HtmlDocument();
            if (p.Comment != null) HtmlComment.LoadHtml(p.Comment.Replace("<wbr>", ""));
            SimpleComment = WebUtility.HtmlDecode(HtmlComment.DocumentNode.InnerText).Replace("&#039;", "'").Replace("&#44;", ",");
            PrettyTime = p.PrettyTime;

            CapCode = p.CapCode;

            ThumbHeight = p.ThumbHeight;
            ThumbWidth = p.RenamedFileName != 0 ? 100 : 0;
            ThumbMargin = p.RenamedFileName != 0 ? new Thickness(0, 0, 12, 0) : new Thickness(0);
            ThumbMargin2 = p.RenamedFileName != 0 ? new Thickness(0, 6, 12, 6) : new Thickness(0);

            RenamedFileName = p.RenamedFileName;
            ImageWidth = p.ImageWidth;
            ImageHeight = p.ImageHeight;

            IsGIF = p.FileType == APIPost.FileTypes.gif;
            ThumbnailSrc = p.ThumbnailSrc;
            ImageSrc = p.ImageSrc;

            string posts = p.PostCount != 1 ? "posts" : "post";
            string images = p.ImageCount != 1 ? "images" : "image";
            CounterText = p.PostCount + " " + posts + " and " + p.ImageCount + " " + images + ".";
            TruncatedCounterText = p.PostCount + " / " + p.ImageCount;

            Number = p.Number;
            LongNumber = p.LongNumber;

            HasImage = _post.RenamedFileName != 0;

            ThreadNavigated = new ModelCommand(DoThreadNavigated);
            ImageNavigated = new ModelCommand(DoImageNavigated);
            NumberTapped = new ModelCommand(DoNumberTapped);
            TextCopied = new ModelCommand(DoTextCopied);
            ViewQuotes = new ModelCommand(DoViewQuotes);
        }

        ~PostViewModel()
        {
            //if (Image != null)
            //{
            //    System.Diagnostics.Debugger.Break();
            //    throw new System.Exception();
            //}
        }

        private void DoThreadNavigated()
        {
            if (!App.RootFrame.IsHitTestVisible) return;

            Navigate(new Uri(String.Format("/Views/PostsPage.xaml?board={0}&thread={1}", Uri.EscapeUriString(_post.Thread.Board.Name), Uri.EscapeDataString(Number + "")), UriKind.Relative));
        }

        private void DoImageNavigated()
        {
            if (!App.RootFrame.IsHitTestVisible) return;

            Navigate(new Uri(String.Format("/Views/ImageViewer.xaml?board={0}&thread={1}&post={2}&skipped=true",
                Uri.EscapeUriString(_post.Thread.Board.Name), Uri.EscapeDataString(_post.Thread.Number + ""), Uri.EscapeDataString(Number + "")), UriKind.Relative));
        }

        private void DoNumberTapped()
        {
            if (_quoteTapped != null) _quoteTapped(Number);
        }

        private void DoTextCopied()
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml((_post.Comment ?? "").Replace("<br>", "\n"));
            Clipboard.SetText(doc.DocumentNode.InnerText);
        }

        private void DoViewQuotes()
        {
            MessageBox.Show("Tap the post number in the top right corner to view quotes.");
        }

        private BitmapImage _loading;
        public void LoadImage(int displayWidth = 100)
        {
            if (_post.RenamedFileName == 0 || _post.FileDeleted || _loading != null) return;

            //if (_loading != null) throw new Exception();
            _loading = new BitmapImage() { DecodePixelWidth = displayWidth };
            _loading.ImageOpened += ImageLoaded;
            _loading.CreateOptions = BitmapCreateOptions.BackgroundCreation;
            _loading.UriSource = _post.ThumbnailSrc;
        }

        private void ImageLoaded(object sender, RoutedEventArgs e)
        {
            Image = _loading;
        }

        public void UnloadImage()
        {
            if (_loading != null)
            {
                _loading.ImageOpened -= ImageLoaded;
                _loading.UriSource = null;
                try
                {
                    using (var ms = new MemoryStream(new byte[] { 0x0 }))
                    {
                        _loading.SetSource(ms);
                    }
                }
                catch
                {
                }
                _loading = null;
            }

            if (Image != null)
            {
                try
                {
                    using (var ms = new MemoryStream(new byte[] { 0x0 }))
                    {
                        Image.SetSource(ms);
                    }
                }
                catch
                {
                }
                Image.UriSource = null;
                Image = null;
            }
        }

        public bool QuotesPost(ulong post)
        {
            return _post.Quotes.Contains(post);
        }

        public int CompareTo(PostViewModel other)
        {
            if (_post.Thread.Board.Name == other._post.Thread.Board.Name) return Number.CompareTo(other.Number);
            else return _post.Thread.Board.Name.CompareTo(other._post.Thread.Board.Name);
        }

        public void QuoteLinkTapped(string board, ulong newThread, ulong post)
        {
            if ((board == "" || board == _post.Thread.Board.Name) && newThread == _post.Thread.Number)
            {
                if (_quoteTapped != null) _quoteTapped(post);
            }
            else
            {
                string newBoard = board;
                if (newBoard == "") newBoard = _post.Thread.Board.Name;
                if (BoardList.Boards.ContainsKey(newBoard))
                {
                    Navigate(new Uri(String.Format("/Views/PostsPage.xaml?board={0}&thread={1}&post={2}", newBoard, newThread, post), UriKind.Relative));
                }
            }
        }

        public void BoardLinkTapped(string name)
        {
            if (BoardList.Boards.ContainsKey(name))
            {
                Navigate(new Uri(String.Format("/Views/ThreadsPage.xaml?board={0}", Uri.EscapeUriString(name)), UriKind.Relative));
            }
            else
            {
                MessageBox.Show("Cannot navigate to board /" + name + "/ as it does not exist.");
            }
        }
    }
}
