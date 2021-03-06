﻿using _4charm.Models;
using _4charm.Resources;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace _4charm.ViewModels
{
    class ImageViewerPageViewModel : PageViewModelBase
    {
        public bool IsLoading
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public ObservableCollection<object> ImagePosts
        {
            get { return GetProperty<ObservableCollection<object>>(); }
            set { SetProperty(value); }
        }

        public int SelectedIndex
        {
            get { return GetProperty<int>(); }
            set
            {
                int oldValue = GetProperty<int>();
                SetProperty(value);
                SelectedIndexChanged(oldValue);
            }
        }

        private Thread _thread;
        private HashSet<ulong> _seenPosts;
        private bool _showLoading;
        private ulong _postID;

        public override void Initialize(IDictionary<string, string> arguments, NavigationEventArgs e)
        {
            string boardName = arguments["board"];
            ulong threadID = ulong.Parse(arguments["thread"]);
            _postID = ulong.Parse(arguments["post"]);
            bool skipped = arguments.ContainsKey("skipped") && arguments["skipped"] == "true";

            _thread = ThreadCache.Current.EnforceBoard(boardName).EnforceThread(threadID);
            _showLoading = skipped && (ulong)_thread.Posts.Where(x => x.Value.RenamedFileName != 0).Count() < _thread.ImageCount + 1;
            _seenPosts = new HashSet<ulong>();

            IEnumerable<Post> _posts = _thread.Posts.Values.Where(x => x.RenamedFileName != 0);
            foreach (Post post in _posts)
            {
                _seenPosts.Add(post.Number);
            }
            ImagePosts = new ObservableCollection<object>(_posts.Select(x => new ImageViewerPostViewModel(x)));

            ScrollToTargetPost();

            Update().ContinueWith(result =>
            {
                throw result.Exception;
            }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

            Thread thread = TransitorySettingsManager.Current.History.FirstOrDefault(x => x.Board.Name == _thread.Board.Name && x.Number == _thread.Number);
            if (thread != null)
            {
                TransitorySettingsManager.Current.History.Remove(thread);
            }
            TransitorySettingsManager.Current.History.Insert(0, _thread);
        }

        private void ScrollToTargetPost()
        {
            for (int i = 0; i < ImagePosts.Count; i++)
            {
                if ((ImagePosts[i] as ImageViewerPostViewModel).Number == _postID)
                {
                    SelectedIndex = i;
                    break;
                }
            }
        }

        public override void SaveState(IDictionary<string, object> state)
        {
            if (SelectedIndex >= 0 && SelectedIndex < ImagePosts.Count)
            {
                state["PostID"] = ((ImageViewerPostViewModel)ImagePosts[SelectedIndex]).Number;
            }
        }

        public override void RestoreState(IDictionary<string, object> state)
        {
            if (state.ContainsKey("PostID"))
            {
                _postID = (ulong)state["PostID"];
                ScrollToTargetPost();
            }
        }

        public async Task Update()
        {
            if (_showLoading)
            {
                IsLoading = true;
            }

            List<Post> posts;
            try
            {
                posts = await _thread.GetPostsAsync();
            }
            catch
            {
                return;
            }
            finally
            {
                IsLoading = false;
            }

            for (int i = 0; i < posts.Count; i++)
            {
                Post post = posts[i];

                if (!_seenPosts.Contains(post.Number) && post.RenamedFileName != 0)
                {
                    _seenPosts.Add(post.Number);
                    ImagePosts.Add(new ImageViewerPostViewModel(post));
                    if (post.Number == _postID)
                    {
                        SelectedIndex = ImagePosts.Count - 1;
                    }
                }
            }
        }

        private void SelectedIndexChanged(int oldValue)
        {
            // Toggles if animated GIF is playing
            if (oldValue >= 0 && oldValue < ImagePosts.Count)
            {
                (ImagePosts[oldValue] as ImageViewerPostViewModel).IsSelected = false;
            }
            if (SelectedIndex >= 0 && SelectedIndex < ImagePosts.Count)
            {
                (ImagePosts[SelectedIndex] as ImageViewerPostViewModel).IsSelected = true;
            }
        }

        public async Task Save()
        {
            if (SelectedIndex >= 0 && SelectedIndex < ImagePosts.Count)
            {
                bool cantSave = false;

                Models.API.APIPost.FileTypes fileType = (ImagePosts[SelectedIndex] as ImageViewerPostViewModel).FileType;
                if (fileType == Models.API.APIPost.FileTypes.webm || fileType == Models.API.APIPost.FileTypes.swf || fileType == Models.API.APIPost.FileTypes.gif)
                {
                    cantSave = true;
                }

                ProgressIndicator progress = new ProgressIndicator
                {
                    IsVisible = true,
                    IsIndeterminate = true,
                    Text = cantSave ? AppResources.ImageViewerPage_CantSave : AppResources.ImageViewerPage_SavingImage
                };

                PhoneApplicationPage page = (App.Current.RootVisual as PhoneApplicationFrame).Content as PhoneApplicationPage;

                SystemTray.SetOpacity(page, 0.99);
                SystemTray.SetIsVisible(page, true);
                SystemTray.SetProgressIndicator(page, progress);

                if (cantSave)
                {
                    await Task.Delay(1000);
                }
                else
                {
                    bool result = await SaveInternal(ImagePosts[SelectedIndex] as ImageViewerPostViewModel);

                    if (result)
                    {
                        progress.Text = AppResources.ImageViewerPage_ImageSaved;
                        await Task.Delay(400);
                    }
                    else
                    {
                        progress.Text = AppResources.ImageViewerPage_ImageFailed;
                        await Task.Delay(1000);
                    }
                }

                SystemTray.SetProgressIndicator(page, null);
                SystemTray.SetIsVisible(page, false);
            }
        }

        private async Task<bool> SaveInternal(ImageViewerPostViewModel item)
        {
            Stream responseStream;
            try
            {
                HttpResponseMessage response = await RequestManager.Current.GetAsync(item.ImageSrc);
                responseStream = await response.Content.ReadAsStreamAsync();
            }
            catch
            {
                return false;
            }

            BitmapImage bi = new BitmapImage() { CreateOptions = BitmapCreateOptions.None };
            try
            {
                bi.SetSource(responseStream);
            }
            catch (Exception ex)
            {
                if (ex.HResult == -2146233088)
                {
                    // Image unrecognized
                    return false;
                }
                else
                {
                    throw;
                }
            }

            WriteableBitmap wbmp = new WriteableBitmap(bi);
                
            using (MemoryStream ms = new MemoryStream())
            {
                try
                {
                    wbmp.SaveJpeg(ms, bi.PixelWidth, bi.PixelHeight, 0, 95);
                    ms.Seek(0, SeekOrigin.Begin);
                    new MediaLibrary().SavePicture(item.RenamedFileName + "", ms);
                }
                catch
                {
                    return false;
                }
                finally
                {
                    bi.UriSource = null;
                    bi = null;
                    wbmp = null;
                }
            }

            return true;
        }
    }
}
