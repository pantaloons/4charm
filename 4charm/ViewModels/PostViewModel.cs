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
    public class PostViewModel : ViewModelBase
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

        public ICommand Tapped
        {
            get { return GetProperty<ICommand>(); }
            set { SetProperty(value); }
        }

        public ICommand CopyToClipboard
        {
            get { return GetProperty<ICommand>(); }
            set { SetProperty(value); }
        }

        public ICommand ViewQuotes
        {
            get { return GetProperty<ICommand>(); }
            set { SetProperty(value); }
        }

        public ICommand QuoteTapped
        {
            get { return GetProperty<ICommand>(); }
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

        private Post _post;

        private PostViewModel()
        {
        }

        public PostViewModel(Post p)
        {
            _post = p;

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

            RenamedFileName = p.RenamedFileName;
            ImageWidth = p.ImageWidth;
            ImageHeight = p.ImageHeight;

            string posts = p.PostCount != 1 ? "posts" : "post";
            string images = p.ImageCount != 1 ? "images" : "image";
            CounterText = p.PostCount + " " + posts + " and " + p.ImageCount + " " + images + ".";
            TruncatedCounterText = p.PostCount + " / " + p.ImageCount;

            Number = p.Number;
            LongNumber = p.LongNumber;

            HasImage = _post.RenamedFileName != 0;
            if (HasImage)
            {
                ThumbnailSrc = p.ThumbnailSrc;
                ImageSrc = p.ImageSrc;
            }

            ThreadNavigated = new ModelCommand(DoThreadNavigated);
            ImageNavigated = new ModelCommand(DoImageNavigated);
            CopyToClipboard = new ModelCommand(DoCopyToClipboard);
            ViewQuotes = new ModelCommand(DoViewQuotes);
        }

        /// <summary>
        /// User tapped on the post as the initial one in a thread view. Navigat to the threads page.
        /// </summary>
        private void DoThreadNavigated()
        {
            // App.IsPostTapAllowed is used to mark the tap routed event as handled, since tapping post quotes
            // can only be handled by a click event, they can't cancel it normally.
            if (!App.IsPostTapAllowed) return;

            Navigate(new Uri(String.Format("/Views/PostsPage.xaml?board={0}&thread={1}", Uri.EscapeUriString(_post.Thread.Board.Name), Uri.EscapeUriString(Number + "")), UriKind.Relative));
        }

        /// <summary>
        /// User tapped the post image. Navigate to the image viewer.
        /// </summary>
        private void DoImageNavigated()
        {
            // App.IsPostTapAllowed is used to mark the tap routed event as handled, since tapping post quotes
            // can only be handled by a click event, they can't cancel it normally.
            if (!App.IsPostTapAllowed) return;

            Navigate(new Uri(String.Format("/Views/ImageViewer.xaml?board={0}&thread={1}&post={2}&skipped=true",
                Uri.EscapeUriString(_post.Thread.Board.Name), Uri.EscapeUriString(_post.Thread.Number + ""), Uri.EscapeUriString(Number + "")), UriKind.Relative));
        }

        /// <summary>
        /// User copied post text from the context menu.
        /// </summary>
        private void DoCopyToClipboard()
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
        /// <param name="boardID">Board part of the quote hyperlink.</param>
        /// <param name="threadID">Thread ID of the quote hyperlink.</param>
        /// <param name="postID">Post ID of the quote hyperlink.</param>
        public void QuoteLinkTapped(string boardID, ulong threadID, ulong postID)
        {
            if ((boardID == "" || boardID == _post.Thread.Board.Name) && threadID == _post.Thread.Number && QuoteTapped != null)
            {
                // The quote links into the current thread. The view model was constructed with an appropriate handler,
                // since the existing thread view wants to handle this.
                QuoteTapped.Execute(postID);
            }
            else
            {
                string newBoard = boardID;
                // If the board part is empty, assume it is the current board.
                if (newBoard == "") newBoard = _post.Thread.Board.Name;

                if (BoardList.Boards.ContainsKey(newBoard))
                {
                    Navigate(new Uri(String.Format("/Views/PostsPage.xaml?board={0}&thread={1}&post={2}",
                        Uri.EscapeUriString(newBoard), Uri.EscapeUriString(threadID + ""), Uri.EscapeUriString(postID + "")), UriKind.Relative));
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
