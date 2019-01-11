using Microsoft.QueryStringDotNET;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using Windows.ApplicationModel;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

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
		CheckBox[] toastCheckBoxes;
		CheckBox[] liveTileCheckBoxes;
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
		/// The event handler called when the page has finished loading. Used to initialize settings.
		/// </summary>
		/// <param name="sender">The page which has finished loading.</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void Page_Loaded(object sender, RoutedEventArgs e)
		{
			// Initialize homeUrl setting
			if (localSettings.Values["homeUrl"] != null)
			{
				HomeUrl.Text = (string)localSettings.Values["homeUrl"];
			}

			// Initialize theme setting
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

			// Initialize notification settings
			if (localSettings.Values["notificationsEnabled"] != null)
			{
				enableNotificationsToggle.IsOn = (bool)localSettings.Values["notificationsEnabled"];
			}
			else
			{
				// Make sure to default enabling notifications to false.
				// We don't want to bother people with notifications they don't want.
				enableNotificationsToggle.IsOn = false;
			}

			// Initialize toast check boxes
			toastCheckBoxes = new CheckBox[] { itToastCheckBox, techToastCheckBox, businessToastCheckBox,
				securityToastCheckBox, policyToastCheckBox, gamingToastCheckBox, scienceToastCheckBox, fantasyToastCheckBox,
				carsToastCheckBox, blogToastCheckBox, boardGameToastCheckBox };

			ApplicationDataCompositeValue toastCheckBoxComposite = 
				(ApplicationDataCompositeValue)localSettings.Values["toastCheckBoxComposite"];

			if (toastCheckBoxComposite != null)
			{
				for (int i = 0; i < 11; i++)
				{
					if (toastCheckBoxComposite[i.ToString()] != null)
					{
						toastCheckBoxes[i].IsChecked = (bool)toastCheckBoxComposite[i.ToString()];
					}
				}

				// Determine how to set the "All subscriptions" checkbox
				bool allChecked = true;
				bool noneChecked = true;

				foreach (CheckBox c in toastCheckBoxes)
				{
					if (c.IsChecked != null)
					{
						if ((bool)c.IsChecked)
						{
							noneChecked = false;
						}
						else
						{
							allChecked = false;
						}
					}
				}

				if (allChecked)
				{
					allToastCheckBox.IsChecked = true;
				}
				else if (noneChecked)
				{
					allToastCheckBox.IsChecked = false;
				}
				else
				{
					// Set this checkbox to indeterminate
					allToastCheckBox.IsChecked = null;
				}
			}

			// Initialize live tile check boxes
			liveTileCheckBoxes = new CheckBox[] { itLiveTileCheckBox, techLiveTileCheckBox, businessLiveTileCheckBox,
				securityLiveTileCheckBox, policyLiveTileCheckBox, gamingLiveTileCheckBox, scienceLiveTileCheckBox,
				fantasyLiveTileCheckBox, carsLiveTileCheckBox, blogLiveTileCheckBox, boardGameLiveTileCheckBox };

			ApplicationDataCompositeValue liveTileCheckBoxComposite = 
				(ApplicationDataCompositeValue)localSettings.Values["liveTileCheckBoxComposite"];

			if (liveTileCheckBoxComposite != null)
			{
				for (int i = 0; i < 11; i++)
				{
					if (liveTileCheckBoxComposite[i.ToString()] != null)
					{
						liveTileCheckBoxes[i].IsChecked = (bool)liveTileCheckBoxComposite[i.ToString()];
					}
				}

				// Determine how to set the "All subscriptions" checkbox
				bool allChecked = true;
				bool noneChecked = true;

				foreach (CheckBox c in liveTileCheckBoxes)
				{
					if (c.IsChecked != null)
					{
						if ((bool)c.IsChecked)
						{
							noneChecked = false;
						}
						else
						{
							allChecked = false;
						}
					}
				}

				if (allChecked)
				{
					allLiveTileCheckBox.IsChecked = true;
				}
				else if (noneChecked)
				{
					allLiveTileCheckBox.IsChecked = false;
				}
				else
				{
					// Set this checkbox to indeterminate
					allLiveTileCheckBox.IsChecked = null;
				}
			}

			ApplicationDataCompositeValue notificationFrequencyCompositeValue = 
				(ApplicationDataCompositeValue)localSettings.Values["notificationFrequencyComposite"];

			if(notificationFrequencyCompositeValue != null)
			{
				notificationFrequencyComboBox.SelectedIndex = (int)notificationFrequencyCompositeValue["comboBoxSelectedIndex"];
			}
			else
			{
				// By default, check for notifications once every hour
				notificationFrequencyComboBox.SelectedIndex = 2;
			}

			// Initialize currentUrl setting
			if (localSettings.Values["currentUrl"] != null)
			{
				CurrentUrl.Text = (string)localSettings.Values["currentUrl"];
			}

			// Initialize setting for inline frames in html
			if (localSettings.Values["enableIframes"] != null)
			{
				enableIframesToggle.IsOn = (bool)localSettings.Values["enableIframes"];
			} 
			else
			{
				// The app defaults to true because disabling Iframes removes ads and may cause issues I haven't found yet.
				enableIframesToggle.IsOn = true;
			}

			// Initialize last check for updates data
			if(localSettings.Values["lastCheckForUpdatesDateTime"] != null)
			{
				lastCheckForUpdatesTextBox.Text = ((DateTime)localSettings.Values["lastCheckForUpdatesDateTime"]).ToString();
			}
			else
			{
				lastCheckForUpdatesTextBox.Text = new DateTime(DateTime.Now.Ticks).ToString();
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
				case "LightRadioButton":
					theme = 1;
					Frame.RequestedTheme = ElementTheme.Light;
					break;
				case "DarkRadioButton":
					theme = 2;
					Frame.RequestedTheme = ElementTheme.Dark;
					break;
				default:
					// This is also the "DefaultRadioButton" case.
					theme = 0;

					// If the system default background is opposite the current application-set background, then request the 
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
		}

		/// <summary>
		/// The event handler called when the allToastCheckBox is moved to the checked state.
		/// </summary>
		/// <param name="sender">The allToastCheckBox.</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void AllToastCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			// Go through and set all toastCheckBoxes to true
			foreach (CheckBox c in toastCheckBoxes)
			{
				c.IsChecked = true;
			}
		}

		/// <summary>
		/// The event handler called when the allToastCheckBox is moved to the unchecked state.
		/// </summary>
		/// <param name="sender">The allToastCheckBox.</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void AllToastCheckBox_Unchecked(object sender, RoutedEventArgs e)
		{
			// Go through and set all toastCheckBoxes to false
			foreach (CheckBox c in toastCheckBoxes)
			{
				c.IsChecked = false;
			}
		}

		/// <summary>
		/// The event handler called when the allToastCheckBox is moved to the indeterminate state. This method is used to 
		/// move this checkbox directly from checked to unchecked if all of the other checkboxes are checked.
		/// </summary>
		/// <param name="sender">The allToastCheckBox.</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void AllToastCheckBox_Indeterminate(object sender, RoutedEventArgs e)
		{
			// Checkboxes typically rotate from unchecked -> checked -> indeterminate if they
			// are three state enabled. When this checkbox is indeterminate and all other 
			// checkboxes in the group are checked, uncheck this one, which will uncheck the 
			// rest. Basically, this skips right from everything checked to everythiong unchecked.

			bool allChecked = true;

			foreach (CheckBox c in toastCheckBoxes)
			{
				if (c.IsChecked != null)
				{
					if ((bool)!(c.IsChecked))
					{
						allChecked = false;
					}
				}
			}

			if (allChecked)
			{
				allToastCheckBox.IsChecked = false;
			}
		}

		/// <summary>
		/// The event handler called when a regular toastCheckBox is clicked.
		/// </summary>
		/// <param name="sender">Any toastCheckBox other than the allToastCheckBox.</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void ToastCheckBox_Click(object sender, RoutedEventArgs e)
		{
			bool allChecked = true;
			bool noneChecked = true;

			foreach (CheckBox c in toastCheckBoxes)
			{
				if (c.IsChecked != null)
				{
					if ((bool)c.IsChecked)
					{
						noneChecked = false;
					}
					else
					{
						allChecked = false;
					}
				}
			}

			if (allChecked)
			{
				allToastCheckBox.IsChecked = true;
			}
			else if (noneChecked)
			{
				allToastCheckBox.IsChecked = false;
			}
			else
			{
				// Set this checkbox to indeterminate
				allToastCheckBox.IsChecked = null;
			}
		}

		/// <summary>
		/// The event handler called when the allLiveTileCheckBox is moved to the checked state.
		/// </summary>
		/// <param name="sender">The allLiveTileCheckBox.</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void AllLiveTileCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			// Go through and set all liveTileCheckBoxes to true
			foreach (CheckBox c in liveTileCheckBoxes)
			{
				c.IsChecked = true;
			}
		}

		/// <summary>
		/// The event handler called when the allLiveTileCheckBox is moved to the unchecked state.
		/// </summary>
		/// <param name="sender">The allLiveTileCheckBox.</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void AllLiveTileCheckBox_Unchecked(object sender, RoutedEventArgs e)
		{
			//Go through and set all liveTileCheckBoxes to false
			foreach (CheckBox c in liveTileCheckBoxes)
			{
				c.IsChecked = false;
			}
		}

		/// <summary>
		/// The event handler called when the allLiveTileCheckBox is moved to the indeterminate state. This method is used to 
		/// move this checkbox directly from checked to unchecked if all of the other checkboxes are checked.
		/// </summary>
		/// <param name="sender">The allLiveTileCheckBox.</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void AllLiveTileCheckBox_Indeterminate(object sender, RoutedEventArgs e)
		{
			// Checkboxes typically rotate from unchecked -> checked -> indeterminate if they
			// are three state enabled. When this checkbox is indeterminate and all other 
			// checkboxes in the group are checked, uncheck this one, which will uncheck the 
			// rest. Basically, this skips right from everything checked to everythiong unchecked.

			bool allChecked = true;

			foreach (CheckBox c in liveTileCheckBoxes)
			{
				if (c.IsChecked != null)
				{
					if ((bool)!(c.IsChecked))
					{
						allChecked = false;
					}
				}
			}

			if (allChecked)
			{
				allLiveTileCheckBox.IsChecked = false;
			}
		}

		/// <summary>
		/// The event handler called when a regular liveTileCheckBox is clicked.
		/// </summary>
		/// <param name="sender">Any toastCheckBox other than the allLiveTileCheckBox.</param>
		/// <param name="e">Any arguments provided by the event.</param>
		private void LiveTileCheckBox_Click(object sender, RoutedEventArgs e)
		{
			bool allChecked = true;
			bool noneChecked = true;

			foreach (CheckBox c in liveTileCheckBoxes)
			{
				if (c.IsChecked != null)
				{
					if ((bool)c.IsChecked)
					{
						noneChecked = false;
					}
					else
					{
						allChecked = false;
					}
				}
			}

			if (allChecked)
			{
				allLiveTileCheckBox.IsChecked = true;
			}
			else if (noneChecked)
			{
				allLiveTileCheckBox.IsChecked = false;
			}
			else
			{
				// Set this checkbox to indeterminate
				allLiveTileCheckBox.IsChecked = null;
			}
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
		/// The event handler called when the Frame this Page is in is about to navigate away from the Page.
		/// This method is a good place to save values that must be preserved until the setting page's next 
		/// activation.
		/// </summary>
		/// <param name="e">Any arguments provided by the event.</param>
		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			// Call the superclass
			base.OnNavigatingFrom(e);

			SaveLocalSettings();
		}

		/// <summary>
		/// The event handler called when the app is being suspended by the system. This method is another good 
		/// place to save values that need to be preserved until the app's next activiation.
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
			// Save home URL setting
			if (HomeUrl.Text != null)
			{
				string homeUrl = HomeUrl.Text;
				localSettings.Values["homeUrl"] = homeUrl;
			}

			// Save theme setting
			localSettings.Values["Theme"] = theme;

			// Save notification settings
			localSettings.Values["notificationsEnabled"] = enableNotificationsToggle.IsOn;

			// Save toast check boxes
			ApplicationDataCompositeValue toastCheckBoxComposite = new ApplicationDataCompositeValue();

			for (int i = 0; i < 11; i++)
			{
				toastCheckBoxComposite[i.ToString()] = (bool)toastCheckBoxes[i].IsChecked;
			}

			localSettings.Values["toastCheckBoxComposite"] = toastCheckBoxComposite;

			// Save live tile check boxes
			ApplicationDataCompositeValue liveTileCheckBoxComposite = new ApplicationDataCompositeValue();

			for (int i = 0; i < 11; i++)
			{
				liveTileCheckBoxComposite[i.ToString()] = (bool)liveTileCheckBoxes[i].IsChecked;
			}

			localSettings.Values["liveTileCheckBoxComposite"] = liveTileCheckBoxComposite;

			// Save notification frequency
			int notificationFrequency = -1;

			switch (notificationFrequencyComboBox.SelectedIndex)
			{
				case 0:
					notificationFrequency = 15;
					break;
				case 1:
					notificationFrequency = 30;
					break;
				case 2:
					notificationFrequency = 60;
					break;
				case 3:
					notificationFrequency = 120;
					break;
				case 4:
					notificationFrequency = 180;
					break;
				case 5:
					notificationFrequency = 300;
					break;
				default:
					break;
			}

			ApplicationDataCompositeValue notificationFrequencyComposite = new ApplicationDataCompositeValue
			{
				["notificationFrequency"] = notificationFrequency,
				["comboBoxSelectedIndex"] = notificationFrequencyComboBox.SelectedIndex
			};

			localSettings.Values["notificationFrequencyComposite"] = notificationFrequencyComposite;

			// Save setting for inline frames in html
			localSettings.Values["enableIframes"] = enableIframesToggle.IsOn;
		}
	}
}
