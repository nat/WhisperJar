
//
// This is a sample program that streams audio from an HTTP server using
// Mono's HTTP stack, AudioStreamFile to parse partial audio streams and
// AudioQueue/OutputAudioQueue to generate the output.
//
// MIT X11
//
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.Net;
using MonoTouch.AudioToolbox;
using MonoTouch.AVFoundation;
using MonoTouch.Dialog;
using System.Threading.Tasks;
using System.Drawing;
using SQLite;

namespace WhisperJar
{
	public class Application
	{
		static void Main (string[] args)
		{
			UIApplication.Main (args);
		}
	}
	
	public class Whisper 
	{
		[PrimaryKey, AutoIncrement]    
		public int Id { get; set; }

		[MaxLength(32)]
	    public string Name { get; set; }
    	public DateTime CreatedAt { get; set; }
		public string FileName { get; set; }
		public int Length { get; set; }
		
		private DateTime _startedRecordingAt;
		private AVAudioRecorder _recorder;
		private AVAudioPlayer _player;

		public Whisper () : base()
		{
			CreatedAt = DateTime.Now;
			Name = "";
		}
		
		public void StartRecording ()
		{
			FileName = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments), 
			                                     String.Format ("whisper-{0}.wav", Id));
			SimpleAudio.StartRecording(FileName);
			_startedRecordingAt = DateTime.Now;
		}
		
		public void StopRecording (Action finishedRecording)
		{
			SimpleAudio.StopRecording(delegate {
				Length = (int) (DateTime.Now - _startedRecordingAt).TotalMilliseconds;

				finishedRecording();
			});		                
		}
		
		public void Play ()
		{
			Console.WriteLine ("Playing audio in whisper");
			SimpleAudio.PlayFile(FileName);
		}
		
		public override string ToString ()
		{
			return string.Format ("{0} ({1}s)", Name, Length / 1000);
		}
	}
	
	public class Database : SQLiteConnection
	{			
		public Database (string path) : base(path)
		{
			CreateTable<Whisper> ();
		}
	}
	
	public class SimpleAudio {
		static AVAudioRecorder _recorder;
		static AVAudioPlayer _player;
		
		public static void PlayFile(string FileName)
		{
			var url = NSUrl.FromFilename(FileName);
			_player = AVAudioPlayer.FromUrl(url);
			_player.Play();
		}
		
		public static void StartRecording(string FileName)
		{
            NSObject[] values = new NSObject[]
            {    
                NSNumber.FromFloat(44100.0f),
                NSNumber.FromInt32((int)AudioFormatType.LinearPCM),
                NSNumber.FromInt32(1),
                NSNumber.FromInt32((int)AVAudioQuality.High)
            };

            NSObject[] keys = new NSObject[]
            {
                AVAudioSettings.AVSampleRateKey,
                AVAudioSettings.AVFormatIDKey,
                AVAudioSettings.AVNumberOfChannelsKey,
                AVAudioSettings.AVEncoderAudioQualityKey
            };

            NSDictionary settings = NSDictionary.FromObjectsAndKeys (values, keys);
            NSUrl url = NSUrl.FromFilename(FileName);
            
            // Set recorder parameters
            NSError error;
            _recorder = AVAudioRecorder.ToUrl (url, settings, out error);
			if (_recorder == null){
				Console.WriteLine (error);
				return;
			}
            
            // Set Metering Enabled so you can get the time of the wav file
            _recorder.MeteringEnabled = true;
            _recorder.PrepareToRecord();            
            _recorder.Record();
		}

		public static void StopRecording(Action finishedRecording)
		{
            _recorder.Stop();
			_recorder.FinishedRecording += delegate {
				_recorder.Dispose();
				finishedRecording();
			};
		}
	}
	
	public class WhisperElement : StringElement {
		Whisper _whisper;
		
		public WhisperElement(Whisper w)
			: base (w.ToString ()) {
			_whisper = w;
		}
		
		public override void Selected (DialogViewController dvc, UITableView tableView, NSIndexPath indexPath)
		{
			base.Selected (dvc, tableView, indexPath);
			Task.Factory.StartNew (() => {
				_whisper.Play ();
			});
		}
		
	}
	
	public partial class AppDelegate : UIApplicationDelegate
	{
		DateTime startedRecording;
		bool recording = false;
		NSTimer timer;
		int maxLength = 10;
		DialogViewController whisperList;
		Database _db;
		Whisper _recordingWhisper;
		
		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			whisperList = new DialogViewController(UITableViewStyle.Plain, new RootElement("Whispers"));			
			window.RootViewController = viewController;
			window.MakeKeyAndVisible ();

			Task.Factory.StartNew (() => {
				var dbPath = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments), "whispers.db");
				_db = new Database (dbPath);

				viewController.AddChildViewController (whisperList);
				whisperList.View.Frame = new RectangleF(0, 60, viewController.View.Frame.Width, viewController.View.Frame.Height - 130);
				
				whisperList.Root.Add (new Section("") {});
				
				foreach (Whisper w in _db.Query<Whisper>("select * from Whisper where Length > 0")) {
					whisperList.Root[0].Add (new WhisperElement(w));
				}
				
				BeginInvokeOnMainThread(() => {
					viewController.View.AddSubview (whisperList.View);
				});
			});
			
			AudioSession.Initialize ();
			return true;
		}

		// Action hooked up on Interface Builder
		partial void toggleRecording (NSObject sender)
		{
			UIButton button = (UIButton) sender;
			
			if (! recording) {
				Task.Factory.StartNew (() => {
					StartRecording (button);
				});
			} else {
				StopRecording (button);
			}

			recording = !recording;
		}
		
		void RecordingTimer(UIButton button)
		{			
			double timeSinceRecording = (DateTime.Now - startedRecording).TotalMilliseconds;
			recordProgress.Progress = (float) timeSinceRecording/(maxLength*1000f);
			if (timeSinceRecording >= maxLength*1000) {
				StopRecording (button);
			}
		}

		public void StartRecording (UIButton button)
        {
			button.Enabled = false;
			
			_recordingWhisper = new Whisper ();
			_db.Insert (_recordingWhisper); 
			_recordingWhisper.StartRecording ();
			
			BeginInvokeOnMainThread(() => {
				recordProgress.Hidden = false;
				recordProgress.Progress = 0f;
				button.SetTitle ("Stop", UIControlState.Normal);

				startedRecording = DateTime.Now;
				timer = NSTimer.CreateRepeatingScheduledTimer(TimeSpan.FromSeconds(0.1), delegate {
					RecordingTimer (button);
				});
				
				button.Enabled = true;
			});
        }
		
        public void StopRecording (UIButton button)
        {
			button.SetTitle ("Record", UIControlState.Normal);

			_recordingWhisper.StopRecording(delegate {	
                Console.WriteLine("Done Recording");
				timer.Invalidate();
				timer = null;
				recordProgress.Hidden = true;
				
				_db.Update (_recordingWhisper);
				whisperList.Root[0].Add (new WhisperElement(_recordingWhisper));
            });
		}
	}
}
