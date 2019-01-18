using Microsoft.QueryStringDotNET;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using Windows.UI.Notifications;

namespace Tasks
{
	public sealed class NotificationsBackgroundTask : IBackgroundTask
    {
		private BackgroundTaskDeferral _deferral;

		public async void Run(IBackgroundTaskInstance taskInstance)
		{
			_deferral = taskInstance.GetDeferral();

			// Set up notification arrays
			ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
			string[] rssFeedUrls = new string[] { "http://feeds.arstechnica.com/arstechnica/technology-lab",
				"http://feeds.arstechnica.com/arstechnica/gadgets", "http://feeds.arstechnica.com/arstechnica/business",
				"http://feeds.arstechnica.com/arstechnica/security", "http://feeds.arstechnica.com/arstechnica/tech-policy",
				"http://feeds.arstechnica.com/arstechnica/gaming", "http://feeds.arstechnica.com/arstechnica/science",
				"http://feeds.arstechnica.com/arstechnica/multiverse", "http://feeds.arstechnica.com/arstechnica/cars",
				"http://feeds.arstechnica.com/arstechnica/staff-blogs", "http://feeds.arstechnica.com/arstechnica/cardboard"};
			string[] rssFeedTitles = new string[] { "Information Technology", "Tech News & Reviews", "Business", "Security & Hactivism",
				"Tech Policy", "Gaming & Entertainment", "Science", "Sci-Fi & Fantasy", "Cars", "Staff Blogs", "Board Games"};

			bool[] toastNotificationBools = new bool[11];
			bool[] liveTileNotificationBools = new bool[11];

			List<string> toastRssFeedUrls = new List<string>();
			List<string> toastFeedTitles = new List<string>();

			List<string> liveTileRssFeedUrls = new List<string>();
			List<string> liveTileFeedTitles = new List<string>();

			// Retrieve which notifications are enabled
			ApplicationDataCompositeValue toastCheckBoxComposite =
				(ApplicationDataCompositeValue)localSettings.Values["toastCheckBoxComposite"];

			if (toastCheckBoxComposite != null)
			{
				for (int i = 0; i < 11; i++)
				{
					if (toastCheckBoxComposite[i.ToString()] != null)
					{
						toastNotificationBools[i] = (bool)toastCheckBoxComposite[i.ToString()];
					}
					else
					{
						toastNotificationBools[i] = false;
					}
				}
			}
			else
			{
				for (int i = 0; i < 11; i++)
				{
					toastNotificationBools[i] = false;
				}
			}

			ApplicationDataCompositeValue liveTileCheckBoxComposite =
				(ApplicationDataCompositeValue)localSettings.Values["liveTileCheckBoxComposite"];

			if (liveTileCheckBoxComposite != null)
			{
				for (int i = 0; i < 11; i++)
				{
					if (liveTileCheckBoxComposite[i.ToString()] != null)
					{
						liveTileNotificationBools[i] = (bool)liveTileCheckBoxComposite[i.ToString()];
					}
					else
					{
						toastNotificationBools[i] = false;
					}
				}
			}
			else
			{
				for (int i = 0; i < 11; i++)
				{
					toastNotificationBools[i] = false;
				}
			}

			// Transfer the enabled notification feeds and titles to lists
			for (int i = 0; i < toastNotificationBools.Length; i++)
			{
				if (toastNotificationBools[i] == true)
				{
					toastRssFeedUrls.Add(rssFeedUrls[i]);
					toastFeedTitles.Add(rssFeedTitles[i]);
				}

				if (liveTileNotificationBools[i] == true)
				{
					liveTileRssFeedUrls.Add(rssFeedUrls[i]);
					liveTileFeedTitles.Add(rssFeedTitles[i]);
				}
			}

			List<XmlDocument> toastRssFeedXmlList = new List<XmlDocument>();
			List<XmlDocument> liveTileRssFeedXmlList = new List<XmlDocument>();

			// Download XML from each notification page/RSS feed and parse it
			// Toast notifications first
			foreach (string s in toastRssFeedUrls)
			{
				toastRssFeedXmlList.Add(await XmlDocument.LoadFromUriAsync(new Uri(s)));
			}

			string notificationTitle = "";
			string notificationUrl = "";
			string notificationDescription = "";
			string notificationFeedTitle = "";
			DateTimeOffset notificationDateTime = DateTimeOffset.UtcNow;
			DateTimeOffset lastCheckDateTime;

			if (localSettings.Values["lastCheckForUpdatesDateTime"] != null)
			{
				lastCheckDateTime = ((DateTimeOffset)localSettings.Values["lastCheckForUpdatesDateTime"]).ToUniversalTime();
			}
			else
			{
				// By default, only fetch notifications from the last day
				lastCheckDateTime = DateTimeOffset.UtcNow.AddDays(-1);
			}

			// Set up badge notifications
			int numberOfNotificationsOnBadge;
			if (localSettings.Values["numberOfUnreadNotifications"] != null)
			{
				numberOfNotificationsOnBadge = (int)localSettings.Values["numberOfUnreadNotifications"];
				if (numberOfNotificationsOnBadge > 100)
				{
					numberOfNotificationsOnBadge = 0;
				}
			}
			else
			{
				numberOfNotificationsOnBadge = 0;
			}

			// Variable to set toast and live tile feed titles
			int j = 0;

			foreach (XmlDocument document in toastRssFeedXmlList)
			{
				notificationFeedTitle = toastFeedTitles[j];

				foreach (IXmlNode exteriorNode in document.DocumentElement.ChildNodes)
				{
					if (exteriorNode.LocalName != null && exteriorNode.LocalName.Equals("channel"))
					{
						foreach (IXmlNode interiorNode in exteriorNode.ChildNodes)
						{
							if (interiorNode.LocalName != null && interiorNode.LocalName.Equals("item")) 
							{
								foreach (IXmlNode finalNode in interiorNode.ChildNodes)
								{
									if(finalNode.LocalName != null && finalNode.InnerText != null)
									{
										if (finalNode.LocalName.Equals("title"))
										{
											notificationTitle = finalNode.InnerText;
										}
										else if (finalNode.LocalName.Equals("link"))
										{
											notificationUrl = finalNode.InnerText;
										}
										else if (finalNode.LocalName.Equals("description"))
										{
											notificationDescription = finalNode.InnerText;
										}
										else if (finalNode.LocalName.Equals("pubDate"))
										{
											try
											{
												notificationDateTime = DateTimeOffset.Parse(finalNode.InnerText);
											}
											catch (ArgumentException)
											{
												// String did not exist or the offset was incorrect. Just use the date and time for right now.
											}
											catch (FormatException)
											{
												// This date was formatted incorrectly, so just use right now.
											}
										}
									}
								}

								int shouldPostNotification = DateTimeOffset.Compare(notificationDateTime, lastCheckDateTime);

								if (shouldPostNotification >= 0)
								{
									// If the RSS post was posted after our last check, then post a notification
									// First, parse the title and description to remove HTML tags
									try
									{
										notificationTitle = Regex.Replace(notificationTitle, "<.*?>", 
											String.Empty, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));
									}
									catch (ArgumentException)
									{
										// A parse error occured, so just use the text from before parsing.
									}
									catch (RegexMatchTimeoutException)
									{
										// The regular expression timed out, either because the timeout limit was too low or the regex 
										// matcher was backtracking too much. In this instance, also use the text from before parsing.
									}

									try
									{
										notificationDescription = Regex.Replace(notificationDescription, "<.*?>",
											String.Empty, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));
									}
									catch (ArgumentException)
									{
										// A parse error occured, so just use the text from before parsing.
									}
									catch (RegexMatchTimeoutException)
									{
										// The regular expression timed out, either because the timeout limit was too low or the regex 
										// matcher was backtracking too much. In this instance, also use the text from before parsing.
									}
									
									// Now, create the actual toast content
									ToastContent toastContent = new ToastContent()
									{
										// Construct the visuals of the toast
										Visual = new ToastVisual()
										{
											BindingGeneric = new ToastBindingGeneric()
											{
												Children =
												{
													new AdaptiveText()
													{
														Text = notificationTitle
													},

													new AdaptiveText()
													{
														Text = notificationDescription
													},
												},

												Attribution = new ToastGenericAttributionText()
												{
													Text = notificationFeedTitle
												}
											}
										},

										// Arguments when the user taps body of toast
										Launch = new QueryString()
										{
											{ "action", "viewArticle"},
											{ "url", notificationUrl}
										}.ToString(),

										DisplayTimestamp = notificationDateTime,

										// Add button to view comments
										Actions = new ToastActionsCustom()
										{
											Buttons =
											{
												new ToastButton("View comments", new QueryString()
													{
														{ "action", "viewArticle" },
														{ "url", notificationUrl + "&comments=1" }
													}.ToString())
												{
													ActivationType = ToastActivationType.Foreground
												},
											}
										}
									};

									// Create and send the toast notification
									var toastNotification = new ToastNotification(toastContent.GetXml())
									{
										Group = notificationFeedTitle
									};
									ToastNotificationManager.CreateToastNotifier().Show(toastNotification);

									// Update number of notifications on the badge
									numberOfNotificationsOnBadge++;
								}
								else
								{
									// The user has already seen this notification and all the ones after it, so break
									break;
								}
							}
						}
					}
				}

