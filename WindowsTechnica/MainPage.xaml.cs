using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WindowsTechnica
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Uri currentUri;
        private string currentUrl;
        private string homeUrl = "https://arstechnica.com/";

        public MainPage()
        {
            this.InitializeComponent();

			DataTransferManager.GetForCurrentView().DataRequested += MainPage_DataRequested;	//Overrides data requested method to share stuff
		}

		/// <summary>
		/// A helper method for converting a URI to a string
		/// </summary>
		/// <param name="uri">The URI to convert to a string</param>
		/// <returns>A string representing this URI</returns>
		static string UriToString(Uri uri)
        {
            return (uri != null) ? uri.ToString() : "";
        }

        private void ArsWebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
			// Cancel navigation if URL is not allowed
			if (IsAllowedUri(args.Uri))
			{
				ArsProgressBar.ShowPaused = false;
				ArsProgressBar.Visibility = Visibility.Visible;    //Display the element
			}
			else
			{
				// Otherwise, the link is not for a page on Ars, so launch the system-provided browser
				IAsyncOperation<bool> b = Windows.System.Launcher.LaunchUriAsync(args.Uri);
				args.Cancel = true;
			}
        }

		private bool IsAllowedUri(Uri uri)
		{
			if(uri.Host.Contains("arstechnica.com"))
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		private void ArsWebView_ContentLoading(WebView sender, WebViewContentLoadingEventArgs args)
		{
			currentUri = args.Uri;
			currentUrl = UriToString(currentUri);
			//titleBar.Text = currentUrl.Substring(8);
			titleBar.Text = ArsWebView.DocumentTitle;

			if (ArsWebView.CanGoBack)
			{
				BackButton.IsEnabled = true;
			}
			else
			{
				BackButton.IsEnabled = false;
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

        private void ArsWebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
			ArsProgressBar.Visibility = Visibility.Collapsed;   //Collapse the Element
			ArsProgressBar.ShowPaused = true;
		}

		private void ArsWebView_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
		{
			//FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
		}

		private void ArsWebView_UnviewableContentIdentified(WebView sender, WebViewUnviewableContentIdentifiedEventArgs args)
		{
			IAsyncOperation<bool> b = Windows.System.Launcher.LaunchUriAsync(args.Uri);
			ArsWebView.Stop();
		}

		private void CommandBar_Loaded(object sender, RoutedEventArgs e)
		{
			if (ApiInformation.IsEventPresent("Windows.UI.Xaml.Controls.CommandBar", "DynamicOverflowItemsChanging"))
			{
				commandBar.IsDynamicOverflowEnabled = false;
			}
		}

		private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (ArsWebView.CanGoBack)
            {
                ArsWebView.GoBack();
            }
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (ArsWebView.CanGoForward)
            {
                ArsWebView.GoForward();
            }
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Uri targetUri = new Uri(homeUrl);
                ArsWebView.Navigate(targetUri);
            }
            catch (FormatException)
            {
                // Bad address.
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            //Refresh the webpage
            ArsWebView.Refresh();
        }

        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
			//Share the current url
			DataTransferManager.ShowShareUI();								//Shows share UI
		}

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            //Copy the current url to the clipboard
            copyToClipboard(currentUrl, currentUri);
            FlyoutBase.ShowAttachedFlyout(CopyButton);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
			//Open the settings screen
			this.Frame.Navigate(typeof(SettingsPage));
		}

        private void copyToClipboard (String Text, Uri WebLink)
        {
			DataPackage CopyDataPackage = new DataPackage
			{
				RequestedOperation = DataPackageOperation.Copy
			};
			CopyDataPackage.SetText(Text);
            CopyDataPackage.SetWebLink(WebLink);
            Clipboard.SetContent(CopyDataPackage);
        }

		private void MainPage_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)	//Actually does sharing
		{
			args.Request.Data.SetWebLink(currentUri);
			args.Request.Data.Properties.Title = ArsWebView.DocumentTitle;
			args.Request.Data.Properties.Description = UriToString(currentUri);
		}

		private void ShareFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            //Share target of hit test
        }

		private void CopyFlyoutItem_Click(object sender, RoutedEventArgs e)
		{
			//Copy target of hit test to clipboard
		}
	}
}
