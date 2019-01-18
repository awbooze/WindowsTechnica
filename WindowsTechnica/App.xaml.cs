using Microsoft.QueryStringDotNET;
using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace WindowsTechnica
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
		private ApplicationDataContainer localSettings;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;

			// Get the theme from settings and change the app theme to the correct theme.
			localSettings = ApplicationData.Current.LocalSettings;
			if(localSettings.Values["Theme"] != null)
			{
				int theme = (int)localSettings.Values["Theme"];

				if (theme == 0)
				{
					// The default theme, so the app doesn't need to request it.
				}
				else if (theme == 1)
				{
					RequestedTheme = ApplicationTheme.Light;
				}
				else
				{
					RequestedTheme = ApplicationTheme.Dark;
				}
			}
			else
			{
				// Save the default theme so that it initializes correctly next time.
				localSettings.Values["Theme"] = 0;
			}
		}

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
			Frame rootFrame = OnLaunchedOrActivated(e);

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

			// Register a handler for BackRequested events and set the visibility of the Back button
			SystemNavigationManager systemNavigationManager = SystemNavigationManager.GetForCurrentView();
			systemNavigationManager.BackRequested += OnBackRequested;
			systemNavigationManager.AppViewBackButtonVisibility = rootFrame.CanGoBack ?
				AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;
		}

		/// <summary>
		/// Invoked when the application is launched by the end user through clicking a notification, link, or similar 
		/// event. This is not involked when the application is launched normally.
		/// </summary>
		/// <param name="e">Details about the activation request and process.</param>
		protected override void OnActivated(IActivatedEventArgs e)
		{
			Frame rootFrame = OnLaunchedOrActivated(e);

			// Handle toast activation
			if (e is ToastNotificationActivatedEventArgs)
			{
				var toastActivationArgs = e as ToastNotificationActivatedEventArgs;

				// Parse the query string (using QueryString.NET)
				QueryString args = QueryString.Parse(toastActivationArgs.Argument);

				// See what action is being requested 
				switch (args["action"])
				{
					case "viewSettings":
						if (!(rootFrame.Content is SettingsPage))
						{
							rootFrame.Navigate(typeof(SettingsPage));

							// If we're loading the Settings page for the first time, place the main page on
							// the back stack so that user can go back after they've been
							// navigated to the specific page
							if (rootFrame.BackStack.Count == 0)
							{
								rootFrame.BackStack.Add(new PageStackEntry(typeof(MainPage), null, null));
							}
						}
						break;
					case "viewArticle":
						if (args["url"] != null)
						{
							rootFrame.Navigate(typeof(MainPage), args["url"]);
						}
						else
						{
							if (!(rootFrame.Content is MainPage))
							{
								rootFrame.Navigate(typeof(MainPage));
							}
						}
						break;
				}
			}

			// TODO: Handle other types of activation, such as links.

			// Ensure the current window is active
			Window.Current.Activate();

			// Register a handler for BackRequested events and set the visibility of the Back button
			SystemNavigationManager systemNavigationManager = SystemNavigationManager.GetForCurrentView();
			systemNavigationManager.BackRequested += OnBackRequested;
			systemNavigationManager.AppViewBackButtonVisibility = rootFrame.CanGoBack ?
				AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;
		}

		/// <summary>
		/// A method called by both the OnLaunched and OnActivated methods. It is used to set up the root frame for the 
		/// application and update the live tile.
		/// </summary>
		/// <param name="e">Details about the activation request and process.</param>
		/// <returns>The root frame for this application.</returns>
		private Frame OnLaunchedOrActivated(IActivatedEventArgs e)
		{
			// Get the root frame
			Frame rootFrame = Window.Current.Content as Frame;

			// Do not repeat app initialization when the Window already has content,
			// just ensure that the window is active
			if (rootFrame == null)
			{
				// Create a Frame to act as the navigation context
				rootFrame = new Frame();
				rootFrame.Navigated += OnNavigated;
				rootFrame.NavigationFailed += OnNavigationFailed;

				if (e.PreviousExecutionState == ApplicationExecutionState.Terminated ||
					e.PreviousExecutionState == ApplicationExecutionState.ClosedByUser)
				{
					//TODO: Load state from previously suspended application
				}

				// Place the frame in the current Window
				Window.Current.Content = rootFrame;
			}

			// Assume that the user has interacted with content that they have been notified about or does not 
			// care to do so, so clear all live tile notifications and badge notifications.
			TileUpdater tileUpdater = TileUpdateManager.CreateTileUpdaterForApplication();
			tileUpdater.EnableNotificationQueue(true);
			tileUpdater.Clear();

			BadgeUpdateManager.CreateBadgeUpdaterForApplication().Clear();
			localSettings.Values["numberOfUnreadNotifications"] = 0;

			return rootFrame;
		}

		/// <summary>
		/// The event handler called when the system-provided back button or back event is pressed or activated.
		/// </summary>
		/// <param name="sender">The object that produced the event.</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void OnBackRequested(object sender, BackRequestedEventArgs e)
		{
			Frame rootFrame = Window.Current.Content as Frame;
			if (rootFrame.Content is MainPage)
			{
				// Do nothing
			}
			else
			{
				if (rootFrame.CanGoBack && e.Handled == false)
				{
					rootFrame.GoBack();
					e.Handled = true;
				}
			}
		}

		/// <summary>
		/// Invoked when the root frame has navigated to a new page in the app. Used to update the visibility of the 
		/// system-provided back button.
		/// </summary>
		/// <param name="sender">The frame which navigated to a new page.</param>
		/// <param name="e">Details about the navigation event.</param>
		private void OnNavigated(object sender, NavigationEventArgs e)
		{
			// Each time a navigation event occurs, update the Back button's visibility
			SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = ((Frame)sender).CanGoBack ?
				AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;
		}

		/// <summary>
		/// Invoked when Navigation to a certain page fails.
		/// </summary>
		/// <param name="sender">The frame which failed navigation.</param>
		/// <param name="e">Details about the navigation failure.</param>
		private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
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
            //var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            //deferral.Complete();
        }
    }
}
