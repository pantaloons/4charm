using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace _4charm.Controls
{
    public class TextSizeSlider : Control
    {
        private const int TEXTSIZE_MIN = 17;
        private const int TEXTSIZE_STEP = 1;

        #region TextSize DependencyProperty

        public static readonly DependencyProperty TextSizeProperty = DependencyProperty.Register(
            "TextSize",
            typeof(int),
            typeof(TextSizeSlider),
            new PropertyMetadata(TEXTSIZE_MIN, OnTextSizeChanged));

        public int TextSize
        {
            get { return (int)GetValue(TextSizeProperty); }
            set { SetValue(TextSizeProperty, value); }
        }

        private static void OnTextSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as TextSizeSlider).TextSizeChanged();
        }

        #endregion

        private Size? _size;
        private Grid _sliderGrid;
        private Rectangle _sliderRect;
        private TextBlock _sampleText;
        private Rectangle _sliderFill1, _sliderFill2, _sliderFill3;

        public TextSizeSlider()
        {
            DefaultStyleKey = typeof(TextSizeSlider);

            SizeChanged += TextSizeSlider_SizeChanged;
        }

        private void TextSizeSlider_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _size = e.NewSize;

            TextSizeChanged();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _sliderGrid = GetTemplateChild("SliderGrid") as Grid;
            _sliderRect = GetTemplateChild("SliderRect") as Rectangle;
            _sampleText = GetTemplateChild("SampleText") as TextBlock;

            _sliderFill1 = GetTemplateChild("SliderFill1") as Rectangle;
            _sliderFill2 = GetTemplateChild("SliderFill2") as Rectangle;
            _sliderFill3 = GetTemplateChild("SliderFill3") as Rectangle;

            if (_sliderGrid != null)
            {
                _sliderGrid.Tap += SliderGrid_Tap;
                _sliderGrid.ManipulationStarted += SliderGrid_ManipulationStarted;
                _sliderGrid.ManipulationDelta += SliderGrid_ManipulationDelta;
                _sliderGrid.ManipulationCompleted += SliderGrid_ManipulationCompleted;
            }

            TextSizeChanged();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            TextSizeChanged();

            return base.ArrangeOverride(finalSize);
        }

        private void SliderGrid_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            GeneralTransform gt = e.ManipulationContainer.TransformToVisual(_sliderGrid);
            UpdateSliderForX(gt.Transform(new Point(e.ManipulationOrigin.X, e.ManipulationOrigin.Y)).X);
        }

        private void SliderGrid_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            GeneralTransform gt = e.ManipulationContainer.TransformToVisual(_sliderGrid);
            UpdateSliderForX(gt.Transform(new Point(e.ManipulationOrigin.X, e.ManipulationOrigin.Y)).X);
        }

        private void SliderGrid_ManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            GeneralTransform gt = e.ManipulationContainer.TransformToVisual(_sliderGrid);
            UpdateSliderForX(gt.Transform(e.ManipulationOrigin).X);
        }

        private void UpdateSliderForX(double delta)
        {
            double sectionWidth = (ActualWidth - 2 * 2) / 3;

            double center = delta - _sliderRect.Width / 2.0;

            double normalized = Math.Max(0, Math.Min(3, center / (sectionWidth + 2.0)));
            int section = (int)Math.Round(normalized, MidpointRounding.AwayFromZero);

            TextSize = TEXTSIZE_MIN + section * TEXTSIZE_STEP;
        }

        private void SliderGrid_Tap(object sender, GestureEventArgs e)
        {
            UpdateSliderForX(e.GetPosition((UIElement)sender).X);
        }

        private void TextSizeChanged()
        {
            if (_size == null)
            {
                return;
            }

            Debug.Assert(TextSize >= TEXTSIZE_MIN && TextSize <= TEXTSIZE_MIN + 3 * TEXTSIZE_STEP);

            double sectionWidth = (ActualWidth - 2 * 2) / 3;
            int section = Math.Max(0, Math.Min(3, (TextSize - TEXTSIZE_MIN) / TEXTSIZE_STEP));

            if (_sliderRect != null)
            {
                Canvas.SetLeft(_sliderRect, Math.Max(0, Math.Min(ActualWidth - _sliderRect.Width, (sectionWidth + 2) * section - _sliderRect.Width / 2.0)));
            }

            if (_sampleText != null)
            {
                _sampleText.FontSize = TextSize;
            }

            if (_sliderFill1 != null)
            {
                _sliderFill1.Fill = section > 0 ? (Brush)App.Current.Resources["PhoneAccentBrush"] : (Brush)App.Current.Resources["PhoneSubtleBrush"];
            }
            if (_sliderFill2 != null)
            {
                _sliderFill2.Fill = section > 1 ? (Brush)App.Current.Resources["PhoneAccentBrush"] : (Brush)App.Current.Resources["PhoneSubtleBrush"];
            }
            if (_sliderFill3 != null)
            {
                _sliderFill3.Fill = section > 2 ? (Brush)App.Current.Resources["PhoneAccentBrush"] : (Brush)App.Current.Resources["PhoneSubtleBrush"];
            }
        }
    }
}
