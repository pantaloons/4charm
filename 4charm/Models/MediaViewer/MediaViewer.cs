using _4charm.ViewModels;
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Microsoft.Phone.Controls
{
    public enum DisplayedElementType { None, Header, Item, Footer }
    public enum InitiallyDisplayedElementType { First, Last }

    /// <summary>
    /// Displays a virtualized list of Items, allowing the user to swipe 
    /// from item to item.  An optional header and optional footer may
    /// also be displayed.  The user can also pinch zoom into and back 
    /// out of items.
    /// </summary>
    [TemplatePart(Name = "MediaStrip", Type = typeof(Canvas))]
    [TemplatePart(Name = "MediaStripCompositeTransform", Type = typeof(CompositeTransform))]
    public class MediaViewer : Control
    {
        /// <summary>
        /// The number of items to load at a time.  Must be odd.
        /// </summary>
        private const int _virtualizedItemPoolSize = 3;
        
        /// <summary>
        /// The amount of space between items.  For full-screen items this will only be visible when scrolling from
        /// one item to another.
        /// </summary>
        private const double _itemGutter = 18;

        /// <summary>
        /// How many pixels past the beginning or end of the list the user will be allowed to drag.
        /// Note that during a drag past the beginning or end of the list the user will see the "squish" animation.
        /// </summary>
        private const double _maxDraggingSquishDistance = 150;

        /// <summary>
        /// How much to squish the UI if you drag maxDraggingSquishDistance past the beginning or end of the UI.
        /// </summary>
        private const double _minDraggingSquishScale = 0.90;

        /// <summary>
        /// How long the unsquish animation should take
        /// </summary>
        private const int _unsquishAnimationMilliseconds = 100;

        /// <summary>
        /// How long should a pause in dragging be before it resets the inertia calculation?  This is in milliseconds.
        /// </summary>
        private const double _dragStagnationTimeThreshold = 300;

        /// <summary>
        /// Tolerance in pixels for considering a drag stopped
        /// </summary>
        private const double _dragStagnationDistanceThreshold = 15;

        // These constants define how fast the inertia animation will run after a flick.  The actual flick speed 
        // (in pixels / ms) is mapped in the range _flickMinInputVelocity, _flickMaxInputVelocity, and an inertia
        // animation duration is then calculated as the same proportion of _flickMinOutputMilliseconds, 
        // _flickMaxOutputMilliseconds.
        private const double _flickMinInputVelocity = 0;
        private const double _flickMaxInputVelocity = 5;
        private const double _flickMinOutputMilliseconds = 100;
        private const double _flickMaxOutputMilliseconds = 800;
        
        private enum MediaViewerState 
        { 
            Uninitialized, 
            Initialized, 
            InertiaAnimating, 
            Dragging, 
            DraggingAndSquishing, 
            Pinching, 
            UnsquishAnimating 
        }

        private CompositeTransform _mediaStripCompositeTransform;
        private MediaViewerState _state = MediaViewerState.Uninitialized;
        private List<VirtualizedItem> _virtualizedItemPool;
        private Canvas _mediaStrip;
        private Size? _size = null;
        private DragState _dragState = new DragState(_maxDraggingSquishDistance);
        private double ScrollOffset
        {
            get
            {
                return _mediaStripCompositeTransform.TranslateX;
            }
            set
            {
                _mediaStripCompositeTransform.TranslateX = value;
            }
        }
        private Storyboard _dragInertiaAnimation;
        private Storyboard _unsquishAnimation;
        private DoubleAnimation _dragInertiaAnimationTranslation;
        private DoubleAnimation _unsquishAnimationTranslation;
        private VirtualizedItem _displayedVirtualizedItem;
        private FrameworkElement _headerTemplateInstance;
        private FrameworkElement _footerTemplateInstance;
        private FrameworkElement _headerOnMediaStrip;
        private FrameworkElement _footerOnMediaStrip;

        /// <summary>
        /// The index of the displayed element.  An element can be the header, an item, or the footer.
        /// If there is no header, footer, or items, this value is null;
        /// </summary>
        private int? _displayedElementIndex = null;

        #region Public Properties

        public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(
            "ItemTemplate", 
            typeof(DataTemplate), 
            typeof(MediaViewer), 
            new PropertyMetadata(null, OnItemTemplatePropertyChanged));

        /// <summary>
        /// The DataTemplate used to represent each virtualized item.
        /// </summary>
        public DataTemplate ItemTemplate
        {
            get
            {
                return (DataTemplate)GetValue(ItemTemplateProperty);
            }
            set
            {
                SetValue(ItemTemplateProperty, value);
            }
        }

        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
            "Items", 
            typeof(ObservableCollection<PostViewModel>), 
            typeof(MediaViewer),
            new PropertyMetadata(new PropertyChangedCallback(OnItemsPropertyChanged)));

        /// <summary>
        /// The collection of items to display in a virtualized fashion.
        /// </summary>
        public ObservableCollection<PostViewModel> Items
        {
            get { return (ObservableCollection<PostViewModel>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        public static readonly DependencyProperty DisplayedElementProperty = DependencyProperty.Register(
            "DisplayedElement",
            typeof(DisplayedElementType),
            typeof(MediaViewer),
            new PropertyMetadata(DisplayedElementType.None));

        /// <summary>
        /// Indicates which type of element is displayed (e.g. Header, Item, Footer).
        /// </summary>
        public DisplayedElementType DisplayedElement
        {
            get { return (DisplayedElementType)GetValue(DisplayedElementProperty); }
            private set { SetValue(DisplayedElementProperty, value); }
        }
        
        public static readonly DependencyProperty DisplayedItemIndexProperty = DependencyProperty.Register(
            "DisplayedItemIndex",
            typeof(int),
            typeof(MediaViewer),
            new PropertyMetadata(null));

        /// <summary>
        /// Index of the currently displayed Item, if any.
        /// </summary>
        public int DisplayedItemIndex
        {
            get { return (int)GetValue(DisplayedItemIndexProperty); }
            private set { SetValue(DisplayedItemIndexProperty, value); }
        }

        public static readonly DependencyProperty InitiallyDisplayedElementProperty = DependencyProperty.Register(
            "InitiallyDisplayedElement",
            typeof(InitiallyDisplayedElementType),
            typeof(MediaViewer),
            new PropertyMetadata(InitiallyDisplayedElementType.First, OnInitiallyDisplayedElementPropertyChanged));

        /// <summary>
        /// Indicates which element should be displayed initially - the first or last one.
        /// </summary>
        public InitiallyDisplayedElementType InitiallyDisplayedElement
        {
            get { return (InitiallyDisplayedElementType)GetValue(InitiallyDisplayedElementProperty); }
            set { SetValue(InitiallyDisplayedElementProperty, value); }
        }

        public static readonly DependencyProperty HeaderVisibilityProperty = DependencyProperty.Register(
            "HeaderVisibility", 
            typeof(Visibility), 
            typeof(MediaViewer), 
            new PropertyMetadata(Visibility.Collapsed, OnHeaderVisibilityPropertyChanged));

        /// <summary>
        /// Shows or hides the optional header.
        /// </summary>
        public Visibility HeaderVisibility
        {
            get { return (Visibility)GetValue(HeaderVisibilityProperty); }
            set { SetValue(HeaderVisibilityProperty, value); }
        }

        public static readonly DependencyProperty FooterVisibilityProperty = DependencyProperty.Register(
            "FooterVisibility", 
            typeof(Visibility), 
            typeof(MediaViewer), 
            new PropertyMetadata(Visibility.Collapsed, OnFooterVisibilityPropertyChanged));

        /// <summary>
        /// Shows or hides the optional footer.
        /// </summary>
        public Visibility FooterVisibility
        {
            get { return (Visibility)GetValue(FooterVisibilityProperty); }
            set { SetValue(FooterVisibilityProperty, value); }
        }

        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            "Header",
            typeof(FrameworkElement),
            typeof(MediaViewer),
            new PropertyMetadata(null, OnHeaderPropertyChanged));

        /// <summary>
        /// The root FrameworkElement of the Header.
        /// </summary>
        public FrameworkElement Header
        {
            get { return (FrameworkElement)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        public static readonly DependencyProperty FooterProperty = DependencyProperty.Register(
            "Footer",
            typeof(FrameworkElement),
            typeof(MediaViewer),
            new PropertyMetadata(null, OnFooterPropertyChanged));

        /// <summary>
        /// The root FrameworkElement of the Footer.
        /// </summary>
        public FrameworkElement Footer
        {
            get { return (FrameworkElement)GetValue(FooterProperty); }
            set { SetValue(FooterProperty, value); }
        }

        public static readonly DependencyProperty HeaderTemplateProperty = DependencyProperty.Register(
            "HeaderTemplate", 
            typeof(DataTemplate), 
            typeof(MediaViewer), 
            new PropertyMetadata(new PropertyChangedCallback(HeaderTemplateChangedEventHandler)));

        /// <summary>
        /// The DataTemplate used to render the header.
        /// </summary>
        public DataTemplate HeaderTemplate
        {
            get
            {
                return (DataTemplate)GetValue(HeaderTemplateProperty);
            }
            set
            {
                SetValue(HeaderTemplateProperty, value);
            }
        }

        public static readonly DependencyProperty FooterTemplateProperty = DependencyProperty.Register(
            "FooterTemplate", 
            typeof(DataTemplate), 
            typeof(MediaViewer), 
            new PropertyMetadata(new PropertyChangedCallback(FooterTemplateChangedEventHandler)));

        /// <summary>
        /// The DataTemplate used to render the footer.
        /// </summary>
        public DataTemplate FooterTemplate
        {
            get
            {
                return (DataTemplate)GetValue(FooterTemplateProperty);
            }
            set
            {
                SetValue(FooterTemplateProperty, value);
            }
        }

        public static readonly DependencyProperty DragEnabledProperty = DependencyProperty.Register(
            "DragEnabled",
            typeof(bool),
            typeof(MediaViewer),
            new PropertyMetadata(true));

        /// <summary>
        /// Enables or disables dragging by the user.
        /// </summary>
        public bool DragEnabled
        {
            get { return (bool)GetValue(DragEnabledProperty); }
            set { SetValue(DragEnabledProperty, value); }
        }

        #endregion

        #region Public Events

        /// <summary>
        /// Raised when the header is displayed.
        /// </summary>
        public event EventHandler HeaderDisplayed;
        /// <summary>
        /// Raised when an item is displayed.
        /// </summary>
        public event EventHandler<ItemDisplayedEventArgs> ItemDisplayed;
        /// <summary>
        /// Raised when the footer is displayed.
        /// </summary>
        public event EventHandler FooterDisplayed;
        /// <summary>
        /// Raised when the currently displayed item is zoomed in.
        /// </summary>
        public event EventHandler ItemZoomed;
        /// <summary>
        /// Raised when the currently displayed item is zoomed back out to nuetral.
        /// </summary>
        public event EventHandler ItemUnzoomed;

        #endregion

        public MediaViewer()
        {
            System.Diagnostics.Debug.Assert(_virtualizedItemPoolSize % 2 == 1);

            DefaultStyleKey = typeof(MediaViewer);
            SizeChanged += OnMediaViewerSizeChanged;
        }

        public void Unload()
        {
            foreach (VirtualizedItem virtualizedItem in this._virtualizedItemPool)
            {
                virtualizedItem.Unload();
            }
            if (_currentItems != null) _currentItems.CollectionChanged -= OnItemsCollectionChanged;
        }

        #region Property Changed Event Handlers

        private static void OnHeaderVisibilityPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            MediaViewer mediaViewer = (MediaViewer)dependencyObject;
            mediaViewer.InitializeOrReset();
        }

        private static void OnFooterVisibilityPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            MediaViewer mediaViewer = (MediaViewer)dependencyObject;
            mediaViewer.InitializeOrReset();
        }

        
        private static void OnItemsPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            MediaViewer thisMediaViewer = (MediaViewer)dependencyObject;

            ObservableCollection<PostViewModel> oldItems = eventArgs.OldValue as ObservableCollection<PostViewModel>;
            ObservableCollection<PostViewModel> newItems = eventArgs.NewValue as ObservableCollection<PostViewModel>;

            if (oldItems != null)
            {
                oldItems.CollectionChanged -= thisMediaViewer.OnItemsCollectionChanged;
            }

            if (newItems != null)
            {
                newItems.CollectionChanged += thisMediaViewer.OnItemsCollectionChanged;
                thisMediaViewer.SetCollection(newItems);
            }

            thisMediaViewer.InitializeOrReset();
        }

        private ObservableCollection<PostViewModel> _currentItems = null;
        private void SetCollection(ObservableCollection<PostViewModel> newItems)
        {
            _currentItems = newItems;
        }

        private void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        int numberAdded = e.NewItems.Count;
 
                        // Adjust the media strip for its new length
                        //
                        ResetMediaStripGeometry();

                        // We always need to update the footer's location if items are added
                        //
                        PlaceFooter();

                        // Update the RepresentingItemIndex for each VirtualizedItem that represents an item
                        // that follows where the new items were added
                        //
                        foreach (VirtualizedItem virtualizedItem in _virtualizedItemPool)
                        {
                            if ((virtualizedItem.RepresentingItemIndex != null) &&
                                (virtualizedItem.RepresentingItemIndex >= e.NewStartingIndex))
                            {
                                virtualizedItem.RepresentingItemIndex += numberAdded;
                            }
                        }

                        if (_displayedElementIndex == null)
                        {
                            JumpToElement(0);
                        }
                        else
                        {
                            if (_displayedElementIndex.Value >= e.NewStartingIndex)
                            {
                                // Jump to the element index that now represents what we were already viewing
                                //
                                JumpToElement(_displayedElementIndex.Value + numberAdded);
                            }
                            else if (_displayedElementIndex.Value == e.NewStartingIndex - 1)
                            {
                                UpdateVirtualizedItemPositions();
                            }
                        }
                    } break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        int numberRemoved = e.OldItems.Count;

                        // Adjust the media strip for its new length
                        //
                        ResetMediaStripGeometry();

                        // We always need to update the footer's location if items are removed
                        //
                        PlaceFooter();

                        foreach (VirtualizedItem virtualizedItem in _virtualizedItemPool)
                        {
                            if ((virtualizedItem.RepresentingItemIndex != null) &&
                                (virtualizedItem.RepresentingItemIndex >= e.OldStartingIndex) &&
                                (virtualizedItem.RepresentingItemIndex < e.OldStartingIndex + numberRemoved))
                            {
                                // This VirtualizedItem represented an item that was removed, disassociate it
                                //
                                virtualizedItem.RepresentingItemIndex = null;
                                virtualizedItem.DataContext = null;
                            }

                            if ((virtualizedItem.RepresentingItemIndex != null) &&
                                (virtualizedItem.RepresentingItemIndex > e.OldStartingIndex))
                            {
                                // This VirtualizedItem represents an item whose index was changed by this removal
                                //
                                virtualizedItem.RepresentingItemIndex -= numberRemoved;
                            }
                        }

                        if (_displayedElementIndex != null)
                        {
                            int newElementCount = (int)GetElementCount();

                            if (newElementCount == 0)
                            {
                                UpdateDisplayedElement(null);
                            }
                            else
                            {
                                // Calculate new element index to display
                                //
                                int newElementIndex = _displayedElementIndex.Value;
                                int elementIndexOfDeletedRange = HeaderVisibility == Visibility.Visible ? e.OldStartingIndex + 1 : e.OldStartingIndex;
                                if (_displayedElementIndex.Value > e.OldStartingIndex)
                                {
                                    int change = -1 * Math.Min(numberRemoved, (int)_displayedElementIndex.Value - elementIndexOfDeletedRange);
                                    newElementIndex += change;
                                }

                                // Ensure it's still a valid index
                                //
                                if (newElementIndex >= newElementCount)
                                {
                                    newElementIndex = newElementCount - 1;
                                }

                                JumpToElement(newElementIndex);
                            }
                        }
                    } break;
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Reset:
                    {
                        // In these cases, do a full reset
                        //
                        InitializeOrReset();
                    } break;
            }
        }

        private static void OnInitiallyDisplayedElementPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            MediaViewer thisMediaViewer = (MediaViewer)dependencyObject;

            if ((DisplayedElementType)eventArgs.NewValue == DisplayedElementType.None)
            {
                throw new ArgumentException("InitiallyDisplayedElement cannot be set to DisplayedElementType.None");
            }
        }

        private static void HeaderTemplateChangedEventHandler(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            MediaViewer mediaViewer = (MediaViewer)dependencyObject;
            DataTemplate newTemplate = (DataTemplate)eventArgs.NewValue;

            if (newTemplate != null)
            {
                mediaViewer._headerTemplateInstance = newTemplate.LoadContent() as FrameworkElement;
            }
            else
            {
                mediaViewer._headerTemplateInstance = null;
            }

            if (mediaViewer._state == MediaViewerState.Initialized)
            {
                mediaViewer.PlaceHeader();
            }
        }

        private static void FooterTemplateChangedEventHandler(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            MediaViewer mediaViewer = (MediaViewer)dependencyObject;
            DataTemplate newTemplate = (DataTemplate)eventArgs.NewValue;

            if (newTemplate != null)
            {
                mediaViewer._footerTemplateInstance = newTemplate.LoadContent() as FrameworkElement;
            }
            else
            {
                mediaViewer._footerTemplateInstance = null;
            }

            if (mediaViewer._state == MediaViewerState.Initialized)
            {
                mediaViewer.PlaceFooter();
            }
        }

        private static void OnHeaderPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            MediaViewer mediaViewer = (MediaViewer)dependencyObject;

            if (mediaViewer._state == MediaViewerState.Initialized)
            {
                mediaViewer.PlaceHeader();
            }
        }

        private static void OnFooterPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            MediaViewer mediaViewer = (MediaViewer)dependencyObject;

            if (mediaViewer._state == MediaViewerState.Initialized)
            {
                mediaViewer.PlaceFooter();
            }
        }

        private static void OnItemTemplatePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            MediaViewer mediaViewer = (MediaViewer)dependencyObject;
            mediaViewer.InitializeVirtualizationIfReady();
        }

        #endregion

        #region Virtualization

        /// <summary>
        /// Initializes virtualization if all of the prerequisites are available, so it's safe to call once each is set.
        /// </summary>
        private void InitializeVirtualizationIfReady()
        {
            if ((_mediaStrip == null) ||
                (_size == null))
            {
                // We don't have everything we need to initialize, wait for the next call
                return;
            }

            //
            // Initialize the virtualized item pool
            //
            _virtualizedItemPool = new List<VirtualizedItem>();
            _mediaStrip.Children.Clear();
            for (int index = 0; index < _virtualizedItemPoolSize; index++)
            {
                VirtualizedItem virtualizedItem = new VirtualizedItem(new Size(_size.Value.Width, _size.Value.Height));
                virtualizedItem.DataTemplate = ItemTemplate;
                virtualizedItem.ItemZoomed += OnItemZoomed;
                virtualizedItem.ItemUnzoomed += OnItemUnzoomed;
                _mediaStrip.Children.Add(virtualizedItem.RootFrameworkElement);
                _virtualizedItemPool.Add(virtualizedItem);
            }

            if (_state == MediaViewerState.Uninitialized)
            {
                _state = MediaViewerState.Initialized;
            }

            ResetDisplayedElement();

            ResetItemLayout();    
        }

        private void ResetDisplayedElement()
        {
            if ((HeaderVisibility == Visibility.Collapsed) &&
                (FooterVisibility == Visibility.Collapsed) &&
                ((Items == null) || (Items.Count == 0)))
            {
                ScrollOffset = 0;
                UpdateDisplayedElement(null);
            }
            else
            {
                if (InitiallyDisplayedElement == InitiallyDisplayedElementType.First)
                {
                    JumpToFirstElement();
                }
                else
                {
                    JumpToLastElement();
                }
            }
        }

        private void UpdateDisplayedElementPropertiesBasedOnIndex()
        {
            if (_displayedElementIndex == null)
            {
                DisplayedElement = DisplayedElementType.None;
                DisplayedItemIndex = -1;
            }
            else
            {
                if ((_displayedElementIndex == 0) && 
                    (HeaderVisibility == Visibility.Visible))
                {
                    DisplayedElement = DisplayedElementType.Header;
                    DisplayedItemIndex = -1;

                    var handler = HeaderDisplayed;
                    if (handler != null)
                    {
                        handler(this, EventArgs.Empty);
                    }
                }
                else if ((_displayedElementIndex == GetElementCount() - 1) &&
                         (FooterVisibility == Visibility.Visible))
                {
                    DisplayedElement = DisplayedElementType.Footer;
                    DisplayedItemIndex = -1;

                    var handler = FooterDisplayed;
                    if (handler != null)
                    {
                        handler(this, EventArgs.Empty);
                    }
                }
                else
                {
                    int index = _displayedElementIndex.Value;
                    if (HeaderVisibility == Visibility.Visible)
                    {
                        index--;
                    }

                    System.Diagnostics.Debug.Assert(index < Items.Count);

                    DisplayedElement = DisplayedElementType.Item;
                    DisplayedItemIndex = (int)index;

                    var handler = ItemDisplayed;
                    if (handler != null)
                    {
                        handler(this, new ItemDisplayedEventArgs(index));
                    }
                }
            }
        }

        private void PlaceHeader()
        {
            FrameworkElement effectiveHeader = null;

            if (Header is FrameworkElement)
            {
                effectiveHeader = Header;
            }
            else
            {
                effectiveHeader = _headerTemplateInstance;
                if ((effectiveHeader != null) &&
                    (Header != null))
                {
                    effectiveHeader.DataContext = Header;
                }
            }

            // If we currently have a different header on the MediaStrip, remove it
            //
            if ((_headerOnMediaStrip != null) &&
                (_headerOnMediaStrip != effectiveHeader))
            {
                _mediaStrip.Children.Remove(_headerOnMediaStrip);
                _headerOnMediaStrip = null;
            }

            if (HeaderVisibility == Visibility.Visible)
            {
                if (effectiveHeader != null)
                {
                    effectiveHeader.SetValue(Canvas.TopProperty, 0.0);
                    effectiveHeader.SetValue(Canvas.LeftProperty, 0.0);

                    effectiveHeader.Height = _size.Value.Height;
                    effectiveHeader.Width = _size.Value.Width;

                    if (_headerOnMediaStrip != effectiveHeader)
                    {
                        _mediaStrip.Children.Add(effectiveHeader);
                        _headerOnMediaStrip = effectiveHeader;
                    }
                }
            }
            else
            {
                if (_headerOnMediaStrip != null)
                {
                    _mediaStrip.Children.Remove(_headerOnMediaStrip);
                    _headerOnMediaStrip = null;
                }
            }
        }

        private void PlaceFooter()
        {
            FrameworkElement effectiveFooter = null;

            if (Footer is FrameworkElement)
            {
                effectiveFooter = Footer;
            }
            else
            {
                effectiveFooter = _footerTemplateInstance;
                if ((effectiveFooter != null) &&
                    (Footer != null))
                {
                    effectiveFooter.DataContext = Footer;
                }
            }

            // If we currently have a different Footer on the MediaStrip, remove it
            //
            if ((_footerOnMediaStrip != null) &&
                (_footerOnMediaStrip != effectiveFooter))
            {
                _mediaStrip.Children.Remove(_footerOnMediaStrip);
                _footerOnMediaStrip = null;
            }

            if (FooterVisibility == Visibility.Visible)
            {
                if (effectiveFooter != null)
                {
                    effectiveFooter.SetValue(Canvas.TopProperty, 0.0);
                    effectiveFooter.SetValue(Canvas.LeftProperty, _mediaStrip.Width - _size.Value.Width);

                    effectiveFooter.Height = _size.Value.Height;
                    effectiveFooter.Width = _size.Value.Width;

                    if (_footerOnMediaStrip != effectiveFooter)
                    {
                        _mediaStrip.Children.Add(effectiveFooter);
                        _footerOnMediaStrip = effectiveFooter;
                    }
                }
            }
            else
            {
                if (_footerOnMediaStrip != null)
                {
                    _mediaStrip.Children.Remove(_footerOnMediaStrip);
                    _footerOnMediaStrip = null;
                }
            }
        }

        private void ResetItemLayout()
        {
            if (_state == MediaViewerState.Uninitialized)
            {
                // Not ready to do this yet
                return;
            }

            ResetMediaStripGeometry();

            PlaceHeader();
            PlaceFooter();
            UpdateVirtualizedItemPositions();
        }

        private void UpdateVirtualizedItemSizes()
        {
            foreach (VirtualizedItem virtualizedItem in _virtualizedItemPool)
            {
                virtualizedItem.Size = _size.Value;
            }
        }
        
        private void UpdateVirtualizedItemPositions()
        {
            int? itemIndexToCenterOn = null;

            switch (DisplayedElement)
            {
                case DisplayedElementType.Header:
                    {
                        if ((Items != null) &&
                            (Items.Count > 0))
                        {
                            itemIndexToCenterOn = 0;
                        }
                        else
                        {
                            itemIndexToCenterOn = null;
                        }
                    } break;
                case DisplayedElementType.Item:
                    {
                        itemIndexToCenterOn = DisplayedItemIndex;
                    } break;
                case DisplayedElementType.Footer:
                    {
                        if ((Items != null) &&
                            (Items.Count > 0))
                        {
                            itemIndexToCenterOn = Items.Count - 1;
                        }
                        else
                        {
                            itemIndexToCenterOn = null;
                        }
                    } break;
            }

            if (itemIndexToCenterOn == null)
            {
                // There are valid cases where there are no items to virtualize in.  
                return;
            }

            // Calculate the range of indexes we want the virtualized items to represent
            int itemsToEitherSide = _virtualizedItemPoolSize / 2;
            int firstIndex = Math.Max(0, itemIndexToCenterOn.Value - itemsToEitherSide);
            int lastIndex = Math.Min(Items.Count - 1, itemIndexToCenterOn.Value + itemsToEitherSide);

            for (int index = firstIndex; index <= lastIndex; index++)
            {
                bool isAlreadyVirtualizedIn = false;
                double correctPosition = CalculateItemOffset(index);
                VirtualizedItem repurposeCandidate = null;
                
                // Check to see if this item index is already virtualized in
                foreach (VirtualizedItem virtualizedItem in _virtualizedItemPool)
                {
                    if ((DisplayedItemIndex != -1) &&
                        (virtualizedItem.RepresentingItemIndex == DisplayedItemIndex))
                    {
                        _displayedVirtualizedItem = virtualizedItem;
                    }

                    if (virtualizedItem.RepresentingItemIndex == index)
                    {
                        isAlreadyVirtualizedIn = true;

                        // Put it in the correct position if it isn't already there
                        if ((double)virtualizedItem.RootFrameworkElement.GetValue(Canvas.LeftProperty) != correctPosition)
                        {
                            virtualizedItem.RootFrameworkElement.SetValue(Canvas.LeftProperty, correctPosition);
                        }

                        break;
                    }
                    else
                    {
                        if ((repurposeCandidate == null) ||
                            (virtualizedItem.RepresentingItemIndex == null))
                        {
                            repurposeCandidate = virtualizedItem;
                        }
                        else if ((repurposeCandidate != null) &&
                                 (repurposeCandidate.RepresentingItemIndex != null))
                        {
                            // Look for the VirtualizedItem that is furthest from our itemIndexToCenterOn

                            int existingDistance = Math.Abs((int)repurposeCandidate.RepresentingItemIndex.Value - (int)itemIndexToCenterOn);
                            int thisDistance = Math.Abs((int)virtualizedItem.RepresentingItemIndex.Value - (int)itemIndexToCenterOn);

                            if (thisDistance > existingDistance)
                            {
                                repurposeCandidate = virtualizedItem;
                            }
                        }
                    }
                }

                if (!isAlreadyVirtualizedIn)
                {
                    // Repurpose the repurposeCandidate to represent this item
                    repurposeCandidate.DataContext = Items[(int)index];
                    repurposeCandidate.RepresentingItemIndex = index;
                    repurposeCandidate.RootFrameworkElement.SetValue(Canvas.LeftProperty, correctPosition);
                    repurposeCandidate.RootFrameworkElement.Visibility = System.Windows.Visibility.Visible;


                    if ((DisplayedItemIndex != -1) &&
                        (repurposeCandidate.RepresentingItemIndex == DisplayedItemIndex))
                    {
                        _displayedVirtualizedItem = repurposeCandidate;
                    }
                }
            }
        }

        private void ResetMediaStripGeometry()
        {
            if(_size == null || !_size.HasValue) return;

            _mediaStrip.Height = _size.Value.Height;
            
            int elementCount = GetElementCount();
            _mediaStrip.Width = Math.Max(0, (_size.Value.Width * elementCount) + (_itemGutter * (elementCount - 1)));

            _dragState.MinDraggingBoundary = _maxDraggingSquishDistance;
            _dragState.MaxDraggingBoundary = -1 * (_mediaStrip.Width - _size.Value.Width + _maxDraggingSquishDistance);
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Displays the header without animating to it.
        /// </summary>
        public bool JumpToHeader()
        {
            if (HeaderVisibility == Visibility.Collapsed)
            {
                return false;
            }

            JumpToFirstElement();
            return true;
        }

        /// <summary>
        /// Displays the footer without animating to it.
        /// </summary>
        public bool JumpToFooter()
        {
            if (FooterVisibility == Visibility.Collapsed)
            {
                return false;
            }

            JumpToLastElement();
            return true;
        }

        /// <summary>
        /// Displays an item without animating to it.
        /// </summary>
        public bool JumpToItem(int itemIndex)
        {
            if ((Items == null) ||
                (itemIndex >= Items.Count) ||
                (itemIndex < 0))
            {
                return false;
            }

            JumpToElement(HeaderVisibility == Visibility.Visible ? itemIndex + 1 : itemIndex);
            return true;
        }

        /// <summary>
        /// Animates to the element to the left of the currently displayed element.
        /// </summary>
        /// <returns></returns>
        public bool ScrollLeftOneElement()
        {
            if ((_displayedElementIndex == null) ||  
                (_displayedElementIndex == 0))
            {
                return false;
            }

            AnimateToElement(_displayedElementIndex.Value - 1, new TimeSpan(0, 0, 0, 0, (int)_flickMaxOutputMilliseconds));

            return true;
        }

        /// <summary>
        /// Finds an object in the header by name
        /// </summary>
        public object FindNameInHeader(string name)
        {
            if (this._headerOnMediaStrip == null)
            {
                return null;
            }
            return this._headerOnMediaStrip.FindName(name);
        }

        /// <summary>
        /// Finds an object in the footer by name
        /// </summary>
        public object FindNameInFooter(string name)
        {
            if (this._footerOnMediaStrip == null)
            {
                return null;
            }
            return this._footerOnMediaStrip.FindName(name);
        }

        #endregion

        #region Control events

        private void OnMediaViewerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _size = e.NewSize;

            if (_state == MediaViewerState.Uninitialized)
            {
                InitializeVirtualizationIfReady();
            }
            else
            {
                UpdateVirtualizedItemSizes();
                ResetItemLayout();
                if (_displayedElementIndex != null)
                {
                    ScrollOffset = -1 * CalculateElementOffset(_displayedElementIndex.Value);
                }
                else
                {
                    ScrollOffset = 0;
                }
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _mediaStrip = (Canvas)GetTemplateChild("MediaStrip");
            _mediaStripCompositeTransform = (CompositeTransform)GetTemplateChild("MediaStripCompositeTransform");

            InitializeVirtualizationIfReady();
        }

        #endregion

        #region User Interaction Events

        protected override void OnDoubleTap(System.Windows.Input.GestureEventArgs e)
        {
            base.OnDoubleTap(e);
            if (DisplayedElement == DisplayedElementType.Item)
            {
                _displayedVirtualizedItem.DoubleTapped();
            }
        }

        protected override void OnManipulationStarted(ManipulationStartedEventArgs e)
        {
            base.OnManipulationStarted(e);

            // If we were in the middle of an inertia animation, end it now and jump to its final position
            //
            if (_state == MediaViewerState.InertiaAnimating)
            {
                CompleteDragInertiaAnimation();
            }
        }

        protected override void OnManipulationDelta(ManipulationDeltaEventArgs e)
        {
            base.OnManipulationDelta(e);

            if (e.PinchManipulation == null)
            {
                if (!IsZoomedInToItem() &&
                    (_state == MediaViewerState.Initialized) &&
                    (DragEnabled) &&
                    (GetElementCount() > 0))
                {
                    _state = MediaViewerState.Dragging;
                    DragStartedEventHandler();
                }

                if ((_state == MediaViewerState.Dragging) ||
                    (_state == MediaViewerState.DraggingAndSquishing))
                {
                    DragDeltaEventHandler(e.DeltaManipulation.Translation.X, e.DeltaManipulation.Translation.Y);
                }
            }
        }

        protected override void OnManipulationCompleted(ManipulationCompletedEventArgs e)
        {
            base.OnManipulationCompleted(e);

            if ((_state == MediaViewerState.Dragging) ||
                (_state == MediaViewerState.DraggingAndSquishing))
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

        private void OnItemZoomed(int? representingItemIndex)
        {
            if ((_displayedElementIndex != null) &&
                (_displayedElementIndex == representingItemIndex))
            {
                var handler = ItemZoomed;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }
        }

        private void OnItemUnzoomed(int? representingItemIndex)
        {
            if ((_displayedElementIndex != null) &&
                (_displayedElementIndex == representingItemIndex))
            {
                var handler = ItemUnzoomed;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }
        }
        
        #endregion

        #region Drag handling
        
        private void DragStartedEventHandler()
        {
            //Tracing.Trace("DragStartedEventHandler() MediaStripOffset = " + offsetHelper.MediaStripOffset);
            int elementCount = GetElementCount();

            _state = MediaViewerState.Dragging;
            _dragState.LastDragUpdateTime = DateTime.Now;
            _dragState.DragStartingMediaStripOffset = ScrollOffset;
            _dragState.NetDragDistanceSincleLastDragStagnation = 0.0;
            _dragState.IsDraggingFirstElement = _displayedElementIndex == 0;
            _dragState.IsDraggingLastElement = _displayedElementIndex == elementCount - 1;
            _dragState.GotDragDelta = false;
        }

        private void DragDeltaEventHandler(double horizontalChange, double verticalChange)
        {
            _dragState.GotDragDelta = true;

            ProcessDragDelta(horizontalChange, verticalChange);
        }

        private void DragCompletedEventHandler()
        {
            switch (_state)
            {
                case MediaViewerState.Dragging:
                    {
                        StartDragInertiaAnimation();
                    } break;
                case MediaViewerState.DraggingAndSquishing:
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
            if (millisecondsSinceLastDragUpdate > _dragStagnationTimeThreshold)
            {
                _dragState.NetDragDistanceSincleLastDragStagnation = 0.0;
            }

            // Calculate new translation value
            //
            double newTranslation = 0;
            _dragState.LastDragDistanceDelta = horizontalChange;
            newTranslation = ScrollOffset + horizontalChange;

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
            ScrollOffset = newTranslation;
        }

        private void ConstructDragInertiaAnimation(double animationEndingValue, TimeSpan animationDuration)
        {
            _dragInertiaAnimation = new Storyboard();

            _dragInertiaAnimationTranslation = new DoubleAnimation();
            Storyboard.SetTarget(_dragInertiaAnimationTranslation, _mediaStripCompositeTransform);
            Storyboard.SetTargetProperty(_dragInertiaAnimationTranslation, new PropertyPath(CompositeTransform.TranslateXProperty));

            QuadraticEase easingFunction = new QuadraticEase();
            easingFunction.EasingMode = EasingMode.EaseOut;

            _dragInertiaAnimationTranslation.From = ScrollOffset;
            _dragInertiaAnimationTranslation.To = animationEndingValue;
            _dragInertiaAnimationTranslation.Duration = animationDuration;
            _dragInertiaAnimationTranslation.EasingFunction = easingFunction;

            _dragInertiaAnimation.Children.Add(_dragInertiaAnimationTranslation);
            _dragInertiaAnimation.Completed += DragInertiaAnimationComplete;
            _dragInertiaAnimation.FillBehavior = FillBehavior.HoldEnd;
        }

        private int CalculateDragInertiaAnimationEndingValue()
        {
            bool userStoppedDrag = (Math.Abs(_dragState.NetDragDistanceSincleLastDragStagnation) <= _dragStagnationDistanceThreshold);
            int elementIndexDelta = 0;

            if (userStoppedDrag == false)
            {
                elementIndexDelta = -1 * Math.Sign(_dragState.LastDragDistanceDelta);

                // Ensure we don't try to drag beyond either end of the list of elements
                //
                if ((_displayedElementIndex == 0) &&
                    (elementIndexDelta == -1))
                {
                    elementIndexDelta = 0;
                }
                else if ((_displayedElementIndex == GetElementCount() - 1) &&
                         (elementIndexDelta == 1))
                {
                    elementIndexDelta = 0;
                }
            }

            return elementIndexDelta;
        }

        private TimeSpan CalculateDragInertiaAnimationDuration(TimeSpan lastDragTimeDelta)
        {
            double actualVelocity = Math.Abs(_dragState.LastDragDistanceDelta / lastDragTimeDelta.TotalMilliseconds);
            actualVelocity = Math.Min(_flickMaxInputVelocity, actualVelocity);
            actualVelocity = Math.Max(_flickMinInputVelocity, actualVelocity);
            double velocityPercentage = (actualVelocity - _flickMinInputVelocity) / (_flickMaxInputVelocity - _flickMinInputVelocity);

            int milliSeconds = (int)((_flickMaxOutputMilliseconds - _flickMinOutputMilliseconds) * (1 - velocityPercentage) + _flickMinOutputMilliseconds);

            milliSeconds = Math.Min((int)_flickMaxOutputMilliseconds, milliSeconds);
            milliSeconds = Math.Max((int)_flickMinOutputMilliseconds, milliSeconds);

            return new TimeSpan(0, 0, 0, 0, milliSeconds);
        }

        private void StartDragInertiaAnimation()
        {
            TimeSpan lastDragTimeDelta = DateTime.Now - _dragState.LastDragUpdateTime;

            // Build animation to finish the drag
            //
            int elementIndexDelta = CalculateDragInertiaAnimationEndingValue();
            TimeSpan animationDuration = CalculateDragInertiaAnimationDuration(lastDragTimeDelta);

            AnimateToElement(_displayedElementIndex.Value + elementIndexDelta, animationDuration);

            _state = MediaViewerState.InertiaAnimating;
        }

        private void AnimateToElement(int elementIndex, TimeSpan animationDuration)
        {
            double animationEndingValue = -1 * CalculateElementOffset(elementIndex);

            ConstructDragInertiaAnimation(animationEndingValue, animationDuration);
            _state = MediaViewerState.InertiaAnimating;
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
                if (_state == MediaViewerState.InertiaAnimating)
                {
                    _state = MediaViewerState.Initialized;
                }

                ScrollOffset = _dragInertiaAnimationTranslation.To.Value;

                _dragInertiaAnimation.Stop();
                _dragInertiaAnimation = null;
                _dragInertiaAnimationTranslation = null;

                UpdateDisplayedElement(_dragState.NewDisplayedElementIndex);
            }
        }

        private void HandleSquishingWhileDragging(double newTranslation)
        {
            double translationOfLastItem = -1 * CalculateElementOffset(GetElementCount() - 1);
            double squishDistance = 0;

            if (newTranslation > 0)
            {
                // We're squishing the first item
                //
                squishDistance = newTranslation;
                _dragState.UnsquishTranslationAnimationTarget = 0;
                _mediaStrip.RenderTransformOrigin = new Point(0, 0);
            }
            else if (newTranslation < translationOfLastItem)
            {
                // We're squishing the last item
                //
                squishDistance = translationOfLastItem - newTranslation;
                _dragState.UnsquishTranslationAnimationTarget = translationOfLastItem;
                _mediaStrip.RenderTransformOrigin = new Point(1, 0);
            }

            double squishScale = 1.0 - (squishDistance / _maxDraggingSquishDistance) * (1 - _minDraggingSquishScale);

            // Apply the squish
            //
            _mediaStripCompositeTransform.ScaleX = squishScale;

            // Update our state
            //
            _state = squishScale == 1.0 ? MediaViewerState.Dragging : MediaViewerState.DraggingAndSquishing;
        }

        private void StartUndoSquishAnimation()
        {
            // Build animation to undo squish
            //
            _unsquishAnimation = new Storyboard();
            DoubleAnimation scaleAnimation = new DoubleAnimation();
            _unsquishAnimationTranslation = new DoubleAnimation();
            Storyboard.SetTarget(scaleAnimation, _mediaStripCompositeTransform);
            Storyboard.SetTarget(_unsquishAnimationTranslation, _mediaStripCompositeTransform);

            Storyboard.SetTargetProperty(scaleAnimation, new PropertyPath(CompositeTransform.ScaleXProperty));
            Storyboard.SetTargetProperty(_unsquishAnimationTranslation, new PropertyPath(CompositeTransform.TranslateXProperty));
            scaleAnimation.From = _mediaStripCompositeTransform.ScaleX;
            _unsquishAnimationTranslation.From = _mediaStripCompositeTransform.TranslateX;

            scaleAnimation.To = 1.0;
            _unsquishAnimationTranslation.To = _dragState.UnsquishTranslationAnimationTarget;
            scaleAnimation.Duration = new TimeSpan(0, 0, 0, 0, _unsquishAnimationMilliseconds);
            _unsquishAnimationTranslation.Duration = scaleAnimation.Duration;

            _unsquishAnimation.Children.Add(scaleAnimation);
            _unsquishAnimation.Children.Add(_unsquishAnimationTranslation);
            _unsquishAnimation.FillBehavior = FillBehavior.Stop;
            _unsquishAnimation.Completed += UnsquishAnimationComplete;
            _state = MediaViewerState.UnsquishAnimating;
            _unsquishAnimation.Begin();

            // Go ahead and set the values we're animating to their final values so when the storyboard ends, these will take effect
            //
            _mediaStripCompositeTransform.ScaleX = scaleAnimation.To.Value;
            _mediaStripCompositeTransform.TranslateX = _unsquishAnimationTranslation.To.Value;
        }

        private void UnsquishAnimationComplete(object sender, EventArgs e)
        {
            if (_state == MediaViewerState.UnsquishAnimating)
            {
                _state = MediaViewerState.Initialized;
            }

            ScrollOffset = _unsquishAnimationTranslation.To.Value;

            _unsquishAnimation.Stop();
            _unsquishAnimation = null;
            _unsquishAnimationTranslation = null;
        }

        #endregion

        #region Helper methods

        private int GetElementCount()
        {
            int elementCount = 0;
            if (HeaderVisibility == Visibility.Visible)
            {
                elementCount++;
            }
            if (Items != null)
            {
                elementCount += Items.Count;
            }
            if (FooterVisibility == Visibility.Visible)
            {
                elementCount++;
            }

            return elementCount;
        }

        private void UpdateDisplayedElement(int? newElementIndex)
        {
            if (DisplayedElement == DisplayedElementType.Item) _displayedVirtualizedItem.Unanimate();

            _displayedElementIndex = newElementIndex;
            UpdateDisplayedElementPropertiesBasedOnIndex();
            UpdateVirtualizedItemPositions();

            if (DisplayedElement == DisplayedElementType.Item) _displayedVirtualizedItem.Animate();
        }

        private double RoundOffsetDownToElementStart(double offset)
        {
            int index = (int)(offset / (_size.Value.Width + _itemGutter));

            return index * (_size.Value.Width + _itemGutter);
        }

        private double CalculateElementOffset(int elementIndex)
        {
            return elementIndex * (_size.Value.Width + _itemGutter);
        }

        private double CalculateItemOffset(int itemIndex)
        {
            double position = 0;

            if (HeaderVisibility == Visibility.Visible)
            {
                position += _size.Value.Width + _itemGutter;
            }

            position += itemIndex * (_size.Value.Width + _itemGutter);

            return position;
        }

        private bool IsZoomedInToItem()
        {
            return ((_displayedVirtualizedItem != null) &&
                    (_displayedVirtualizedItem.IsZoomedIn));
        }

        private void JumpToFirstElement()
        {
            JumpToElement(0);
        }

        private void JumpToLastElement()
        {
            int elementCount = GetElementCount();
            if (elementCount > 0)
            {
                JumpToElement(elementCount - 1);
            }
        }

        private void JumpToElement(int elementIndex)
        {
            // If we are zoomed into an item, unzoom it before jumping
            //
            if ((DisplayedElement == DisplayedElementType.Item) &&
                (_displayedVirtualizedItem.IsZoomedIn))
            {
                _displayedVirtualizedItem.ZoomAllTheWayOut();
            }

            ScrollOffset = -1 * CalculateElementOffset(elementIndex);
            UpdateDisplayedElement(elementIndex);
        }

        private void InitializeOrReset()
        {
            if (_state == MediaViewerState.Uninitialized)
            {
                InitializeVirtualizationIfReady();
            }
            else
            {
                // Clear out all of the VirtualizedItem assignments
                //
                foreach (VirtualizedItem virtualizedItem in this._virtualizedItemPool)
                {
                    virtualizedItem.DataContext = null;
                    virtualizedItem.RepresentingItemIndex = null;
                }

                ResetDisplayedElement();
                ResetItemLayout();
            }
        }

        #endregion
    }
}