				j++;
			}

			// Then create badges based off of the number of toast notifications
			// Badge notification code modified from 
			// https://docs.microsoft.com/en-us/windows/uwp/design/shell/tiles-and-notifications/badges 
			// under the MIT License. See the Licenses Dialog for the full MIT license.
			// Get the blank badge XML payload for a badge number
			XmlDocument badgeXml = BadgeUpdateManager.GetTemplateContent(BadgeTemplateType.BadgeNumber);

			// Set the value of the badge in the XML to the number of Notifications on the badge
			XmlElement badgeElement = badgeXml.SelectSingleNode("/badge") as XmlElement;
			badgeElement.SetAttribute("value", numberOfNotificationsOnBadge.ToString());

			// Create the badge notification
			BadgeNotification badge = new BadgeNotification(badgeXml);

			// Create the badge updater for the application and update the badge
			BadgeUpdateManager.CreateBadgeUpdaterForApplication().Update(badge);
			// End badge notification code.

			localSettings.Values["numberOfUnreadNotifications"] = numberOfNotificationsOnBadge;

			// Then Live Tiles
			foreach (string s in liveTileRssFeedUrls)
			{
				liveTileRssFeedXmlList.Add(await XmlDocument.LoadFromUriAsync(new Uri(s)));
			}

			j = 0;

