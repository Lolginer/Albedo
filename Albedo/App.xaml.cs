using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Net;
using System.Windows.Media;
using System.IO;
using System.IO.Pipes;
using Albedo.Core;

namespace Albedo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
	/// 

    public partial class App : Application
    {
		static string identifier = "9af3f4a8-3282-4377-bd8c-Albedo";
		static Mutex mutex = new Mutex(false, identifier);
		void AppStartup(object sender, StartupEventArgs e)
		{
			try {
				if (!mutex.WaitOne(0, false)) {
					NamedPipeClientStream client = new NamedPipeClientStream(identifier);

					try {
						client.Connect(250);
					} catch (Exception) { }

					Environment.Exit(0);
				}
				GC.KeepAlive(mutex);
				NewPipe();
			} catch (Exception) { //Prevents crash if mutex is abandoned
				return;
			} finally {
				WebRequest.DefaultWebProxy = null;

				if (Platform.ReadSetting("newVersion") == true) {
					Platform.UpgradeSettings();
					Platform.WriteSetting("newVersion", false);
					Platform.SaveSettings();
				}

				if (Platform.ReadSetting("bridgeIP") != "0.0.0.0") {
					Storage.InitializeData();
				} else {
					WindowStorage.setupStorage = new SetupWindow();
					WindowStorage.setupStorage.Show();
				}

				//Set default accent
				dynamic tileColor = Storage.tileColors(0);
				SolidColorBrush newHighlight, newAccent, newAccent2, newAccent3, newAccent4;
				newHighlight = new BrushConverter().ConvertFromString(tileColor.border);
				newAccent = new BrushConverter().ConvertFromString(tileColor.border);
				newAccent2 = new BrushConverter().ConvertFromString(tileColor.border);
				newAccent3 = new BrushConverter().ConvertFromString(tileColor.border);
				newAccent4 = new BrushConverter().ConvertFromString(tileColor.border);

				System.Windows.Application.Current.Resources["HighlightColor"] = newHighlight;
				newAccent.Opacity = 0.75;
				System.Windows.Application.Current.Resources["AccentColorBrush"] = newAccent;
				System.Windows.Application.Current.Resources["WindowTitleColorBrush"] = newAccent;
				System.Windows.Application.Current.Resources["CheckmarkFill"] = newAccent;
				System.Windows.Application.Current.Resources["RightArrowFill"] = newAccent;
				newAccent2.Opacity = 0.60;
				System.Windows.Application.Current.Resources["AccentColorBrush2"] = newAccent2;
				newAccent3.Opacity = 0.40;
				System.Windows.Application.Current.Resources["AccentColorBrush3"] = newAccent3;
				newAccent4.Opacity = 0.20;
				System.Windows.Application.Current.Resources["AccentColorBrush4"] = newAccent4;
			}
		}

		static NamedPipeServerStream server;

		static void NewPipe() //Opens a new pipe to search for clients (other instances of Albedo)
		{
			server = new NamedPipeServerStream(identifier, PipeDirection.InOut, 5, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
			server.BeginWaitForConnection(new AsyncCallback(ClientDetect), null);
			return;
		}

		static void ClientDetect(IAsyncResult result) //Deals with NewPipe's connection attempt
		{
			WindowStorage.dummyStorage.TaskIcon.ShowBalloonTip("Albedo is already running", "Click on this icon to display Albedo's controls.", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.None);
			NewPipe();
			return;
		}
    }
}
