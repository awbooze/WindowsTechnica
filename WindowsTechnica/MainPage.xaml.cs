using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WindowsTechnica
{
	/// <summary>
	/// The main page of this application. Contains a CommandBar and a WebView.
	/// </summary>
	public sealed partial class MainPage : Page
	{
		private bool onMainPage = true;
		private const string FALLBACK_HOME_URL = "https://arstechnica.com/";
		private Uri currentUri;
		private string currentUrl;
		private string homeUrl;
		ApplicationDataContainer localSettings;

		/// <summary>
		/// The constructor for the main page. Initializes all components, adds a DataRequested method for 
		/// when the user would like to share the current page, and adds a OnAppSuspending method to save data 
		/// when the app is being suspended.
		/// </summary>
		public MainPage()
		{
			InitializeComponent();

			// Gets local settings to initialize the webview to the correct page.
			localSettings = ApplicationData.Current.LocalSettings;
			var tempHome = localSettings.Values["homeUrl"];
			if (tempHome == null)
			{
				homeUrl = FALLBACK_HOME_URL;
			}
			else
			{
				homeUrl = (string)tempHome;
			}

			var tempUrl = localSettings.Values["currentUrl"];
			if (tempUrl == null)
			{
				currentUrl = homeUrl;
			}
			else
			{
				currentUrl = (string)tempUrl;
			}

			try
			{
				currentUri = new Uri(currentUrl);
			}
			catch (FormatException)
			{
				currentUri = new Uri(FALLBACK_HOME_URL);
			}
			ArsWebView.Navigate(currentUri);

			// Overrides data requested method to share the current page
			DataTransferManager.GetForCurrentView().DataRequested += MainPage_DataRequested;

			// Registers an event handler for the DataChanged event as part of initializing notifications
			ApplicationData.Current.DataChanged += new TypedEventHandler<ApplicationData, object>(Data_Changed);
			if(localSettings.Values["recentlyUpdated"] != null && (bool)localSettings.Values["recentlyUpdated"] == true)
			{
				InitializeNotifications(true);
				localSettings.Values["recentlyUpdated"] = false;
			}
			else
			{
				InitializeNotifications(false);
			}
			
			// Adds an event handler that is called when the app is suspending
			Application.Current.Suspending += new SuspendingEventHandler(OnAppSuspending);
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			if(e.Parameter != null && !e.Parameter.ToString().Equals(String.Empty))
			{
				try
				{
					currentUri = new Uri(e.Parameter.ToString());
					currentUrl = e.Parameter.ToString();
				}
				catch (FormatException)
				{
					try
					{
						currentUri = new Uri(currentUrl);
					}
					catch (FormatException)
					{
						currentUri = new Uri(FALLBACK_HOME_URL);
					}
				}
				ArsWebView.Navigate(currentUri);
			}

			onMainPage = true;
		}

		/// <summary>
		/// The event handler called when the page has finished loading. Used to change whether the back button is visible.
		/// </summary>
		/// <param name="sender">The page which has finished loading.</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void Page_Loaded(object sender, RoutedEventArgs e)
		{
			SystemNavigationManager systemNavigationManager = SystemNavigationManager.GetForCurrentView();
			Frame rootFrame = Window.Current.Content as Frame;
			systemNavigationManager.BackRequested += OnBackRequested;
			systemNavigationManager.AppViewBackButtonVisibility = ArsWebView.CanGoBack || rootFrame.CanGoBack ?
				AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;
		}


		private void Data_Changed(ApplicationData sender, object args)
		{
			InitializeNotifications(true);
		}


		private async void InitializeNotifications(bool shouldReschedule)
		{
			//Check if notifications are enabled by the user
			bool notificationsEnabled = false;

			if (localSettings.Values["notificationsEnabled"] != null)
			{
				notificationsEnabled = (bool)localSettings.Values["notificationsEnabled"];
			}

			bool notificationTaskRegistered = false;
			string notificationTaskName = "NotificationsBackgroundTask";

			// If notifications enabled, deal with setting up the background task
			if (notificationsEnabled)
			{
				// Figure out whether notification background task is active and don't re-register it unless 
				// some settings have changed to make it necessary to re-register
				foreach (var task in BackgroundTaskRegistration.AllTasks)
				{
					if (task.Value.Name.Equals(notificationTaskName))
					{
						if(shouldReschedule)
						{
							task.Value.Unregister(true);
							BackgroundExecutionManager.RemoveAccess();
						}
						else
						{
							notificationTaskRegistered = true;
						}
						break;
					}
				}

				// Get the background access status
				BackgroundAccessStatus backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();
				bool notificationsAllowed = false;
				if (backgroundAccessStatus == BackgroundAccessStatus.AlwaysAllowed ||
					backgroundAccessStatus == BackgroundAccessStatus.AllowedSubjectToSystemPolicy)
				{
					notificationsAllowed = true;
				}

				// Register the background task if it has not already been registered.
				if (!notificationTaskRegistered && notificationsAllowed)
				{
					BackgroundTaskBuilder builder = new BackgroundTaskBuilder
					{
						Name = notificationTaskName,
						TaskEntryPoint = "Tasks." + notificationTaskName
					};

					uint notificationFrequency;

					ApplicationDataCompositeValue notificationFrequencyCompositeValue =
						(ApplicationDataCompositeValue)localSettings.Values["notificationFrequencyComposite"];

					if (notificationFrequencyCompositeValue != null)
					{
						notificationFrequency = Convert.ToUInt32((int)notificationFrequencyCompositeValue["notificationFrequency"]);
					}
					else
					{
						// Default to checking for notifications once every hour
						notificationFrequency = 60;
					}

					builder.SetTrigger(new TimeTrigger(notificationFrequency, false));
					builder.IsNetworkRequested = true;
					builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));

					BackgroundTaskRegistration task = builder.Register();
				}
				else if (!notificationsAllowed)
				{
					// Display a dialog box that states that the app can't run in the background, 
					// so notifications won't work, only if the user is on the main page and notifications 
					// are enabled in the first place.
					if(onMainPage)
					{
						ContentDialog backgroundNotAllowedDialog = new ContentDialog()
						{
							Title = "Background Activity not allowed",
							Content = "It seems that Windows is not allowing Windows Technica to run in the background. " +
										"This means you will not get any notifications. If you want to get notifications, change " +
										"the background app setting for Windows Technica.",
							PrimaryButtonText = "Check Windows settings",
							CloseButtonText = "Disable notifications"
						};

						backgroundNotAllowedDialog.Loaded += BackgroundNotAllowedDialog_Loaded;
						backgroundNotAllowedDialog.PrimaryButtonClick += BackgroundNotAllowedDialog_PrimaryButtonClickAsync;
						backgroundNotAllowedDialog.CloseButtonClick += BackgroundNotAllowedDialog_CloseButtonClick;

						await backgroundNotAllowedDialog.ShowAsync();
					}
				}
			}
			else
			{
				foreach (var task in BackgroundTaskRegistration.AllTasks)
				{
					if (task.Value.Name.Equals(notificationTaskName))
					{
						task.Value.Unregister(true);
						break;
					}
				}

				BackgroundExecutionManager.RemoveAccess();
			}
		}

		/// <summary>
		/// The event handler called when the backgroundNotAllowedDialog is loaded. Used to set the default 
		/// button for the dialog.
		/// </summary>
		/// <param name="sender">The backgroundNotAllowedDialog</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void BackgroundNotAllowedDialog_Loaded(object sender, RoutedEventArgs e)
		{
			if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Controls.ContentDialog", "DefaultButton"))
			{
				((ContentDialog)sender).DefaultButton = ContentDialogButton.Primary;
			}
		}

		/// <summary>
		/// The event handler called when the primary button of the backgroundNotAllowedDialog is pressed. 
		/// Launches the settings app to allow the user to alter the background app permission for this app.
		/// </summary>
		/// <param name="sender">The backgroundNotAllowedDialog</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private async void BackgroundNotAllowedDialog_PrimaryButtonClickAsync(ContentDialog sender, 
			ContentDialogButtonClickEventArgs args)
		{
			await Launcher.LaunchUriAsync(new Uri("ms-settings:appsfeatures-app"));
		}

		/// <summary>
		/// The event handler called when the close button of the backgroundNotAllowedDialog is clicked. Used 
		/// to disable notifications, as the app isn't allowed to fetch them, anyway.
		/// </summary>
		/// <param name="sender">The backgroundNotAllowedDialog</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void BackgroundNotAllowedDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			localSettings.Values["notificationsEnabled"] = false;
		}

		/// <summary>
		/// A helper method for converting a URI to a string.
		/// </summary>
		/// <param name="uri">The URI to convert to a string.</param>
		/// <returns>A string representing this URI.</returns>
		static string UriToString(Uri uri)
		{
			return (uri != null) ? uri.ToString() : "";
		}

		/// <summary>
		/// The event handler called when ArsWebView begins navigating to a new page.
		/// </summary>
		/// <param name="sender">
		/// The WebView which has started navigating to a new page. In this case, it will always be ArsWebView.
		/// </param>
		/// <param name="args">Any arguments provided by the event.</param>
		private void ArsWebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
		{
			if (IsAllowedUri(args.Uri))
			{
				// If the page is allowed, show the progress bar and let the WebView load the page
				ArsProgressBar.ShowPaused = false;
				ArsProgressBar.Visibility = Visibility.Visible;    //Display the element
			}
			else
			{
				// Otherwise, the link is not for a page on Ars, so launch the system-provided browser and stop 
				// loading it in the WebView
				IAsyncOperation<bool> b = Launcher.LaunchUriAsync(args.Uri);
				args.Cancel = true;
			}
		}

		/// <summary>
		/// Checks if the WebView is allowed to load the provided URI. If it is an Ars Technica URL, then the WebView 
		/// is allowed to navigate to it.
		/// </summary>
		/// <param name="uri">The URI the WebView wants to navigate to</param>
		/// <returns>True if the URI is related to Ars Technica and false if it is not</returns>
		private bool IsAllowedUri(Uri uri)
		{
			if (uri.Host.Contains("arstechnica.com"))
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// The event handler called when ArsWebView has started to load webpage content. Used to adjust various user 
		/// interface elements.
		/// </summary>
		/// <param name="sender">
		/// The WebView which has started navigating to a new page. In this case, it will always be ArsWebView.
		/// </param>
		/// <param name="args">Any arguments provided by the event.</param>
		private void ArsWebView_ContentLoading(WebView sender, WebViewContentLoadingEventArgs args)
		{
			currentUri = args.Uri;
			currentUrl = UriToString(currentUri);
			// Use the current URL for the titlebar text...
			titleBar.Text = currentUrl.Substring(8);
			// Or use the Document Title
			// titleBar.Text = ArsWebView.DocumentTitle;

			//Enable and disable back and forward buttons if the webview can go back or forward, respectively
			if (ArsWebView.CanGoBack)
			{
				BackButton.IsEnabled = true;
				SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
					AppViewBackButtonVisibility.Visible;
			}
			else
			{
				BackButton.IsEnabled = false;
				SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
					AppViewBackButtonVisibility.Collapsed;
			}

			if (ArsWebView.CanGoForward)
			{
				ForwardButton.IsEnabled = true;
			}
			else
			{
				ForwardButton.IsEnabled = false;
			}
		}

		/// <summary>
		/// The event handler called when ArsWebView has completed navigation to a webpage.
		/// </summary>
		/// <param name="sender">
		/// The WebView which has completed navigation to a new page. In this case, it will always be ArsWebView.
		/// </param>
		/// <param name="args">Any arguments provided by the event.</param>
		private void ArsWebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
		{
			// Hide the progress bar
			ArsProgressBar.Visibility = Visibility.Collapsed;
			ArsProgressBar.ShowPaused = true;
		}

		private void ArsWebView_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
		{
			//FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
		}

		/// <summary>
		/// The event handler called when ArsWebView has encountered a link which cannot be opened by the WebView, 
		/// such as a PDF.
		/// </summary>
		/// <param name="sender">
		/// The WebView which has tried to open unviewable content. In this case, it will always be ArsWebView.
		/// </param>
		/// <param name="args">Any arguments provided by the event.</param>
		private void ArsWebView_UnviewableContentIdentified(WebView sender,
			WebViewUnviewableContentIdentifiedEventArgs args)
		{
			IAsyncOperation<bool> b = Launcher.LaunchUriAsync(args.Uri);
			ArsWebView.Stop();
		}

		/// <summary>
		/// The event handler called when the WebView detects a script that has run for a long time.
		/// </summary>
		/// <param name="sender">
		/// The WebView which has detected the script. In this case, it will always be ArsWebView.
		/// </param>
		/// <param name="args">Any arguments provided by the event.</param>
		private void ArsWebView_LongRunningScriptDetected(WebView sender, WebViewLongRunningScriptDetectedEventArgs args)
		{
			// Stop scripts from executing after doing so for 16 milliseconds.
			if (args.ExecutionTime.TotalMilliseconds > 16)
			{
				args.StopPageScriptExecution = true;
			}
		}

		/// <summary>
		/// The event handler called when a frame inside of the webview begins to navigate to another URI.
		/// </summary>
		/// <param name="sender">
		/// The WebView which contains the frame which has begun to navigate to a new URI. In this case, it 
		/// will always be ArsWebView.
		/// </param>
		/// <param name="args">Any arguments provided by the event.</param>
		private void ArsWebView_FrameNavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
		{
			if (localSettings.Values["enableIframes"] != null)
			{
				// Cancel all frame navigations if setting is set to do so.
				// Before enabling this, the app will regularly use above 750MB of RAM and ~20% CPU on my computer.
				// After enabling this, app will typically use between 300 and 500 MB RAM and ~5% CPU.
				args.Cancel = !(bool)localSettings.Values["enableIframes"];
			}
			else
			{
				args.Cancel = false;
			}
		}

		/// <summary>
		/// The event handler called when the CommandBar has finished loading. Currently used to disable dynamic 
		/// overflow for CommandBar buttons, as using dynamic overflow reduces the amount of buttons readily 
		/// available on smaller screens.
		/// </summary>
		/// <param name="sender">
		/// The object which has finished loading. In this case, it will always be commandBar.
		/// </param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void CommandBar_Loaded(object sender, RoutedEventArgs e)
		{
			if (ApiInformation.IsEventPresent("Windows.UI.Xaml.Controls.CommandBar", "DynamicOverflowItemsChanging"))
			{
				commandBar.IsDynamicOverflowEnabled = false;
			}
		}

		/// <summary>
		/// The event handler called when the system-provided back button or back event is pressed or activated.
		/// </summary>
		/// <param name="sender">The object that produced the event.</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void OnBackRequested(object sender, BackRequestedEventArgs e)
		{
			if (ArsWebView.CanGoBack)
			{
				ArsWebView.GoBack();
				e.Handled = true;
			}
		}

		/// <summary>
		/// The event handler called when the back button is clicked.
		/// </summary>
		/// <param name="sender">The back button on the CommandBar.</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void BackButton_Click(object sender, RoutedEventArgs e)
		{
			if (ArsWebView.CanGoBack)
			{
				ArsWebView.GoBack();
			}
		}

		/// <summary>
		/// The event handler called when the forward button is clicked.
		/// </summary>
		/// <param name="sender">The forward button on the CommandBar.</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void ForwardButton_Click(object sender, RoutedEventArgs e)
		{
			if (ArsWebView.CanGoForward)
			{
				ArsWebView.GoForward();
			}
		}

		/// <summary>
		/// The event handler called when the home button is clicked.
		/// </summary>
		/// <param name="sender">The home button on the CommandBar.</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void HomeButton_Click(object sender, RoutedEventArgs e)
		{
			Uri targetUri = new Uri(homeUrl);
			ArsWebView.Navigate(targetUri);
		}

		/// <summary>
		/// The event handler called when the refresh button is clicked.
		/// </summary>
		/// <param name="sender">The refresh button on the CommandBar.</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void RefreshButton_Click(object sender, RoutedEventArgs e)
		{
			//Refresh the webpage
			ArsWebView.Refresh();
		}

		/// <summary>
		/// The event handler called when the share button is clicked.
		/// </summary>
		/// <param name="sender">The share button on the CommandBar.</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void ShareButton_Click(object sender, RoutedEventArgs e)
		{
			//Share the current url
			DataTransferManager.ShowShareUI();                              //Shows share UI
		}

		/// <summary>
		/// The event handler called when the page needs share information. Sets this information.
		/// </summary>
		/// <param name="sender">The DataTransferManager that is requesting data to share.</param>
		/// <param name="args">Any arguments provided by the event.</param>
		private void MainPage_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
		{
			args.Request.Data.SetWebLink(currentUri);
			args.Request.Data.Properties.Title = ArsWebView.DocumentTitle;
			args.Request.Data.Properties.Description = UriToString(currentUri);
		}

		/// <summary>
		/// The event handler called when the copy button is clicked.
		/// </summary>
		/// <param name="sender">The copy button on the CommandBar.</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void CopyButton_Click(object sender, RoutedEventArgs e)
		{
			//Copy the current url to the clipboard
			CopyToClipboard(currentUrl, currentUri);
			FlyoutBase.ShowAttachedFlyout(CopyButton);
		}

		/// <summary>
		/// Copies the current URI to the clipboard.
		/// </summary>
		/// <param name="Text">The text to copy.</param>
		/// <param name="WebLink">The website the text comes from.</param>
		private void CopyToClipboard(String Text, Uri WebLink)
		{
			DataPackage CopyDataPackage = new DataPackage
			{
				RequestedOperation = DataPackageOperation.Copy
			};
			CopyDataPackage.SetText(Text);
			CopyDataPackage.SetWebLink(WebLink);
			Clipboard.SetContent(CopyDataPackage);
		}

		/// <summary>
		/// The event handler called when the settings button is clicked.
		/// </summary>
		/// <param name="sender">The settings button on the CommandBar.</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void SettingsButton_Click(object sender, RoutedEventArgs e)
		{
			//Open the settings screen
			Frame.Navigate(typeof(SettingsPage));
		}

		private void ShareFlyoutItem_Click(object sender, RoutedEventArgs e)
		{
			//Share target of hit test
		}

		private void CopyFlyoutItem_Click(object sender, RoutedEventArgs e)
		{
			//Copy target of hit test to clipboard
		}

		/// <summary>
		/// The event handler called when the Frame this Page is in is about to navigate away from the Page.
		/// </summary>
		/// <param name="e">Any arguments provided by the event.</param>
		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			// Call the superclass
			base.OnNavigatingFrom(e);

			// Save local settings
			SaveLocalSettings();

			onMainPage = false;
		}

		/// <summary>
		/// The event handler called when the app is being suspended by the system. This method is a good place to 
		/// save values that need to be preserved until the app's next activiation.
		/// </summary>
		/// <param name="sender">The object that produced the event.</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void OnAppSuspending(object sender, SuspendingEventArgs e)
		{
			// Save local settings
			var deferral = e.SuspendingOperation.GetDeferral();
			SaveLocalSettings();
			deferral.Complete();
		}

		/// <summary>
		/// A method called by both OnNavigatingFrom and OnAppSuspending to save settings values to LocalSettings.
		/// </summary>
		private void SaveLocalSettings()
		{
			// Save the home url
			localSettings.Values["homeUrl"] = homeUrl;

			// Save the current url
			localSettings.Values["currentUrl"] = currentUrl;
		}
	}
}
