using _4charm.Converters;
/* 
    Copyright (c) 2012 Microsoft Corporation.  All rights reserved.
    Use of this sample source code is subject to the terms of the Microsoft license 
    agreement under which you licensed this sample source code and is provided AS-IS.
    If you did not accept the terms of the license agreement, you are not authorized 
    to use this sample source code.  For the terms of the license, please see the 
    license agreement between you and Microsoft.
  
    To see all Code Samples for Windows Phone, visit http://go.microsoft.com/fwlink/?LinkID=219604 
  
*/
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Microsoft.Phone.Controls
{
    /// <summary>
    /// Represents a virtualized item in a MediaViewer.  It knows which
    /// index it represents in the MediaViewer.Items collection, it holds
    /// the FrameworkElement reference for the ItemTemplate instance, and
    /// it handles zooming the item.
    /// </summary>
    internal class VirtualizedItem
    {
        private const double maxTextureSideLength = 4096;
      
        private FrameworkElement _zoomableContent;
        private ProgressBar _progress;
        private ScaleTransform _contentTransform;
        private Point _lastOrigin;

        /// <summary>
        /// Stores the current zoom ratio, where 1.0 is zoomed all the way out to fit within the control.
        /// NOTE: this is not updated during a pinch operation, but serves as the "settled" zoom ratio
        /// that is only updated after a pinch is complete.
        /// </summary>
        private double _currentZoomRatio = 1.0;
        /// <summary>
        /// The temporary zoom ratio used during a pinch operation.
        /// </summary>
        private double _zoomRatioDuringPinch;
        
        private FrameworkElement _rootFrameworkElement;
        private DataTemplate _dataTemplate;
        private Size _size;
        private ViewportControl _viewport;
        private Canvas _canvas;
        /// <summary>
        /// Since we set a bounding box on the content and let it fill that box as much as possible, the actual size of the content is likely smaller.
        /// This stores the actual size of the content that has been rendered.
        /// </summary>
        private Size _actualContentSize = new Size(0,0);

        private bool _pinchInProgress;
        private bool _centerAtNextOpportunity = false;
        private Point _pinchMidpointInControlCoordinates;
        private Point _pinchMidpointInPercentOfContent;

        /// <summary>
        /// The index in the MediaViewer.Items collection that this instance
        /// represents
        /// </summary>
        public int? RepresentingItemIndex { get; set; }

        /// <summary>
        /// Raised when an item is zoomed in.
        /// </summary>
        public event Action<int?> ItemZoomed;
        /// <summary>
        /// Raised when an item is zoomed out to its original size.
        /// </summary>
        public event Action<int?> ItemUnzoomed;

        /// <summary>
        /// Creates a VirtualizedItem of a particular presentation Size.
        /// </summary>
        /// <param name="size">Initial Size for the item.</param>
        public VirtualizedItem(Size size)
        {
            _size = size;
        }

        public void Unload()
        {
            ((ThumbnailedImageViewer)_zoomableContent).ReleaseImages();
        }

        /// <summary>
        /// The DataTemplate that should be instantiated to represent an item.
        /// </summary>
        public DataTemplate DataTemplate
        {
            get
            {
                return _dataTemplate;
            }
            set
            {
                _dataTemplate = value;

                _rootFrameworkElement = (FrameworkElement)_dataTemplate.LoadContent();
                _rootFrameworkElement.Visibility = Visibility.Collapsed;
                _rootFrameworkElement.Height = _size.Height;
                _rootFrameworkElement.Width = _size.Width;

                _progress = (ProgressBar)_rootFrameworkElement.FindName("Progress");
                _zoomableContent = (FrameworkElement)_rootFrameworkElement.FindName("ZoomableContent");
                _contentTransform = (ScaleTransform)_rootFrameworkElement.FindName("ZoomableContentTransform");
                _viewport = (ViewportControl)_rootFrameworkElement.FindName("Viewport");
                _canvas = (Canvas)_rootFrameworkElement.FindName("Canvas");

                _zoomableContent.ManipulationStarted += OnManipulationStarted;
                _zoomableContent.ManipulationDelta += OnManipulationDelta;
                _zoomableContent.ManipulationCompleted += OnManipulationCompleted;
                _zoomableContent.LayoutUpdated += OnZoomableContentLayoutUpdated;

                _progress.SetBinding(ProgressBar.ValueProperty, new System.Windows.Data.Binding("Progress") { Source = _zoomableContent });
                _progress.SetBinding(ProgressBar.VisibilityProperty, new System.Windows.Data.Binding("Progress") {
                    Source = _zoomableContent,
                    Converter = new UnloadedVisibleConverter()
                });
            }
        }

        /// <summary>
        /// The root FrameworkElement of the item.  Exposed so the MediaViewer
        /// can position it on the parent Canvas.
        /// </summary>
        public FrameworkElement RootFrameworkElement
        {
            get
            {
                return _rootFrameworkElement;
            }
        }

        /// <summary>
        /// The Item to be virtualized is assigned to this property.
        /// </summary>
        public object DataContext
        {
            get
            {
                return _rootFrameworkElement.DataContext;
            }
            set
            {
                _rootFrameworkElement.DataContext = value;
                ZoomAllTheWayOut();
            }
        }

        /// <summary>
        /// The render Size available for this virtualized item.
        /// </summary>
        public Size Size
        {
            get
            {
                return _size;
            }
            set
            {
                _size = value;
                _rootFrameworkElement.Height = _size.Height;
                _rootFrameworkElement.Width = _size.Width;
                ZoomAllTheWayOut();
            }
        }

        /// <summary>
        /// Returns true if the item is zoomed in.
        /// </summary>
        public bool IsZoomedIn
        {
            get
            {
                return ((_currentZoomRatio > 1.0) ||
                        (_pinchInProgress));
            }
        }

        /// <summary>
        /// Processes a double tap performed on the virtualized item.
        /// </summary>
        public void DoubleTapped()
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

        public void Animate()
        {
            ((ThumbnailedImageViewer)_zoomableContent).Animate();
        }

        public void Unanimate()
        {
            ((ThumbnailedImageViewer)_zoomableContent).Unanimate();
        }

        /// <summary>
        /// Zooms out to the original Size.
        /// </summary>
        public void ZoomAllTheWayOut()
        {
            ItemUnzoomed(RepresentingItemIndex);
            _currentZoomRatio = 1.0;
            _centerAtNextOpportunity = true;
            ResizeContent();
        }

        private void ZoomInToDefaultLevel()
        {
            ItemZoomed(RepresentingItemIndex);
            _currentZoomRatio = 2.5;
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
                    ItemZoomed(RepresentingItemIndex);

                    Point center = e.PinchManipulation.Original.Center;
                    _pinchMidpointInPercentOfContent = new Point(center.X / _actualContentSize.Width, center.Y / _actualContentSize.Height);

                    var xform = _zoomableContent.TransformToVisual(_rootFrameworkElement);
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

            if (_currentZoomRatio == 1.0)
            {
                ItemUnzoomed(RepresentingItemIndex);
            }
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

            // Give the content a bounding box to fit within.
            //
            _zoomableContent.MaxWidth = newMaxWidth;
            _zoomableContent.MaxHeight = newMaxHeight;

            // Undo any scaling
            //
            _contentTransform.ScaleX = 1.0;
            _contentTransform.ScaleY = 1.0;
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

            _contentTransform.ScaleX = scaler;
            _contentTransform.ScaleY = scaler;

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
        void OnZoomableContentLayoutUpdated(object sender, EventArgs e)
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
                maxTextureSideLength / _size.Width,
                maxTextureSideLength / _size.Height);

            return Math.Min(maxZoomRatio, Math.Max(1.0, ratio));
        }
    }
}
