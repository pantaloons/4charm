using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using _4charm.Models;
using _4charm.Resources;
using _4charm.ViewModels;
using Microsoft.Xna.Framework.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace _4charm.Views
{
    public partial class ImageViewer : PhoneApplicationPage
    {
        private ImageViewerPageViewModel _viewModel;

        private ApplicationBarIconButton _saveImage;
        private ApplicationBarMenuItem _orientLock;
        
        public ImageViewer()
        {
            InitializeComponent();
            InitializeApplicationBar();

            _viewModel = new ImageViewerPageViewModel();
            DataContext = _viewModel;

            Loaded += ImageViewerLoaded;
        }

        private void InitializeApplicationBar()
        {
            _saveImage = new ApplicationBarIconButton(new Uri("Assets/Appbar/appbar.download.png", UriKind.Relative)) { Text = AppResources.ApplicationBar_Save };
            _saveImage.Click += (sender, e) =>
            {
                //int index = MediaViewer.DisplayedItemIndex;
                //if (index >= 0 && _viewModel.ImagePosts.Count > index)
                //{
                //    BitmapImage bi = new BitmapImage() { CreateOptions = BitmapCreateOptions.BackgroundCreation };
                //    EventHandler<RoutedEventArgs> opened = null;
                //    EventHandler<ExceptionRoutedEventArgs> failed = null;

                //    ProgressIndicator progress = new ProgressIndicator
                //    {
                //        IsVisible = true,
                //        IsIndeterminate = true,
                //        Text = "Saving to Saved Pictures..."
                //    };

                //    SystemTray.SetOpacity(this, 0.99);
                //    SystemTray.SetIsVisible(this, true);
                //    SystemTray.SetProgressIndicator(this, progress);

                //    opened = async (ssender, se) =>
                //    {
                //        (ssender as BitmapImage).ImageOpened -= opened;
                //        (ssender as BitmapImage).ImageFailed -= failed;

                //        try
                //        {
                //            DownloadImageOpened(ssender as BitmapImage, _viewModel.ImagePosts[index]);
                //        }
                //        catch
                //        {
                //            failed(ssender, null);
                //            return;
                //        }

                //        progress.Text = "Done.";
                //        await Task.Delay(400);
                //        SystemTray.SetProgressIndicator(this, null);
                //        SystemTray.SetIsVisible(this, false);
                //    };

                //    failed = async (ssender, se) =>
                //    {
                //        (ssender as BitmapImage).ImageOpened -= opened;
                //        (ssender as BitmapImage).ImageFailed -= failed;

                //        progress.Text = "Image download failed.";
                //        await Task.Delay(1000);
                //        SystemTray.SetProgressIndicator(this, null);
                //        SystemTray.SetIsVisible(this, false);
                //    };

                //    bi.ImageOpened += opened;
                //    bi.ImageFailed += failed;
                //    bi.UriSource = _viewModel.ImagePosts[index].ImageSrc;
                //}
            };

            _orientLock = new ApplicationBarMenuItem(AppResources.ApplicationBar_LockOrientation);
            _orientLock.Click += (sender, e) =>
            {
                if (CriticalSettingsManager.Current.LockOrientation == SupportedPageOrientation.PortraitOrLandscape)
                {
                    bool isPortrait =
                        Orientation == PageOrientation.Portrait || Orientation == PageOrientation.PortraitDown ||
                        Orientation == PageOrientation.PortraitUp;

                    CriticalSettingsManager.Current.LockOrientation = isPortrait ? SupportedPageOrientation.Portrait : SupportedPageOrientation.Landscape;
                }
                else
                {
                    CriticalSettingsManager.Current.LockOrientation = SupportedPageOrientation.PortraitOrLandscape;
                }
                OrientationLockChanged();
            };

            ApplicationBar = new ApplicationBar();
            ApplicationBar.Opacity = 0.0;
            ApplicationBar.Buttons.Add(_saveImage);
            ApplicationBar.MenuItems.Add(_orientLock);
        }

        private ulong _postID;

        private bool _initialized;
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (!_initialized)
            {
                string boardName = NavigationContext.QueryString["board"];
                ulong threadID = ulong.Parse(NavigationContext.QueryString["thread"]);
                _postID = ulong.Parse(NavigationContext.QueryString["post"]);

                bool skipped = NavigationContext.QueryString.ContainsKey("skipped") && NavigationContext.QueryString["skipped"] == "true";

                Thread t = ThreadCache.Current.EnforceBoard(boardName).EnforceThread(threadID);

                _viewModel.OnNavigatedTo(boardName, threadID, skipped);

                ThreadViewModel tvm = TransitorySettingsManager.Current.History.FirstOrDefault(x => x.BoardName == t.Board.Name && x.Number == t.Number);
                if (tvm != null)
                {
                    TransitorySettingsManager.Current.History.Remove(tvm);
                    TransitorySettingsManager.Current.History.Insert(0, tvm);
                }

                _initialized = true;
            }

            OrientationLockChanged();
            ApplicationBar.StateChanged += AppplicationBar_Opened;
        }

        private void ImageViewerLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= ImageViewerLoaded;

            //PostViewModel pvm = _viewModel.ImagePosts.FirstOrDefault(x => x.Number == _postID);
            //if (pvm != null)
            //{
            //    try
            //    {
            //        //MediaViewer.JumpToItem(_viewModel.ImagePosts.IndexOf(pvm));
            //    }
            //    catch
            //    {
            //    }
            //}
        }

        protected override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);

            //MediaViewer.Unload();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            ApplicationBar.StateChanged -= AppplicationBar_Opened;
        }

        private void AppplicationBar_Opened(object sender, ApplicationBarStateChangedEventArgs e)
        {
            if (e.IsMenuVisible) ApplicationBar.Opacity = 0.99;
            else ApplicationBar.Opacity = 0.0;
        }

        private void OrientationLockChanged()
        {
            this.SupportedOrientations = CriticalSettingsManager.Current.LockOrientation;
            if (this.SupportedOrientations == SupportedPageOrientation.PortraitOrLandscape)
            {
                _orientLock.Text = AppResources.ApplicationBar_LockOrientation;
            }
            else
            {
                _orientLock.Text = AppResources.ApplicationBar_UnlockOrientation;
            }
        }

        private void DownloadImageOpened(BitmapImage bmp, PostViewModel item)
        {
            WriteableBitmap wbmp = new WriteableBitmap(bmp);
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    wbmp.SaveJpeg(ms, bmp.PixelWidth, bmp.PixelHeight, 0, 95);
                    ms.Seek(0, SeekOrigin.Begin);
                    new MediaLibrary().SavePicture(item.RenamedFileName + "", ms);
                }
            }
            catch
            {
                bmp.UriSource = null;
                bmp = null;

                throw;
            }
            wbmp = null;

            bmp.UriSource = null;
            bmp = null;
        }

        private void MediaViewer_ItemZoomed(object sender, EventArgs e)
        {
            ApplicationBar.IsVisible = false;
        }

        private void MediaViewer_ItemUnzoomed(object sender, EventArgs e)
        {
            ApplicationBar.IsVisible = true;
        }
    }
}