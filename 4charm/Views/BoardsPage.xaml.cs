using _4charm.Models;
using _4charm.Resources;
using _4charm.ViewModels;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace _4charm.Views
{
    public partial class BoardsPage : PhoneApplicationPage
    {
        internal static BoardViewModel SetBoard = null;

        private BoardsPageViewModel _viewModel;
        private ApplicationBarIconButton _clear, _create;

        public BoardsPage()
        {
            InitializeComponent();
            InitializeApplicationBar();

            _viewModel = new BoardsPageViewModel();
            DataContext = _viewModel;

            BoardViewModel.NewBoardAdded += NewBoardAdded;
            BoardViewModel.BoardPinned += BoardPinned;
            TilePickerViewModel.TilePinCompleted += TilePinCompleted;
        }

        private void InitializeApplicationBar()
        {
            _clear = new ApplicationBarIconButton(new Uri("/Assets/Appbar/appbar.delete.png", UriKind.Relative))
            {
                Text = AppResources.ApplicationBar_Clear
            };
            _clear.Click += (sender, e) =>
            {
                TransitorySettingsManager.Current.History.Clear();
            };

            _create = new ApplicationBarIconButton(new Uri("/Assets/Appbar/appbar.add.png", UriKind.Relative))
            {
                Text = AppResources.BoardsPage_AddBoard
            };
            _create.Click += (sender, e) =>
            {
                _viewModel.Navigate(new Uri("/Views/AddBoardPage.xaml", UriKind.Relative));
            };

            ApplicationBar = new ApplicationBar() { IsVisible = false };
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);

            if (_viewModel.TilePicker.IsVisible)
            {
                TilePinCompleted(null, null);
                e.Cancel = true;
            }
        }

        private void NewBoardAdded(object sender, Board e)
        {
            BoardViewModel bvm = _viewModel.Favorites.FirstOrDefault(x => x.Name == e.Name);
            if (bvm != null)
            {
                RootPivot.SelectedIndex = 0;
                Favorites.UpdateLayout();
                Favorites.ScrollTo(bvm);
            }
        }

        private void BoardPinned(object sender, Board board)
        {
            _viewModel.TilePicker.SetBoard(board);
            TilePickerFadeIn.Begin();

            ApplicationBar.IsVisible = false;
            RootPivot.IsHitTestVisible = false;
        }

        private void TilePinCompleted(object sender, EventArgs e)
        {
            TilePickerFadeOut.Begin();

            RootPivot.IsHitTestVisible = true;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.Back && SetBoard != null)
            {
                BoardViewModel bvm = _viewModel.All.FirstOrDefault(x => x.Name == SetBoard.Name);
                if(bvm != null)
                {
                    RootPivot.SelectedIndex = 3;
                    All.UpdateLayout();
                    All.ScrollTo(bvm);
                }
            }
            SetBoard = null;

            _viewModel.OnNavigatedTo();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            _viewModel.OnNavigatedFrom(e);
        }

        protected override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);

            BoardViewModel.NewBoardAdded -= NewBoardAdded;
        }

        private void RootPivotChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplicationBar.IsVisible = RootPivot.SelectedIndex == 2 || RootPivot.SelectedIndex == 3;
            if (RootPivot.SelectedIndex == 2)
            {
                ApplicationBar.Buttons.Clear();
                ApplicationBar.Buttons.Add(_clear);
            }
            else if(RootPivot.SelectedIndex == 3)
            {
                ApplicationBar.Buttons.Clear();
                ApplicationBar.Buttons.Add(_create);
            }
        }

        private void BoardRealized(object sender, ItemRealizationEventArgs e)
        {
            if (e.ItemKind == LongListSelectorItemKind.Item)
            {
                (e.Container.Content as BoardViewModel).LoadImage();
            }
        }

        private void BoardUnrealized(object sender, ItemRealizationEventArgs e)
        {
            if (e.ItemKind == LongListSelectorItemKind.Item)
            {
                (e.Container.Content as BoardViewModel).UnloadImage();
            }
        }

        private void ThreadRealized(object sender, ItemRealizationEventArgs e)
        {
            if (e.ItemKind == LongListSelectorItemKind.Item)
            {
                (e.Container.Content as ThreadViewModel).LoadImage();
            }
        }

        private void ThreadUnrealized(object sender, ItemRealizationEventArgs e)
        {
            if (e.ItemKind == LongListSelectorItemKind.Item)
            {
                (e.Container.Content as ThreadViewModel).UnloadImage();
            }
        }

        private void TilePickerFadeOutCompleted(object sender, EventArgs e)
        {
            _viewModel.TilePicker.SetBoard(null);
            ApplicationBar.IsVisible = RootPivot.SelectedIndex == 2 || RootPivot.SelectedIndex == 3;
        }

        private void AddboardTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Views/AddBoardPage.xaml", UriKind.Relative));
        }

        private void SettingsTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Views/SettingsPage.xaml", UriKind.Relative));
        }

        private void RateTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            new MarketplaceReviewTask().Show();
        }

        private void FeedbackTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            new EmailComposeTask() { To = "michael@pantaloons.co.nz", Subject = "4charm feedback" }.Show();
        }

        private void AboutTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Views/AboutPage.xaml", UriKind.Relative));
        }

        private void ContextMenuOpened(object sender, System.Windows.RoutedEventArgs e)
        {
            var menu = (ContextMenu)sender;
            var owner = (FrameworkElement)menu.Owner;
            if (owner.DataContext != menu.DataContext) menu.DataContext = owner.DataContext;
        }
    }
}