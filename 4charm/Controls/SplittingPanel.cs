using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace _4charm.Controls
{
    public class SplittingPanel : Panel
    {
        #region SplitRatio DependencyProperty

        public static readonly DependencyProperty SplitRatioProperty = DependencyProperty.Register(
            "SplitRatio",
            typeof(double),
            typeof(SplittingPanel),
            new PropertyMetadata(0.0, OnSplitRatioChanged));

        public double SplitRatio
        {
            get { return (double)GetValue(SplitRatioProperty); }
            set { SetValue(SplitRatioProperty, value); }
        }

        private static void OnSplitRatioChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as SplittingPanel).SplitRatioChanged();
        }

        #endregion

        #region IsExpanded DependencyProperty

        public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
            "IsExpanded",
            typeof(bool),
            typeof(SplittingPanel),
            new PropertyMetadata(false, OnIsExpandedChanged));

        public bool IsExpanded
        {
            get { return (bool)GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, value); }
        }

        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as SplittingPanel).IsExpandedChanged();
        }

        #endregion

        private bool _fullyExpanded;
        private Storyboard _translateStoryboard;
        private DoubleAnimation[] _translateAnimations = new DoubleAnimation[2];

        protected override Size MeasureOverride(Size availableSize)
        {
            Children[0].Measure(new Size(availableSize.Width, SplitRatio * availableSize.Height));
            Children[1].Measure(new Size(availableSize.Width, availableSize.Height));

            return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (!_fullyExpanded)
            {
                Children[0].Arrange(new Rect(0, -SplitRatio * finalSize.Height, finalSize.Width, SplitRatio * finalSize.Height));
                Children[1].Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
                (Children[1] as FrameworkElement).Margin = new Thickness(0, 0, 0, 0);
            }
            else
            {
                Children[0].Arrange(new Rect(0, 0, finalSize.Width, SplitRatio * finalSize.Height));
                Children[1].Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));

                // We have to use a margin since the ViewportControl doesn't behave nicely when rearranging its bounds.
                (Children[1] as FrameworkElement).Margin = new Thickness(0, finalSize.Height * SplitRatio, 0, 0);
            }

            return finalSize;
        }

        private void SplitRatioChanged()
        {
            InvalidateMeasure();
            InvalidateArrange();
        }

        private void IsExpandedChanged()
        {
            if (Children.Count != 2) return;

            Debug.Assert(SplitRatio >= 0 && SplitRatio <= 1.0);

            EnsureStoryboardsLoaded();

            _translateStoryboard.Stop();

            if (IsExpanded)
            {
                _translateAnimations[0].From = 0;
                _translateAnimations[0].To = SplitRatio * ActualHeight;

                _translateAnimations[1].From = 0;
                _translateAnimations[1].To = SplitRatio * ActualHeight;

                EasingFunctionBase ease = new ElasticEase() { EasingMode = EasingMode.EaseOut, Oscillations = 3, Springiness = 7 };
                TimeSpan duration = TimeSpan.FromMilliseconds(750);

                _translateAnimations[0].Duration = duration;
                _translateAnimations[0].EasingFunction = ease;
                _translateAnimations[1].Duration = duration;
                _translateAnimations[1].EasingFunction = ease;
            }
            else
            {
                _fullyExpanded = false;
                InvalidateArrange();
                UpdateLayout();

                _translateAnimations[0].From = SplitRatio * ActualHeight;
                _translateAnimations[0].To = 0;

                _translateAnimations[1].From = SplitRatio * ActualHeight;
                _translateAnimations[1].To = 0;

                EasingFunctionBase ease = new ExponentialEase() { EasingMode = EasingMode.EaseOut };
                TimeSpan duration = TimeSpan.FromMilliseconds(150);

                _translateAnimations[0].Duration = duration;
                _translateAnimations[0].EasingFunction = ease;
                _translateAnimations[1].Duration = duration;
                _translateAnimations[1].EasingFunction = ease;
            }

            _translateStoryboard.Begin();
        }

        private void EnsureStoryboardsLoaded()
        {
            if (_translateStoryboard != null)
            {
                return;
            }

            Children[0].RenderTransform = new CompositeTransform();
            Children[1].RenderTransform = new CompositeTransform();

            _translateStoryboard = new Storyboard();

            _translateAnimations[0] = new DoubleAnimation();
            Storyboard.SetTarget(_translateAnimations[0], Children[0]);
            Storyboard.SetTargetProperty(_translateAnimations[0], new PropertyPath("(UIElement.RenderTransform).(CompositeTransform.TranslateY)"));

            _translateAnimations[1] = new DoubleAnimation();
            Storyboard.SetTarget(_translateAnimations[1], Children[1]);
            Storyboard.SetTargetProperty(_translateAnimations[1], new PropertyPath("(UIElement.RenderTransform).(CompositeTransform.TranslateY)"));

            _translateStoryboard.Children.Add(_translateAnimations[0]);
            _translateStoryboard.Children.Add(_translateAnimations[1]);

            _translateStoryboard.Completed += TranslateStoryboard_Completed;
        }

        private void TranslateStoryboard_Completed(object sender, EventArgs e)
        {
            (sender as Storyboard).Stop();

            if (IsExpanded)
            {
                _fullyExpanded = true;
                InvalidateArrange();
                UpdateLayout();
            }
        }
    }
}
