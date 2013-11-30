using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace _4charm.Models
{
    public static class BitmapImageExtensions
    {
        public class BitmapImageTaskSource
        {
            private TaskCompletionSource<bool> _task;

            public Task<bool> Task
            {
                get { return _task.Task; }
            }

            public BitmapImageTaskSource(BitmapImage bi, Stream source)
            {
                _task = new TaskCompletionSource<bool>();
                bi.ImageOpened += Image_ImageOpened;
                bi.ImageFailed += Image_ImageFailed;
                bi.SetSource(source);
            }

            private void Image_ImageOpened(object sender, System.Windows.RoutedEventArgs e)
            {
                ((BitmapImage)sender).ImageOpened += Image_ImageOpened;
                ((BitmapImage)sender).ImageFailed += Image_ImageFailed;

                _task.SetResult(true);
            }

            private void Image_ImageFailed(object sender, System.Windows.RoutedEventArgs e)
            {
                ((BitmapImage)sender).ImageOpened += Image_ImageOpened;
                ((BitmapImage)sender).ImageFailed += Image_ImageFailed;

                _task.SetResult(false);
            }
        }
    }
}
