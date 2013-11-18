using _4charm.Models;
using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace _4charm.Controls
{
    public class TilePicker : Control
    {
        public enum TileResult
        {
            TextOption,
            ImageOption,
            Cancelled
        };

        private string _board;
        private Popup _popup;
        private PhoneApplicationFrame _frame;
        private PhoneApplicationPage _page;
        private Grid _container;

        private TextBlock _tileText;
        private MultiResolutionImage _tileImage;
        private VisualState _fadeOutState;

        private bool _hasApplicationBar;
        private bool _isDismissing;
        private TileResult _result;
        private TaskCompletionSource<TileResult> _tcs;

        public TilePicker()
        {
            DefaultStyleKey = typeof(TilePicker);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _tileText = (TextBlock)GetTemplateChild("TileText");
            _tileImage = (MultiResolutionImage)GetTemplateChild("TileImage");
            _fadeOutState = (VisualState)GetTemplateChild("Faded");
            _fadeOutState.Storyboard.Completed += FadeOutStoryboard_Completed;

            Grid tileTextGrid = (Grid)GetTemplateChild("TileTextGrid");
            tileTextGrid.Tap += (sender, e) => Dismiss(TileResult.TextOption);
            _tileImage.Tap += (sender, e) => Dismiss(TileResult.ImageOption);

            UpdateVisuals();
        }

        public Task<TileResult> ShowAsync(string board)
        {
            _board = board;

            _frame = Application.Current.RootVisual as PhoneApplicationFrame;
            _page = _frame.Content as PhoneApplicationPage;

            // Hide the application bar if necessary.
            if (_page.ApplicationBar != null)
            {
                // Cache the original visibility of the system tray.
                _hasApplicationBar = _page.ApplicationBar.IsVisible;

                // Hide it.
                if (_hasApplicationBar)
                {
                    _page.ApplicationBar.IsVisible = false;
                }
            }
            else
            {
                _hasApplicationBar = false;
            }

            _container = new Grid() { Width = Application.Current.Host.Content.ActualWidth, Height = Application.Current.Host.Content.ActualHeight };
            _container.Children.Add(this);

            // Create and open the popup.
            _popup = new Popup()
            {
                Child = _container,
                IsOpen = true
            };

            // Attach event handlers.
            _page.BackKeyPress += OnBackKeyPress;
            _frame.Navigating += OnNavigating;

            UpdateVisuals();

            _tcs = new TaskCompletionSource<TileResult>();
            return _tcs.Task;
        }

        private void UpdateVisuals()
        {
            if (_tileText != null && _board != null)
            {
                string displayName = ThreadCache.Current.EnforceBoard(_board).DisplayName;
                _tileText.FontSize = 64 - 10 * (_board.Length - 1);
                _tileText.Margin = new Thickness(0, -16 + 4 * (_board.Length - 1), 0, 0);
                _tileText.Text = displayName;
            }
            if (_tileImage != null && _board != null)
            {
                _tileImage.Thumbnail = ThreadCache.Current.EnforceBoard(_board).IconURI;
            }

            if (_tileText != null && _tileImage != null)
            {
                VisualStateManager.GoToState(this, "Unfaded", true);
            }
        }

        private void OnBackKeyPress(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            Dismiss(TileResult.Cancelled);
        }

        private void OnNavigating(object sender, NavigatingCancelEventArgs e)
        {
            Dismiss(TileResult.Cancelled);
        }

        private void Dismiss(TileResult result)
        {
            if (_isDismissing)
            {
                return;
            }

            _isDismissing = true;
            _result = result;

            if (_fadeOutState != null)
            {
                VisualStateManager.GoToState(this, "Faded", true);
            }
            else
            {
                Close(result);
            }
        }

        private void FadeOutStoryboard_Completed(object sender, EventArgs e)
        {
            Close(_result);
        }

        private void Close(TileResult result)
        {
            // Remove the popup.
            if (_popup != null)
            {
                _popup.IsOpen = false;
                _popup = null;
            }

            // Bring the application bar if necessary.
            if (_hasApplicationBar)
            {
                _hasApplicationBar = false;

                // Application bar can be nulled during the Dismissed event
                // so a null check needs to be performed here.
                if (_page.ApplicationBar != null)
                {
                    _page.ApplicationBar.IsVisible = true;
                }
            }

            // Dettach event handlers.
            if (_page != null)
            {
                _page.BackKeyPress -= OnBackKeyPress;
                _page = null;
            }

            if (_frame != null)
            {
                _frame.Navigating -= OnNavigating;
                _frame = null;
            }

            _tcs.SetResult(result);
        }
    }
}
