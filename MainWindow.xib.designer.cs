// WARNING
//
// This file has been generated automatically by MonoDevelop to store outlets and
// actions made in the Xcode designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoTouch.Foundation;

namespace WhisperJar
{
	[Register ("AppDelegate")]
	partial class AppDelegate
	{
		[Outlet]
		MonoTouch.UIKit.UIWindow window { get; set; }

		[Outlet]
		MonoTouch.UIKit.UINavigationController viewController { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIButton button { get; set; }

		[Outlet]
		MonoTouch.UIKit.UIProgressView recordProgress { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITableView tableView { get; set; }

		[Action ("toggleRecording:")]
		partial void toggleRecording (MonoTouch.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (window != null) {
				window.Dispose ();
				window = null;
			}

			if (viewController != null) {
				viewController.Dispose ();
				viewController = null;
			}

			if (button != null) {
				button.Dispose ();
				button = null;
			}

			if (recordProgress != null) {
				recordProgress.Dispose ();
				recordProgress = null;
			}

			if (tableView != null) {
				tableView.Dispose ();
				tableView = null;
			}
		}
	}
}
