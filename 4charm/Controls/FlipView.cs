using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace _4charm.Controls
{
    public class FlipView : Control
    {
        private class DragState
        {
            public double MaxDraggingBoundary { get; set; }
            public double MinDraggingBoundary { get; set; }
            public bool GotDragDelta { get; set; }
            public bool IsDraggingFirstElement { get; set; }
            public bool IsDraggingLastElement { get; set; }
            public DateTime LastDragUpdateTime { get; set; }
            public double DragStartingMediaStripOffset { get; set; }
            public double NetDragDistanceSincleLastDragStagnation { get; set; }
            public double LastDragDistanceDelta { get; set; }
            public int NewDisplayedElementIndex { get; set; }
            public double UnsquishTranslationAnimationTarget { get; set; }

            public DragState(double maxDraggingBoundary)
            {
                this.MaxDraggingBoundary = maxDraggingBoundary;
            }
        }

        private class ContentFlipperContent
        {
            private const double MaxTextureSideLength = 2048;

            public bool IsZoomedIn
            {
                get { return _currentZoomRatio > 1.0 || _pinchInProgress; }
            }

            public double HorizontalOffset
            {
                get { return Canvas.GetLeft(_container); }
                set { Canvas.SetLeft(_container, value); }
            }

            private Point _lastOrigin;
            private bool _pinchInProgress;
            private bool _centerAtNextOpportunity;
            private Point _pinchMidpointInControlCoordinates;
            private Point _pinchMidpointInPercentOfContent;
            private double _zoomRatioDuringPinch;
            private double _currentZoomRatio = 1.0;
            private Size _actualContentSize = new Size(0, 0);

            private Size _size;
            private FrameworkElement _container;
            private ViewportControl _viewport;
            private Canvas _canvas;
            private FrameworkElement _zoomableContent;
            private ScaleTransform _transform;

            public ContentFlipperContent(FrameworkElement container, Size size)
            {
                _container = container;
                _viewport = (ViewportControl)container.FindName("FlipperViewport");
                _canvas = (Canvas)container.FindName("FlipperCanvas");
                _zoomableContent = (FrameworkElement)container.FindName("FlipperZoomableContent");
                _transform = (ScaleTransform)container.FindName("FlipperZoomableContentTransform");

                _zoomableContent.ManipulationStarted += OnManipulationStarted;
                _zoomableContent.ManipulationDelta += OnManipulationDelta;
                _zoomableContent.ManipulationCompleted += OnManipulationCompleted;
                _zoomableContent.LayoutUpdated += OnZoomableContentLayoutUpdated;

                SetSize(size);
            }

            public void SetSize(Size size)
            {
                _size = size;

                _container.Width = size.Width;
                _container.Height = size.Height;

                ZoomAllTheWayOut();
            }

            public void MakeVisible()
            {
                _container.Visibility = Visibility.Visible;
            }

            public void SetItem(object item)
            {
                _container.DataContext = item;
            }

            public void OnDoubleTap()
            {
                if (IsZoomedIn)
                {
                    ZoomAllTheWayOut();
                }
                else
                {
                    ZoomInToDefaultLevel();
                }
            }

            private void ZoomInToDefaultLevel()
            {
                _currentZoomRatio = 2.0;
                _centerAtNextOpportunity = true;
                ResizeContent();
            }

            private void ZoomAllTheWayOut()
            {
                _currentZoomRatio = 1.0;
                _centerAtNextOpportunity = true;
                ResizeContent();
            }

            private void OnManipulationStarted(object sender, ManipulationStartedEventArgs e)
            {
                // Clear the pinchInProgress flag since we don't know yet if this is a pinch or not
                //
                _pinchInProgress = false;
            }

            private void OnManipulationDelta(object sender, ManipulationDeltaEventArgs e)
            {
                if (e.PinchManipulation != null)
                {
                    e.Handled = true;

                    // If this is the first delta of this pinch, calculate the starting position of the pinch
                    //
                    if (!_pinchInProgress)
                    {
                        _pinchInProgress = true;

                        Point center = e.PinchManipulation.Original.Center;
                        _pinchMidpointInPercentOfContent = new Point(center.X / _actualContentSize.Width, center.Y / _actualContentSize.Height);

                        var xform = _zoomableContent.TransformToVisual(_container);
                        _pinchMidpointInControlCoordinates = xform.Transform(center);

                        _lastOrigin = new Point(-1, -1);
                    }

                    _zoomRatioDuringPinch = ClampZoomRatioToMinMax(_currentZoomRatio * e.PinchManipulation.CumulativeScale);
                    ScaleContentDuringPinch();
                }
                else
                {
                    if (_pinchInProgress)
                    {
                        // This happens when pinching then lifting a finger - let's commit the pinch as if it were the end of the manipulation
                        //
                        FinishPinch();
                    }
                }
            }

            private void OnManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
            {
                if (_pinchInProgress)
                {
                    e.Handled = true;

                    FinishPinch();
                }
            }

            private void FinishPinch()
            {
                _pinchInProgress = false;
                _currentZoomRatio = _zoomRatioDuringPinch;

                ResizeContent();
            }

            /// <summary>
            /// Resizes the content itself into the "settled" zoom ratio (currentZoomRatio).  
            /// This is not used during a pinch operation.
            /// </summary>
            private void ResizeContent()
            {
                double newMaxWidth = Math.Round(_size.Width * _currentZoomRatio);
                double newMaxHeight = Math.Round(_size.Height * _currentZoomRatio);

                _canvas.Width = newMaxWidth;
                _canvas.Height = newMaxHeight;

                //// Give the content a bounding box to fit within.
                ////
                _zoomableContent.MaxWidth = newMaxWidth;
                _zoomableContent.MaxHeight = newMaxHeight;

                //// Undo any scaling
                ////
                _transform.ScaleX = 1.0;
                _transform.ScaleY = 1.0;
            }

            /// <summary>
            /// Used during a pinch operation to scale the content for a fast pinch effect.  This does not actually change
            /// the layout of the content, so this can be done very quickly.  When the pinch completes, ResizeContent() will
            /// update the content's layout with the final zoom ratio.
            /// </summary>
            private void ScaleContentDuringPinch()
            {
                double newMaxWidth = Math.Round(_size.Width * _zoomRatioDuringPinch);
                double newMaxHeight = Math.Round(_size.Height * _zoomRatioDuringPinch);

                double scaler = newMaxWidth / _zoomableContent.MaxWidth;

                double newWidth = _actualContentSize.Width * scaler;
                double newHeight = _actualContentSize.Height * scaler;

                _transform.ScaleX = scaler;
                _transform.ScaleY = scaler;

                _viewport.Bounds = new Rect(0, 0, newWidth, newHeight);

                // Pan the Viewport such that the point on the content where the pinch started doesn't move on the screen
                //
                Point pinchMidpointOnScaledContent = new Point(
                    newWidth * _pinchMidpointInPercentOfContent.X,
                    newHeight * _pinchMidpointInPercentOfContent.Y);

                Point origin = new Point(
                    (int)(pinchMidpointOnScaledContent.X - _pinchMidpointInControlCoordinates.X),
                    (int)(pinchMidpointOnScaledContent.Y - _pinchMidpointInControlCoordinates.Y));

                if (origin != _lastOrigin)
                {
                    _viewport.SetViewportOrigin(origin);
                    _lastOrigin = origin;
                }
            }

            /// <summary>
            /// When the ZoomableContent calculates a new DesiredSize, we need to update our Viewport bounds
            /// and possibly center the content now that the DesiredSize is known.
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void OnZoomableContentLayoutUpdated(object sender, EventArgs e)
            {
                if ((_zoomableContent.DesiredSize.Height != _actualContentSize.Height) ||
                    (_zoomableContent.DesiredSize.Width != _actualContentSize.Width))
                {
                    // If the desired size has changed, record the new desired size and update the viewport bounds
                    //
                    _actualContentSize = _zoomableContent.DesiredSize;
                    _viewport.Bounds = new Rect(0, 0, _actualContentSize.Width, _actualContentSize.Height);

                    // Center it if requested
                    //
                    if (_centerAtNextOpportunity)
                    {
                        _viewport.SetViewportOrigin(
                            new Point(
                                Math.Round((_actualContentSize.Width - _size.Width) / 2),
                                Math.Round((_actualContentSize.Height - _size.Height) / 2)
                                ));
                        _centerAtNextOpportunity = false;
                    }
                }
            }

            private double ClampZoomRatioToMinMax(double ratio)
            {
                // Ensure that we never zoom in such that the content has a side > 2048 pixels (the texture size limit)
                //
                double maxZoomRatio = Math.Min(
                    MaxTextureSideLength / _size.Width,
                    MaxTextureSideLength / _size.Height);

                return Math.Min(maxZoomRatio, Math.Max(1.0, ratio));
            }
        }

        /// <summary>
        /// The number of items to load at a time.  Must be odd.
        /// </summary>
        private const int VirtualPoolSize = 3;

        /// <summary>
        /// The amount of space between items.  For full-screen items this will only be visible when scrolling from
        /// one item to another.
        /// </summary>
        private const double ItemGutter = 18;

        /// <summary>
        /// How many pixels past the beginning or end of the list the user will be allowed to drag.
        /// Note that during a drag past the beginning or end of the list the user will see the "squish" animation.
        /// </summary>
        private const double MaxDraggingSquishDistance = 150;

        /// <summary>
        /// How much to squish the UI if you drag maxDraggingSquishDistance past the beginning or end of the UI.
        /// </summary>
        private const double MinDraggingSquishScale = 0.90;

        /// <summary>
        /// How long the unsquish animation should take
        /// </summary>
        private const int UnsquishAnimationMilliseconds = 100;

        /// <summary>
        /// How long should a pause in dragging be before it resets the inertia calculation?  This is in milliseconds.
        /// </summary>
        private const double DragStagnationTimeThreshold = 300;

        /// <summary>
        /// Tolerance in pixels for considering a drag stopped
        /// </summary>
        private const double DragStagnationDistanceThreshold = 15;

        // These constants define how fast the inertia animation will run after a flick.  The actual flick speed 
        // (in pixels / ms) is mapped in the range _flickMinInputVelocity, _flickMaxInputVelocity, and an inertia
        // animation duration is then calculated as the same proportion of _flickMinOutputMilliseconds, 
        // _flickMaxOutputMilliseconds.
        private const double FlickMinInputVelocity = 0;
        private const double FlickMaxInputVelocity = 5;
        private const double FlickMinOutputMilliseconds = 350;
        private const double FlickMaxOutputMilliseconds = 1400;

        #region ItemTemplate DependencyProperty

        public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(
            "ItemTemplate",
            typeof(DataTemplate),
            typeof(FlipView),
            new PropertyMetadata(null, OnItemTemplateChanged));

        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        private static void OnItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as FlipView).ItemTemplateChanged();
        }

        #endregion

        #region ItemsSource DependencyProperty

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            "ItemsSource",
            typeof(ObservableCollection<object>),
            typeof(FlipView),
            new PropertyMetadata(null, OnItemsSourceChanged));

        public ObservableCollection<object> ItemsSource
        {
            get { return (ObservableCollection<object>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as FlipView).ItemsSourceChanged(e);
        }

        #endregion

        private enum FlipperState
        {
            Uninitialized,
            Initialized,
            InertiaAnimating,
            Dragging,
            DraggingAndSquishing,
            Pinching,
            UnsquishAnimating
        };

        private int _displayedItemIndex = -1;
        private int _displayedContainerIndex;
        private int[] _representingIndex = new int[VirtualPoolSize];
        private Size? _size;
        private FlipperState _state = FlipperState.Uninitialized;

        private Canvas _rootCanvas;
        private CompositeTransform _rootTransform;
        private ContentFlipperContent[] _containers = new ContentFlipperContent[VirtualPoolSize];

        private DragState _dragState = new DragState(MaxDraggingSquishDistance);
        private Storyboard _dragInertiaAnimation;
        private Storyboard _unsquishAnimation;
        private DoubleAnimation _dragInertiaAnimationTranslation;
        private DoubleAnimation _unsquishAnimationTranslation;

        public FlipView()
        {
            Debug.Assert(VirtualPoolSize % 2 == 1);

            DefaultStyleKey = typeof(FlipView);

            for (int i = 0; i < VirtualPoolSize; i++)
            {
                _representingIndex[i] = -1;
            }

            Unloaded += ImageFlipper_Unloaded;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _size = finalSize;

            if (_state == FlipperState.Uninitialized)
            {
                InitializeIfReady();
            }
            else
            {
                for (int i = 0; i < VirtualPoolSize; i++)
                {
                    _containers[i].SetSize(_size.Value);
                }

                ResetGeometry();
                UpdateVirtualizedItemPositions();
                UpdateViewport();
            }

            return base.ArrangeOverride(finalSize);
        }

        private void ImageFlipper_Unloaded(object sender, RoutedEventArgs e)
        {
            if (ItemsSource != null)
            {
                ItemsSource.CollectionChanged -= UpdateItems;
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _rootCanvas = (Canvas)GetTemplateChild("FlickRoot");
            _rootTransform = (CompositeTransform)GetTemplateChild("FlickRootTransform");

            InitializeIfReady();
        }

        private void ItemTemplateChanged()
        {
            InitializeIfReady();
        }

        private void ItemsSourceChanged(DependencyPropertyChangedEventArgs e)
        {
            ObservableCollection<object> oldItems = e.OldValue as ObservableCollection<object>;
            ObservableCollection<object> newItems = e.NewValue as ObservableCollection<object>;

            if (e.OldValue != null)
            {
                oldItems.CollectionChanged -= UpdateItems;
            }

            if (e.NewValue != null)
            {
                newItems.CollectionChanged += UpdateItems;
            }

            InitializeIfReady();
        }

        private void InitializeIfReady()
        {
            if (_state != FlipperState.Uninitialized || _size == null || _rootCanvas == null || ItemTemplate == null || ItemsSource == null)
            {
                return;
            }

            for (int i = 0; i < VirtualPoolSize; i++)
            {
                FrameworkElement root = (FrameworkElement)ItemTemplate.LoadContent();
                root.Visibility = Visibility.Collapsed;
                _containers[i] = new ContentFlipperContent(root, _size.Value);
                _rootCanvas.Children.Add(root);
            }

            _state = FlipperState.Initialized;

            _displayedItemIndex = ItemsSource.Count >= 0 ? 0 : -1;
            ResetGeometry();
            UpdateVirtualizedItemPositions();
        }

        private void ResetGeometry()
        {
            _rootCanvas.Height = _size.Value.Height;
            _rootCanvas.Width = ItemsSource.Count * (_size.Value.Width + ItemGutter) - ItemGutter;

            _dragState.MinDraggingBoundary = MaxDraggingSquishDistance;
            _dragState.MaxDraggingBoundary = -1 * (_rootCanvas.Width - _size.Value.Width + MaxDraggingSquishDistance);
        }

        private void UpdateVirtualizedItemPositions()
        {
            // Calculate the range of indexes we want the virtualized items to represent
            int itemsToEitherSide = VirtualPoolSize / 2;
            int firstIndex = Math.Max(0, _displayedItemIndex - itemsToEitherSide);
            int lastIndex = Math.Min(ItemsSource.Count - 1, _displayedItemIndex + itemsToEitherSide);

            for (int i = firstIndex; i <= lastIndex; i++)
            {
                bool isAlreadyVirtualizedIn = false;
                int repurposeCandidate = -1;

                // Check to see if this item index is already virtualized in
                for (int j = 0; j < VirtualPoolSize; j++)
                {
                    if (_displayedItemIndex >= 0 && _representingIndex[j] == _displayedItemIndex)
                    {
                        _displayedContainerIndex = j;
                    }

                    if (i == _representingIndex[j])
                    {
                        isAlreadyVirtualizedIn = true;

                        double offset = i * (_size.Value.Width + ItemGutter);
                        if (_containers[j].HorizontalOffset != offset)
                        {
                            _containers[j].HorizontalOffset = offset;
                        }
                        break;
                    }
                    else
                    {
                        if (repurposeCandidate == -1 || _representingIndex[j] == -1)
                        {
                            repurposeCandidate = j;
                        }
                        else if (repurposeCandidate >= 0 && _representingIndex[repurposeCandidate] >= 0)
                        {
                            // Look for the VirtualizedItem that is furthest from our itemIndexToCenterOn

                            int existingDistance = Math.Abs(_representingIndex[repurposeCandidate] - _displayedItemIndex);
                            int thisDistance = Math.Abs(_representingIndex[j] - _displayedItemIndex);

                            if (thisDistance > existingDistance)
                            {
                                repurposeCandidate = j;
                            }
                        }
                    }
                }

                if (!isAlreadyVirtualizedIn)
                {
                    // Repurpose the repurposeCandidate to represent this item
                    _containers[repurposeCandidate].SetItem(ItemsSource[i]);
                    _representingIndex[repurposeCandidate] = i;
                    _containers[repurposeCandidate].HorizontalOffset = i * (_size.Value.Width + ItemGutter);
                    _containers[repurposeCandidate].MakeVisible();

                    if (_displayedItemIndex >= 0 && _displayedItemIndex == i)
                    {
                        _displayedContainerIndex = repurposeCandidate;
                    }
                }
            }
        }

        private void UpdateItems(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    // Adjust the media strip for its new length
                    ResetGeometry();

                    // Update the RepresentingItemIndex for each VirtualizedItem that represents an item
                    // that follows where the new items were added
                    for (int i = 0; i < VirtualPoolSize; i++)
                    {
                        if (_representingIndex[i] >= e.NewStartingIndex)
                        {
                            _representingIndex[i] += e.NewItems.Count;
                        }
                    }

                    // Calculate new element index to display
                    if (_displayedItemIndex >= e.NewStartingIndex)
                    {
                        _displayedItemIndex += e.NewItems.Count;
                    }

                    UpdateVirtualizedItemPositions();
                    UpdateViewport();

                    break;
                case NotifyCollectionChangedAction.Remove:
                    // Adjust the media strip for its new length
                    ResetGeometry();

                    for (int i = 0; i < VirtualPoolSize; i++)
                    {
                        if (_representingIndex[i] >= e.OldStartingIndex && _representingIndex[i] < e.OldStartingIndex + e.OldItems.Count)
                        {
                            // This VirtualizedItem represented an item that was removed, disassociate it
                            _representingIndex[i] = -1;
                            _containers[i].SetItem(null);
                        }
                        else if (_representingIndex[i] > e.OldStartingIndex)
                        {
                            // This VirtualizedItem represents an item whose index was changed by this removal
                            _representingIndex[i] -= e.OldItems.Count;
                        }
                    }

                    // Calculate new element index to display
                    if (_displayedItemIndex > e.OldStartingIndex)
                    {
                        _displayedItemIndex -= e.OldItems.Count;
                        UpdateVirtualizedItemPositions();
                    }

                    UpdateViewport();

                    break;
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Reset:
                    {
                        // In these cases, do a full reset
                        if (_state == FlipperState.Uninitialized)
                        {
                            InitializeIfReady();
                        }
                        else
                        {
                            for (int i = 0; i < VirtualPoolSize; i++)
                            {
                                _containers[i].SetItem(null);
                                _representingIndex[i] = -1;
                            }
                            _displayedItemIndex = -1;
                            _displayedContainerIndex = -1;

                            ResetGeometry();
                            UpdateVirtualizedItemPositions();
                            UpdateViewport();
                        }
                    } break;
            }
        }

        private void UpdateViewport()
        {
            _rootTransform.TranslateX = -1 * Math.Max(0, _displayedItemIndex) * (_size.Value.Width + ItemGutter);
        }

        protected override void OnDoubleTap(GestureEventArgs e)
        {
            base.OnDoubleTap(e);

            if (_displayedItemIndex >= 0)
            {
                _containers[_displayedContainerIndex].OnDoubleTap();
            }
        }

        protected override void OnManipulationStarted(ManipulationStartedEventArgs e)
        {
            base.OnManipulationStarted(e);

            // If we were in the middle of an inertia animation, end it now and jump to its final position
            if (_state == FlipperState.InertiaAnimating)
            {
                CompleteDragInertiaAnimation();
            }
        }

        protected override void OnManipulationDelta(ManipulationDeltaEventArgs e)
        {
            base.OnManipulationDelta(e);

            if (e.PinchManipulation == null)
            {
                bool isZoomed = _displayedItemIndex >= 0 && _containers[_displayedContainerIndex].IsZoomedIn;
                if (!isZoomed && _state == FlipperState.Initialized && ItemsSource.Count > 0)
                {
                    _state = FlipperState.Dragging;
                    DragStartedEventHandler();
                }

                if (_state == FlipperState.Dragging || _state == FlipperState.DraggingAndSquishing)
                {
                    _dragState.GotDragDelta = true;
                    ProcessDragDelta(e.DeltaManipulation.Translation.X, e.DeltaManipulation.Translation.Y);
                }
            }
        }

        protected override void OnManipulationCompleted(ManipulationCompletedEventArgs e)
        {
            base.OnManipulationCompleted(e);

            if (_state == FlipperState.Dragging || _state == FlipperState.DraggingAndSquishing)
            {
                // NOTE: If the drag was fast enough that we didn't get any drag deltas reported,
                //       then these totals are the same as the first and only delta, so we will
                //       process them accordingly
                //
                if (_dragState.GotDragDelta == false)
                {
                    ProcessDragDelta(e.TotalManipulation.Translation.X, e.TotalManipulation.Translation.Y);
                }

                DragCompletedEventHandler();
            }
        }

        private void DragStartedEventHandler()
        {
            _state = FlipperState.Dragging;
            _dragState.LastDragUpdateTime = DateTime.Now;
            _dragState.DragStartingMediaStripOffset = _rootTransform.TranslateX;
            _dragState.NetDragDistanceSincleLastDragStagnation = 0.0;
            _dragState.IsDraggingFirstElement = _displayedItemIndex == 0;
            _dragState.IsDraggingLastElement = _displayedItemIndex == ItemsSource.Count - 1;
            _dragState.GotDragDelta = false;
        }

        private void DragCompletedEventHandler()
        {
            switch (_state)
            {
                case FlipperState.Dragging:
                    {
                        StartDragInertiaAnimation();
                    } break;
                case FlipperState.DraggingAndSquishing:
                    {
                        StartUndoSquishAnimation();
                    } break;
                default:
                    {
                        // Ignore
                    } break;
            }
        }

        private void ProcessDragDelta(double horizontalChange, double verticalChange)
        {
            // Do time calculations necessary to determine if the drag has stagnated or not.
            // This is important for inertia calculations.
            //
            DateTime currentTime = DateTime.Now;
            double millisecondsSinceLastDragUpdate = ((TimeSpan)(currentTime - _dragState.LastDragUpdateTime)).TotalMilliseconds;
            _dragState.LastDragUpdateTime = currentTime;
            if (millisecondsSinceLastDragUpdate > DragStagnationTimeThreshold)
            {
                _dragState.NetDragDistanceSincleLastDragStagnation = 0.0;
            }

            // Calculate new translation value
            //
            double newTranslation = 0;
            _dragState.LastDragDistanceDelta = horizontalChange;
            newTranslation = _rootTransform.TranslateX + horizontalChange;

            // Update NetDragDistanceSincleLastDragStagnation for stagnation detection
            //
            _dragState.NetDragDistanceSincleLastDragStagnation += horizontalChange;

            // Enforce dragging limits
            //
            newTranslation = Math.Min(newTranslation, _dragState.MinDraggingBoundary);
            newTranslation = Math.Max(newTranslation, _dragState.MaxDraggingBoundary);

            // Possibly do squish animation if we're dragging the first or last element
            //
            if ((_dragState.IsDraggingFirstElement) || (_dragState.IsDraggingLastElement))
            {
                HandleSquishingWhileDragging(newTranslation);
            }

            // Apply the new translation
            //
            _rootTransform.TranslateX = newTranslation;
        }

        private void ConstructDragInertiaAnimation(double animationEndingValue, TimeSpan animationDuration)
        {
            _dragInertiaAnimation = new Storyboard();

            _dragInertiaAnimationTranslation = new DoubleAnimation();
            Storyboard.SetTarget(_dragInertiaAnimationTranslation, _rootTransform);
            Storyboard.SetTargetProperty(_dragInertiaAnimationTranslation, new PropertyPath(CompositeTransform.TranslateXProperty));

            _dragInertiaAnimationTranslation.From = _rootTransform.TranslateX;
            _dragInertiaAnimationTranslation.To = animationEndingValue;
            _dragInertiaAnimationTranslation.Duration = animationDuration;
            _dragInertiaAnimationTranslation.EasingFunction = new PowerEase() { EasingMode = EasingMode.EaseOut, Power = 11 }; ;

            _dragInertiaAnimation.Children.Add(_dragInertiaAnimationTranslation);
            _dragInertiaAnimation.Completed += DragInertiaAnimationComplete;
            _dragInertiaAnimation.FillBehavior = FillBehavior.HoldEnd;
        }

        private int CalculateDragInertiaAnimationEndingValue()
        {
            if (Math.Abs(_dragState.NetDragDistanceSincleLastDragStagnation) > DragStagnationDistanceThreshold)
            {
                if (_dragState.LastDragDistanceDelta > 0 && _displayedItemIndex == 0)
                {
                    return 0;
                }
                else if (_dragState.LastDragDistanceDelta < 0 && _displayedItemIndex == ItemsSource.Count - 1)
                {
                    return 0;
                }
                else
                {
                    return -1 * Math.Sign(_dragState.LastDragDistanceDelta);
                }
            }

            return 0;
        }

        private TimeSpan CalculateDragInertiaAnimationDuration(TimeSpan lastDragTimeDelta)
        {
            double actualVelocity = Math.Abs(_dragState.LastDragDistanceDelta / lastDragTimeDelta.TotalMilliseconds);
            actualVelocity = Math.Min(FlickMaxInputVelocity, actualVelocity);
            actualVelocity = Math.Max(FlickMinInputVelocity, actualVelocity);
            double velocityPercentage = (actualVelocity - FlickMinInputVelocity) / (FlickMaxInputVelocity - FlickMinInputVelocity);

            int milliSeconds = (int)((FlickMaxOutputMilliseconds - FlickMinOutputMilliseconds) * (1 - velocityPercentage) + FlickMinOutputMilliseconds);

            milliSeconds = Math.Min((int)FlickMaxOutputMilliseconds, milliSeconds);
            milliSeconds = Math.Max((int)FlickMinOutputMilliseconds, milliSeconds);

            return new TimeSpan(0, 0, 0, 0, milliSeconds);
        }

        private void StartDragInertiaAnimation()
        {
            TimeSpan lastDragTimeDelta = DateTime.Now - _dragState.LastDragUpdateTime;

            // Build animation to finish the drag
            //
            int elementIndexDelta = CalculateDragInertiaAnimationEndingValue();
            TimeSpan animationDuration = CalculateDragInertiaAnimationDuration(lastDragTimeDelta);

            AnimateToElement(_displayedItemIndex + elementIndexDelta, animationDuration);

            _state = FlipperState.InertiaAnimating;
        }

        private void AnimateToElement(int elementIndex, TimeSpan animationDuration)
        {
            double animationEndingValue = -1 * elementIndex * (_size.Value.Width + ItemGutter);

            ConstructDragInertiaAnimation(animationEndingValue, animationDuration);
            _state = FlipperState.InertiaAnimating;
            _dragInertiaAnimation.Begin();

            _dragState.NewDisplayedElementIndex = elementIndex;
        }

        private void DragInertiaAnimationComplete(object sender, EventArgs e)
        {
            CompleteDragInertiaAnimation();
        }

        private void CompleteDragInertiaAnimation()
        {
            if (_dragInertiaAnimation != null)
            {
                if (_state == FlipperState.InertiaAnimating)
                {
                    _state = FlipperState.Initialized;
                }

                _rootTransform.TranslateX = _dragInertiaAnimationTranslation.To.Value;

                _dragInertiaAnimation.Stop();
                _dragInertiaAnimation = null;
                _dragInertiaAnimationTranslation = null;

                _displayedItemIndex = _dragState.NewDisplayedElementIndex;
                UpdateVirtualizedItemPositions();
                UpdateViewport();
            }
        }

        private void HandleSquishingWhileDragging(double newTranslation)
        {
            double translationOfLastItem = -1 * (ItemsSource.Count - 1) * (_size.Value.Width + ItemGutter);
            double squishDistance = 0;

            if (newTranslation > 0)
            {
                // We're squishing the first item
                //
                squishDistance = newTranslation;
                _dragState.UnsquishTranslationAnimationTarget = 0;
                _rootCanvas.RenderTransformOrigin = new Point(0, 0);
            }
            else if (newTranslation < translationOfLastItem)
            {
                // We're squishing the last item
                //
                squishDistance = translationOfLastItem - newTranslation;
                _dragState.UnsquishTranslationAnimationTarget = translationOfLastItem;
                _rootCanvas.RenderTransformOrigin = new Point(1, 0);
            }

            double squishScale = 1.0 - (squishDistance / MaxDraggingSquishDistance) * (1 - MinDraggingSquishScale);

            // Apply the squish
            //
            _rootTransform.ScaleX = squishScale;

            // Update our state
            //
            _state = squishScale == 1.0 ? FlipperState.Dragging : FlipperState.DraggingAndSquishing;
        }

        private void StartUndoSquishAnimation()
        {
            // Build animation to undo squish
            //
            _unsquishAnimation = new Storyboard();
            DoubleAnimation scaleAnimation = new DoubleAnimation();
            _unsquishAnimationTranslation = new DoubleAnimation();
            Storyboard.SetTarget(scaleAnimation, _rootTransform);
            Storyboard.SetTarget(_unsquishAnimationTranslation, _rootTransform);

            Storyboard.SetTargetProperty(scaleAnimation, new PropertyPath(CompositeTransform.ScaleXProperty));
            Storyboard.SetTargetProperty(_unsquishAnimationTranslation, new PropertyPath(CompositeTransform.TranslateXProperty));
            scaleAnimation.From = _rootTransform.ScaleX;
            _unsquishAnimationTranslation.From = _rootTransform.TranslateX;

            scaleAnimation.To = 1.0;
            _unsquishAnimationTranslation.To = _dragState.UnsquishTranslationAnimationTarget;
            scaleAnimation.Duration = new TimeSpan(0, 0, 0, 0, UnsquishAnimationMilliseconds);
            _unsquishAnimationTranslation.Duration = scaleAnimation.Duration;

            _unsquishAnimation.Children.Add(scaleAnimation);
            _unsquishAnimation.Children.Add(_unsquishAnimationTranslation);
            _unsquishAnimation.FillBehavior = FillBehavior.Stop;
            _unsquishAnimation.Completed += UnsquishAnimationComplete;
            _state = FlipperState.UnsquishAnimating;
            _unsquishAnimation.Begin();

            // Go ahead and set the values we're animating to their final values so when the storyboard ends, these will take effect
            //
            _rootTransform.ScaleX = scaleAnimation.To.Value;
            _rootTransform.TranslateX = _unsquishAnimationTranslation.To.Value;
        }

        private void UnsquishAnimationComplete(object sender, EventArgs e)
        {
            if (_state == FlipperState.UnsquishAnimating)
            {
                _state = FlipperState.Initialized;
            }

            _rootTransform.TranslateX = _unsquishAnimationTranslation.To.Value;

            _unsquishAnimation.Stop();
            _unsquishAnimation = null;
            _unsquishAnimationTranslation = null;
        }
    }
}
