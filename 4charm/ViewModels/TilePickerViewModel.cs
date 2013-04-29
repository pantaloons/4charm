using _4charm.Models;
using Microsoft.Phone.Shell;
using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace _4charm.ViewModels
{
    class TilePickerViewModel : ViewModelBase
    {
        public static event EventHandler TilePinCompleted = delegate { };

        public BoardViewModel Board
        {
            get { return GetProperty<BoardViewModel>(); }
            set { SetProperty(value); }
        }

        public double FontSize
        {
            get { return GetProperty<double>(); }
            set { SetProperty(value); }
        }

        public Thickness Margin
        {
            get { return GetProperty<Thickness>(); }
            set { SetProperty(value); }
        }

        public bool IsVisible
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public ICommand ImageIconTapped
        {
            get { return GetProperty<ICommand>(); }
            set { SetProperty(value); }
        }

        public ICommand TileIconTapped
        {
            get { return GetProperty<ICommand>(); }
            set { SetProperty(value); }
        }

        private Board _board;

        public TilePickerViewModel()
        {
            Margin = new Thickness(0, 0, 0, 0);
            FontSize = 60;

            ImageIconTapped = new ModelCommand(DoImageIconTapped);
            TileIconTapped = new ModelCommand(DoTileIconTapped);
        }

        private void CompensateForRender(int[] bitmapPixels)
        {
            if (bitmapPixels.Length == 0) return;

            for (int i = 0; i < bitmapPixels.Length; i++)
            {
                uint pixel = unchecked((uint)bitmapPixels[i]);

                double a = (pixel >> 24) & 255;
                if ((a == 255) || (a == 0)) continue;

                double r = (pixel >> 16) & 255;
                double g = (pixel >> 8) & 255;
                double b = (pixel) & 255;

                double factor = 255 / a;
                uint newR = (uint)Math.Round(r * factor);
                uint newG = (uint)Math.Round(g * factor);
                uint newB = (uint)Math.Round(b * factor);

                // compose
                bitmapPixels[i] = unchecked((int)((pixel & 0xFF000000) | (newR << 16) | (newG << 8) | newB));
            }
        }

        private void DoTileIconTapped()
        {
            Grid grid = new Grid();
            grid.Background = (Brush)App.Current.Resources["TransparentBrush"];

            TextBlock heading = new TextBlock();
            heading.FontFamily = new FontFamily("Segoe WP Bold");
            heading.Foreground = new SolidColorBrush(Colors.White);
            heading.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            heading.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            heading.Text = _board.DisplayName;

            grid.Children.Add(heading);

            heading.FontSize = 64 - 10 * (_board.Name.Length - 1);
            heading.Margin = new Thickness(0, -16 + 4 * (_board.Name.Length - 1), 0, 0);
            WriteableBitmap small = new WriteableBitmap(159, 159);
            grid.Measure(new Size(159, 159));
            grid.Arrange(new Rect(0, 0, 159, 159));
            small.Render(grid, null);
            small.Invalidate();
            CompensateForRender(small.Pixels);

            heading.FontSize = 126 - 20 * (_board.Name.Length - 1);
            heading.Margin = new Thickness(0, -24 + 5 * (_board.Name.Length - 1), 0, 0);
            WriteableBitmap large = new WriteableBitmap(336, 336);
            grid.Measure(new Size(336, 336));
            grid.Arrange(new Rect(0, 0, 336, 336));
            large.Render(grid, null);
            large.Invalidate();
            CompensateForRender(large.Pixels);

            heading.FontSize = 126;
            heading.Margin = new Thickness(0, -24, 0, 0);
            WriteableBitmap wide = new WriteableBitmap(691, 336);
            grid.Measure(new Size(691, 336));
            grid.Arrange(new Rect(0, 0, 691, 336));
            wide.Render(grid, null);
            wide.Invalidate();
            CompensateForRender(wide.Pixels);

            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream("/Shared/ShellContent/tile-" + _board.Name + "-small.png", FileMode.Create, isf))
                {
                    small.WritePNG(isfs);
                }
                using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream("/Shared/ShellContent/tile-" + _board.Name + "-large.png", FileMode.Create, isf))
                {
                    large.WritePNG(isfs);
                }
                using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream("/Shared/ShellContent/tile-" + _board.Name + "-wide.png", FileMode.Create, isf))
                {
                    wide.WritePNG(isfs);
                }
            }

            Uri pin = new Uri("/Views/ThreadsPage.xaml?board=" + _board.Name, UriKind.Relative);
            FlipTileData data = new FlipTileData()
            {
                SmallBackgroundImage = new Uri("isostore:/Shared/ShellContent/tile-" + _board.Name + "-small.png", UriKind.Absolute),
                BackgroundImage = new Uri("isostore:/Shared/ShellContent/tile-" + _board.Name + "-large.png", UriKind.Absolute),
                WideBackgroundImage = new Uri("isostore:/Shared/ShellContent/tile-" + _board.Name + "-wide.png", UriKind.Absolute),
            };
            try
            {
                ShellTile.Create(pin, data, true);
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show("Board is already pinned.");
            }

            IsVisible = false;
            TilePinCompleted(null, null);
        }

        private void DoImageIconTapped()
        {
            Uri pin = new Uri("/Views/ThreadsPage.xaml?board=" + _board.Name, UriKind.Relative);
            FlipTileData data = new FlipTileData()
            {
                BackgroundImage = _board.IconURI,
                WideBackgroundImage = _board.WideURI,
                Title = _board.DisplayName
            };
            try
            {
                ShellTile.Create(pin, data, true);
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show("Board is already pinned.");
            }

            IsVisible = false;
            TilePinCompleted(null, null);
        }

        public void SetBoard(Board board)
        {
            if (board == null)
            {
                if (Board != null) Board.UnloadIcon();
                IsVisible = false;
            }
            else
            {
                _board = board;
                Board = new BoardViewModel(board);
                FontSize = 64 - 10 * (board.Name.Length - 1);
                Margin = new Thickness(0, -16 + 4 * (board.Name.Length - 1), 0, 0);
                IsVisible = true;

                Board.LoadIcon();
            }
        }
    }
}
