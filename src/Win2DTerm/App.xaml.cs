using System;
using System.IO;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Core.Preview;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Win2DTerm
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        bool isBackPressedReady = false;
        bool isInitializedReady = false;
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            if (isInitializedReady)
            {
                return;
            }
            if (!isBackPressedReady)
            {
                SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;
                SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += OnCloseRequest;
                isBackPressedReady = true;
            }
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
            isInitializedReady = true;
        }

        protected override void OnFileActivated(FileActivatedEventArgs e)
        {
            try
            {
                if (!isBackPressedReady)
                {
                    SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;
                    SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += OnCloseRequest;
                    isBackPressedReady = true;
                }
                InitializeApp(e.PreviousExecutionState, false, null);

                var file = e.Files.First(d => d is IStorageFile);

                var FileExtention = Path.GetExtension(file.Name);
                if (FileExtention.Equals(".tprf"))
                {
                    MainPage.profileFile = (StorageFile)file;
                    if (MainPage.ProfileRequest != null)
                    {
                        MainPage.ProfileRequest.Invoke(null, EventArgs.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }
        private void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            try
            {
                if (MainPage._settingsPaneVisible && MainPage.HideSettingsHandler!=null)
                {
                    e.Handled = true;
                    MainPage.HideSettingsHandler.Invoke(null, EventArgs.Empty);
                }
            }catch(Exception ex)
            {

            }
        }
        private void InitializeApp(ApplicationExecutionState previousExecutionState, bool prelaunchActivated, string args)
        {
            if (isInitializedReady)
            {
                return;
            }
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (previousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window

                Grid rootGrid = Window.Current.Content as Grid;
                //Frame rootFrame = rootGrid?.Children.Where((c) => c is Frame).Cast<Frame>().FirstOrDefault();

                if (rootGrid == null)
                {
                    rootGrid = new Grid();

                    //var notificationGrid = new Grid();
                    //LocalNotificationManager = new LocalNotificationManager(notificationGrid);

                    rootGrid.Children.Add(rootFrame);
                    //rootGrid.Children.Add(notificationGrid);

                    Window.Current.Content = rootGrid;
                }
            }

            try
            {
                if (prelaunchActivated == false)
                {
                    if (rootFrame.Content == null)
                    {
                        // When the navigation stack isn't restored navigate to the first page,
                        // configuring the new page by passing required information as a navigation
                        // parameter
                        rootFrame.Navigate(typeof(MainPage), args);
                    }
                    // Ensure the current window is active
                    Window.Current.Activate();
                }
                isInitializedReady = true;
            }
            catch (Exception ex)
            {

            }
        }

        private async void OnCloseRequest(object sender, SystemNavigationCloseRequestedPreviewEventArgs e)
        {
            try
            {
                e.Handled = true;
                var messageDialog = new MessageDialog("Do you want to exit?");
                messageDialog.Commands.Add(new UICommand("Exit", new UICommandInvokedHandler(this.CommandInvokedHandler)));
                messageDialog.Commands.Add(new UICommand("Dismiss"));
                await messageDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                try
                {
                    if (MainPage.DisconnectRequest != null)
                    {
                        MainPage.DisconnectRequest.Invoke(null, EventArgs.Empty);
                    }
                }
                catch (Exception exx)
                {

                }
                CoreApplication.Exit();
            }
        }
        private void CommandInvokedHandler(IUICommand command)
        {
            try
            {
                if (MainPage.DisconnectRequest != null)
                {
                    MainPage.DisconnectRequest.Invoke(null, EventArgs.Empty);
                }
            }
            catch (Exception exx)
            {

            }
            // Display message showing the label of the command that was invoked
            CoreApplication.Exit();
        }
        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
