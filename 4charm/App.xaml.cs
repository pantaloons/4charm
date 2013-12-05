﻿using _4charm.Models;
using _4charm.Resources;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Navigation;

namespace _4charm
{
    public partial class App : Application
    {
        /// <summary>
        /// Because the tap events of the RichTextBox aren't routed, they can't be cancelled.
        /// We use this to mark "cancellation", and just have other tap targets check it first.
        /// </summary>
        public static bool IsPostTapAllowed { get; set; }

        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public static PhoneApplicationFrame RootFrame { get; private set; }

        /// <summary>
        /// Task representing when the first frame of the application has been
        /// rendered, and delay loaded visuals can be constructed.
        /// </summary>
        public static Task InitialFrameRenderedTask { get; private set; }
        private TaskCompletionSource<bool> _initialFrameTCS;

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            UnhandledException += Application_UnhandledException;

            InitializeComponent();
            InitializePhoneApplication();
            InitializeLanguage();

            _initialFrameTCS = new TaskCompletionSource<bool>();
            InitialFrameRenderedTask = _initialFrameTCS.Task;
            CompositionTarget.Rendering += CompositionTarget_Rendering;

            IsPostTapAllowed = true;

            if (Debugger.IsAttached)
            {
                Application.Current.Host.Settings.EnableFrameRateCounter = true;
                Application.Current.Host.Settings.EnableRedrawRegions = true;
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }
        }

#if DEBUG
        /// <summary>
        /// Memory debugging timer to print out current memory usage every 500ms.
        /// </summary>
        private static System.Threading.Timer t;
#endif
        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
#if DEBUG
            t = new System.Threading.Timer(state =>
            {
                System.Diagnostics.Debug.WriteLine(((long)Microsoft.Phone.Info.DeviceExtendedProperties.GetValue("ApplicationCurrentMemoryUsage") / (1024.0 * 1024)) + " MB");
            }, null, 0, 500);
#endif
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            _initialFrameTCS.SetResult(true);
            CompositionTarget.Rendering -= CompositionTarget_Rendering;
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            CriticalSettingsManager.Current.WaitForPartialWrites();
            TransitorySettingsManager.Current.WaitForPartialWrites();
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            CriticalSettingsManager.Current.WaitForPartialWrites();
            TransitorySettingsManager.Current.WaitForPartialWrites();
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
#if DEBUG
            if (Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                Debugger.Break();
            }
            MessageBox.Show(((long)Microsoft.Phone.Info.DeviceExtendedProperties.GetValue("ApplicationCurrentMemoryUsage") / (1024.0 * 1024)) + " MB");
            MessageBox.Show(e.ExceptionObject.Message);
            MessageBox.Show(e.ExceptionObject.StackTrace);
#endif
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;
            RootFrame.UriMapper = new Models.UriMapper();

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Handle reset requests for clearing the backstack
            RootFrame.Navigated += CheckForResetNavigation;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        private void CheckForResetNavigation(object sender, NavigationEventArgs e)
        {
            // If the app has received a 'reset' navigation, then we need to check
            // on the next navigation to see if the page stack should be reset
            if (e.NavigationMode == NavigationMode.Reset)
                RootFrame.Navigated += ClearBackStackAfterReset;
        }

        private void ClearBackStackAfterReset(object sender, NavigationEventArgs e)
        {
            // Unregister the event so it doesn't get called again
            RootFrame.Navigated -= ClearBackStackAfterReset;

            // Only clear the stack for 'new' (forward) and 'refresh' navigations
            if (e.NavigationMode != NavigationMode.New && e.NavigationMode != NavigationMode.Refresh)
                return;

            // For UI consistency, clear the entire page stack
            while (RootFrame.RemoveBackEntry() != null)
            {
                ; // do nothing
            }
        }

        #endregion

        // Initialize the app's font and flow direction as defined in its localized resource strings.
        //
        // To ensure that the font of your application is aligned with its supported languages and that the
        // FlowDirection for each of those languages follows its traditional direction, ResourceLanguage
        // and ResourceFlowDirection should be initialized in each resx file to match these values with that
        // file's culture. For example:
        //
        // AppResources.es-ES.resx
        //    ResourceLanguage's value should be "es-ES"
        //    ResourceFlowDirection's value should be "LeftToRight"
        //
        // AppResources.ar-SA.resx
        //     ResourceLanguage's value should be "ar-SA"
        //     ResourceFlowDirection's value should be "RightToLeft"
        //
        // For more info on localizing Windows Phone apps see http://go.microsoft.com/fwlink/?LinkId=262072.
        //
        private void InitializeLanguage()
        {
            try
            {
                // Set the font to match the display language defined by the
                // ResourceLanguage resource string for each supported language.
                //
                // Fall back to the font of the neutral language if the Display
                // language of the phone is not supported.
                //
                // If a compiler error is hit then ResourceLanguage is missing from
                // the resource file.
                RootFrame.Language = XmlLanguage.GetLanguage(AppResources.ResourceLanguage);

                // Set the FlowDirection of all elements under the root frame based
                // on the ResourceFlowDirection resource string for each
                // supported language.
                //
                // If a compiler error is hit then ResourceFlowDirection is missing from
                // the resource file.
                FlowDirection flow = (FlowDirection)Enum.Parse(typeof(FlowDirection), AppResources.ResourceFlowDirection);
                RootFrame.FlowDirection = flow;
            }
            catch
            {
                // If an exception is caught here it is most likely due to either
                // ResourceLangauge not being correctly set to a supported language
                // code or ResourceFlowDirection is set to a value other than LeftToRight
                // or RightToLeft.

                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                throw;
            }
        }
    }
}