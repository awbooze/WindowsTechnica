using Windows.ApplicationModel.Background;
using Windows.Storage;

namespace Tasks
{
	public sealed class UpdateTask : IBackgroundTask
	{
		public void Run(IBackgroundTaskInstance taskInstance)
		{
			ApplicationData.Current.LocalSettings.Values["recentlyUpdated"] = true;
		}
	}
}
