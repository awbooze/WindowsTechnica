using Microsoft.QueryStringDotNET;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WindowsTechnica
{
	/// <summary>
	/// The settings page for this application. Allows the user to alter various settings.
	/// </summary>
	public sealed partial class SettingsPage : Page
	{
		int theme;
		int i = 0;
		ApplicationDataContainer localSettings;

		/// <summary>
		/// The constructor for the settings page. Initializes all componenets from XAML, initializes the local 
		/// settings object, and adds adds a OnAppSuspending method to save data when the app is being suspended.
		/// </summary>
		public SettingsPage()
		{
			InitializeComponent();

			// Initialize the local settings
			localSettings = ApplicationData.Current.LocalSettings;

			// Adds an event handler that is called when the app is suspending.
			Application.Current.Suspending += new SuspendingEventHandler(OnAppSuspending);
		}

		/// <summary>
		/// The event handler called when the page has finished loading. Used to change whether the back button 
		/// is visible.
		/// </summary>
		/// <param name="sender">The page which has finished loading.</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void Page_Loaded(object sender, RoutedEventArgs e)
		{
			// Initialize theme settings.
			if (localSettings.Values["Theme"] != null)
			{
				theme = (int)localSettings.Values["Theme"];
				
				if (theme == 0)
				{
					DefaultRadioButton.IsChecked = true;
				}
				else if (theme == 1)
				{
					LightRadioButton.IsChecked = true;
				}
				else
				{
					DarkRadioButton.IsChecked = true;
				}
			}

			// Initialize homeUrl setting
			if (localSettings.Values["homeUrl"] != null)
			{
				HomeUrl.Text = (string)localSettings.Values["homeUrl"];
			}

			// Initialize currentUrl setting
			if (localSettings.Values["currentUrl"] != null)
			{
				CurrentUrl.Text = (string)localSettings.Values["currentUrl"];
			}

			// Initialize setting for inline frames in html.
			if (localSettings.Values["enableIframes"] != null)
			{
				enableIframesToggle.IsOn = (bool)localSettings.Values["enableIframes"];
			} 
			else
			{
				// The app defaults to true because disabling Iframes removes ads and may cause issues I haven't found yet.
				enableIframesToggle.IsOn = true;
			}
		}

		/// <summary>
		/// The event handler called when one of the RadioButtons that define the application's color mode is checked.
		/// </summary>
		/// <param name="sender">The RadioButton that was checked.</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void RadioButton_Checked(object sender, RoutedEventArgs e)
		{
			RadioButton checkedButton = (RadioButton)sender;
			UISettings uiSettings = new UISettings();
			Color systemBgColor = uiSettings.GetColorValue(UIColorType.Background);

			switch (checkedButton.Name)
			{
				case "DefaultRadioButton":
					theme = 0;

					// If the system default background is opposite the application-set background, then request the 
					// system-appropriate theme, not the application-appropriate theme.
					if (systemBgColor == Colors.Black && Application.Current.RequestedTheme == ApplicationTheme.Light)
					{
						Frame.RequestedTheme = ElementTheme.Dark;
					}
					else if (systemBgColor == Colors.White && Application.Current.RequestedTheme == ApplicationTheme.Dark)
					{
						Frame.RequestedTheme = ElementTheme.Light;
					}
					else
					{
						Frame.RequestedTheme = ElementTheme.Default;
					}
					break;
				case "LightRadioButton":
					theme = 1;
					Frame.RequestedTheme = ElementTheme.Light;
					break;
				case "DarkRadioButton":
					theme = 2;
					Frame.RequestedTheme = ElementTheme.Dark;
					break;
				default:
					theme = 0;

					// If the system default background is opposite the application-set background, then request the 
					// system-appropriate theme, not the application-appropriate theme.
					if (systemBgColor == Colors.Black && Application.Current.RequestedTheme == ApplicationTheme.Light)
					{
						Frame.RequestedTheme = ElementTheme.Dark;
					}
					else if (systemBgColor == Colors.White && Application.Current.RequestedTheme == ApplicationTheme.Dark)
					{
						Frame.RequestedTheme = ElementTheme.Light;
					}
					else
					{
						Frame.RequestedTheme = ElementTheme.Default;
					}
					break;
			}

			localSettings.Values["Theme"] = theme;
		}

		/// <summary>
		/// The event handler called when the Iframe toggle switch is toggled.
		/// </summary>
		/// <param name="sender">The enableIframesToggle ToggleSwitch.</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void EnableIframesToggle_Toggled(object sender, RoutedEventArgs e)
		{
			localSettings.Values["enableIframes"] = enableIframesToggle.IsOn;
		}

		/// <summary>
		/// The event handler called when one of the three test notification buttons is clicked. Used to send or clear 
		/// test notifications.
		/// </summary>
		/// <param name="sender">The button that was clicked.</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void TestNotificationButton_Click(object sender, RoutedEventArgs e)
		{
			Button clickedButton = (Button)sender;

			string articleTitle = "Test Article Notification";
			string articleText = "This is long text that would not be able to fit on one line. This area of text " +
									"would typically include a snippet from the article or the article subtitle.";
			string articleType = "Article Type";

			// Send a toast notification
			if (clickedButton.Name.Equals("testToastNotificationButton"))
			{
				// Construct the visuals of the toast
				ToastVisual visual = new ToastVisual()
				{
					BindingGeneric = new ToastBindingGeneric()
					{
						Children =
						{
							new AdaptiveText()
							{
								Text = articleTitle
							},

							new AdaptiveText()
							{
								Text = articleText
							},
						},

						Attribution = new ToastGenericAttributionText()
						{
							Text = articleType
						}
					}
				};

				// Now we can construct the final toast content
				ToastContent toastContent = new ToastContent()
				{
					Visual = visual,

					// Arguments when the user taps body of toast
					Launch = new QueryString()
					{
						{ "action", "viewSettings"}

					}.ToString()
				};

				// Create and send the toast notification
				var testToastNotification = new ToastNotification(toastContent.GetXml())
				{
					Group = "test"
				};
				ToastNotificationManager.CreateToastNotifier().Show(testToastNotification);
			}
			// Send a tile notification
			else if (clickedButton.Name.Equals("testTileNotificationButton"))
			{
				i++;

				// Construct the tile content
				TileContent content = new TileContent()
				{
					Visual = new TileVisual()
					{
						// Medium Tile Content
						TileMedium = new TileBinding()
						{
							Content = new TileBindingContentAdaptive()
							{
								Children =
								{
									new AdaptiveText()
									{
										Text = articleTitle + " " + i,
										HintWrap = true
									},

									new AdaptiveText()
									{
										Text = articleText,
										HintStyle = AdaptiveTextStyle.CaptionSubtle,
										HintWrap = true
									},

									new AdaptiveText()
									{
										Text = articleType,
										HintStyle = AdaptiveTextStyle.CaptionSubtle
									}
								}
							}
						},

						// Create a wide tile binding with the same content, but a bigger title
						TileWide = new TileBinding()
						{
							Content = new TileBindingContentAdaptive()
							{
								Children =
								{
									new AdaptiveText()
									{
										Text = articleTitle + " " + i,
										HintStyle = AdaptiveTextStyle.Body,
										HintWrap = true
									},

									new AdaptiveText()
									{
										Text = articleText,
										HintStyle = AdaptiveTextStyle.CaptionSubtle,
										HintWrap = true
									},

									new AdaptiveText()
									{
										Text = articleType,
										HintStyle = AdaptiveTextStyle.CaptionSubtle
									}
								}
							}
						},

						//Create a large tile binding with the same content, but a bigger title
						TileLarge = new TileBinding()
						{
							Content = new TileBindingContentAdaptive()
							{
								Children =
								{
									new AdaptiveText()
									{
										Text = articleTitle + " " + i,
										HintStyle = AdaptiveTextStyle.Subtitle,
										HintWrap = true
									},

									new AdaptiveText()
									{
										Text = articleText,
										HintStyle = AdaptiveTextStyle.CaptionSubtle,
										HintWrap = true
									},

									new AdaptiveText()
									{
										Text = articleType,
										HintStyle = AdaptiveTextStyle.CaptionSubtle
									}
								}
							}
						},
					}
				};

				// Create the tile notification
				var testTileNotification = new TileNotification(content.GetXml())
				{
					ExpirationTime = DateTimeOffset.UtcNow.AddDays(1)
				};

				// Send the notification to the primary tile
				TileUpdateManager.CreateTileUpdaterForApplication().Update(testTileNotification);
			}
			// Clear all tile notifications
			else
			{
				TileUpdateManager.CreateTileUpdaterForApplication().Clear();
			}
		}

		/// <summary>
		/// The event handler called when the button to activate the licenses dialog box is clicked.
		/// </summary>
		/// <param name="sender">The button for the licenses dialog box.</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private async void LicensesDialogButton_ClickAsync(object sender, RoutedEventArgs e)
		{
			// Show the licenses dialog box
			await licensesDialog.ShowAsync();
		}

		/// <summary>
		/// The event handler called once the licenses dialog box has loaded on the page.
		/// </summary>
		/// <param name="sender">The licenses dialog box.</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void LicensesDialog_Loaded(object sender, RoutedEventArgs e)
		{
			if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Controls.ContentDialog", "DefaultButton"))
			{
				licensesDialog.DefaultButton = ContentDialogButton.Close;
			}
		}

		/// <summary>
		/// The event handler called when the app is being suspended by the system. This method is a good place to 
		/// save values that need to be preserved until the app's next activiation.
		/// </summary>
		/// <param name="sender">The object that produced the event.</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void OnAppSuspending(object sender, SuspendingEventArgs e)
		{
			localSettings.Values["Theme"] = theme;

			if (HomeUrl.Text != null)
			{
				string homeUrl = HomeUrl.Text;
				localSettings.Values["homeUrl"] = homeUrl;
			}
		}
	}
}
