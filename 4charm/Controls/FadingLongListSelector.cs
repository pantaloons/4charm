using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace _4charm.Controls
{
    public class FadingLongListSelector : PlaceHolderLongListSelector
    {
        #region FadeLimit DependencyProperty

        public static readonly DependencyProperty FadeLimitProperty = DependencyProperty.Register(
            "FadeLimit",
            typeof(int),
            typeof(FadingLongListSelector),
            new PropertyMetadata(0));

        public int FadeLimit
        {
            get { return (int)GetValue(FadeLimitProperty); }
            set { SetValue(FadeLimitProperty, value); }
        }

        #endregion

        #region Easing DependencyProperty

        public static readonly DependencyProperty EasingProperty = DependencyProperty.Register(
            "Easing",
            typeof(EasingFunctionBase),
            typeof(FadingLongListSelector),
            new PropertyMetadata(null));

        public EasingFunctionBase Easing
        {
            get { return (EasingFunctionBase)GetValue(EasingProperty); }
            set { SetValue(EasingProperty, value); }
        }

        #endregion

        public FadingLongListSelector()
        {
            ItemRealized += FadingLongListSelector_ItemRealized;
        }

        private void FadingLongListSelector_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            if (e.ItemKind == LongListSelectorItemKind.Item && ItemsSource != null && ItemsSource.Count < FadeLimit)
            {
                int index = ItemsSource.IndexOf(e.Container.DataContext);
                if (index < 0 || index > FadeLimit)
                {
                    return;
                }

                DoubleAnimation da = new DoubleAnimation()
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(500),
                    EasingFunction = Easing
                };
                Storyboard.SetTargetProperty(da, new PropertyPath(FrameworkElement.OpacityProperty));
                Storyboard.SetTarget(da, e.Container);

                Storyboard sb = new Storyboard();
                sb.Children.Add(da);
                sb.Begin();
            }
        }
    }
}