			foreach (XmlDocument document in liveTileRssFeedXmlList)
			{
				notificationFeedTitle = liveTileFeedTitles[j];

				foreach (IXmlNode exteriorNode in document.DocumentElement.ChildNodes)
				{
					if (exteriorNode.LocalName != null && exteriorNode.LocalName.Equals("channel"))
					{
						foreach (IXmlNode interiorNode in exteriorNode.ChildNodes)
						{
							if (interiorNode.LocalName != null && interiorNode.LocalName.Equals("item"))
							{
								foreach (IXmlNode finalNode in interiorNode.ChildNodes)
								{
									if (finalNode.LocalName != null && finalNode.InnerText != null)
									{
										if (finalNode.LocalName.Equals("title"))
										{
											notificationTitle = finalNode.InnerText;
										}
										else if (finalNode.LocalName.Equals("link"))
										{
											notificationUrl = finalNode.InnerText;
										}
										else if (finalNode.LocalName.Equals("description"))
										{
											notificationDescription = finalNode.InnerText;
										}
										else if (finalNode.LocalName.Equals("pubDate"))
										{
											try
											{
												notificationDateTime = DateTimeOffset.Parse(finalNode.InnerText);
											}
											catch (ArgumentException)
											{
												// String did not exist or the offset was incorrect. Just use the date and time for right now.
											}
											catch (FormatException)
											{
												// This date was formatted incorrectly, so just use right now.
											}
										}
									}
								}

								int shouldPostNotification = DateTimeOffset.Compare(notificationDateTime, lastCheckDateTime);

								if (shouldPostNotification >= 0)
								{
									// If the RSS post was posted after our last check, then post it
									// First, parse the title and description to remove HTML tags
									try
									{
										notificationTitle = Regex.Replace(notificationTitle, "<.*?>",
											String.Empty, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));
									}
									catch (ArgumentException)
									{
										// A parse error occured, so just use the text from before parsing.
									}
									catch (RegexMatchTimeoutException)
									{
										// The regular expression timed out, either because the timeout limit was too low or the regex 
										// matcher was backtracking too much. In this instance, also use the text from before parsing.
									}

									try
									{
										notificationDescription = Regex.Replace(notificationDescription, "<.*?>",
											String.Empty, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));
									}
									catch (ArgumentException)
									{
										// A parse error occured, so just use the text from before parsing.
									}
									catch (RegexMatchTimeoutException)
									{
										// The regular expression timed out, either because the timeout limit was too low or the regex 
										// matcher was backtracking too much. In this instance, also use the text from before parsing.
									}

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
															Text = notificationTitle,
															HintWrap = true
														},

														new AdaptiveText()
														{
															Text = notificationDescription,
															HintStyle = AdaptiveTextStyle.CaptionSubtle,
															HintWrap = true
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
															Text = notificationTitle,
															HintStyle = AdaptiveTextStyle.Body,
															HintWrap = true
														},

														new AdaptiveText()
														{
															Text = notificationDescription,
															HintStyle = AdaptiveTextStyle.CaptionSubtle,
															HintWrap = true
														},

														new AdaptiveText()
														{
															Text = notificationFeedTitle,
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
															Text = notificationTitle,
															HintStyle = AdaptiveTextStyle.Subtitle,
															HintWrap = true
														},

														new AdaptiveText()
														{
															Text = notificationDescription,
															HintStyle = AdaptiveTextStyle.CaptionSubtle,
															HintWrap = true
														},

														new AdaptiveText()
														{
															Text = notificationFeedTitle,
															HintStyle = AdaptiveTextStyle.CaptionSubtle
														}
													}
												}
											}
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
								else
								{
									// The user has already seen this notification and all the ones after it, so break
									break;
								}
							}
						}
					}
				}

				j++;
			}

			// Save last check for updates value
			localSettings.Values["lastCheckForUpdatesDateTime"] = DateTimeOffset.UtcNow;

			// Complete the deferral
			_deferral.Complete();
		}
    }
}
