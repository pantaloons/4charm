using _4charm.Models;
using _4charm.Models.API;
using HtmlAgilityPack;
using Microsoft.Phone.Controls;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace _4charm.ViewModels
{
    public class PostViewModel : ImageViewModelBase
    {
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

        public PostViewModel()
        {
        }

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

            if (Subject != null)
            {
                SimpleComment = Subject;
            }
            else
            {
                SimpleComment = WebUtility.HtmlDecode(HtmlComment.DocumentNode.InnerText).Replace("&#039;", "'").Replace("&#44;", ",");
            }
            
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

        /// <summary>
        /// User tapped on the post as the initial one in a thread view. Navigat to the threads page.
        /// </summary>
        private void DoThreadNavigated()
        {
            // App.RootFrame.IsHitTestVisible is used to mark the tap routed event as handled, since tapping post quotes
            // can only be handled by a click event, they can't cancel it normally.
            if (!App.RootFrame.IsHitTestVisible) return;

            Navigate(new Uri(String.Format("/Views/PostsPage.xaml?board={0}&thread={1}", Uri.EscapeUriString(_post.Thread.Board.Name), Uri.EscapeUriString(Number + "")), UriKind.Relative));
        }

        /// <summary>
        /// User tapped the post image. Navigate to the image viewer.
        /// </summary>
        private void DoImageNavigated()
        {
            // App.RootFrame.IsHitTestVisible is used to mark the tap routed event as handled, since tapping post quotes
            // can only be handled by a click event, they can't cancel it normally.
            if (!App.RootFrame.IsHitTestVisible) return;

            Navigate(new Uri(String.Format("/Views/ImageViewer.xaml?board={0}&thread={1}&post={2}&skipped=true",
                Uri.EscapeUriString(_post.Thread.Board.Name), Uri.EscapeUriString(_post.Thread.Number + ""), Uri.EscapeUriString(Number + "")), UriKind.Relative));
        }

        /// <summary>
        /// User tapped the post number. View determines how to handle this, usually does nothing but in the posts view
        /// the replies area is shown for this post.
        /// </summary>
        private void DoNumberTapped()
        {
            if (_quoteTapped != null) _quoteTapped(Number);
        }

        /// <summary>
        /// User copied post text from the context menu.
        /// </summary>
        private void DoTextCopied()
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml((_post.Comment ?? "").Replace("<br>", "\n"));
            Clipboard.SetText(doc.DocumentNode.InnerText);
        }

        /// <summary>
        /// User tried to show quotes from the context menu.
        /// </summary>
        private void DoViewQuotes()
        {
            // Just inform them of a better way to do it, for discoverability.
            MessageBox.Show("Tap the post number in the top right corner to view quotes.");
        }

        public override void LoadImage(int displayWidth = 100)
        {
            if (_post.RenamedFileName == 0 || _post.FileDeleted) return;
            base.LoadImage(_post.ThumbnailSrc, displayWidth);
        }

        /// <summary>
        /// If this post quotes the given post.
        /// </summary>
        /// <param name="post">The post to check if it is quoted.</param>
        /// <returns>True if this post quotes the parameter.</returns>
        public bool QuotesPost(ulong post)
        {
            return _post.Quotes.Contains(post);
        }

        /// <summary>
        /// User tapped a quote on the post. Show the reply box or navigate as appropriate.
        /// </summary>
        /// <param name="board">Board part of the quote hyperlink.</param>
        /// <param name="newThread">Thread ID of the quote hyperlink.</param>
        /// <param name="post">Post ID of the quote hyperlink.</param>
        public void QuoteLinkTapped(string board, ulong newThread, ulong post)
        {
            if ((board == "" || board == _post.Thread.Board.Name) && newThread == _post.Thread.Number)
            {
                // The quote links into the current thread. The view model was constructed with an appropriate handler,
                // since the existing thread view wants to handle this.
                if (_quoteTapped != null) _quoteTapped(post);
            }
            else
            {
                string newBoard = board;
                // If the board part is empty, assume it is the current board.
                if (newBoard == "") newBoard = _post.Thread.Board.Name;

                if (BoardList.Boards.ContainsKey(newBoard))
                {
                    Navigate(new Uri(String.Format("/Views/PostsPage.xaml?board={0}&thread={1}&post={2}",
                        Uri.EscapeUriString(newBoard), Uri.EscapeUriString(newThread + ""), Uri.EscapeUriString(post + "")), UriKind.Relative));
                }
            }
        }

        /// <summary>
        /// User tapped a link to another board.
        /// </summary>
        /// <param name="name">The name of the board.</param>
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
