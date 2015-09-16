using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Timers;
using Albedo.Core;

namespace Albedo
{
	/// <summary>
	/// Interaction logic for SetupWindow.xaml
	/// </summary>
	public partial class SetupWindow : Window
	{
		private class ComboItem
		{
			public string nameStore;
			public string idStore;
			public ComboItem(string name, string id)
			{
				idStore = id;
				nameStore = name;
			}

			public override string ToString()
			{
				return nameStore;
			}
		}

		System.Timers.Timer pairTimer = new System.Timers.Timer(2000);
		System.Timers.Timer searchTimer = new System.Timers.Timer(1000);
		System.Timers.Timer tutTimer = new System.Timers.Timer(250);
		int tutStep = 1;
		string bridgeIP;
		string bridgeUser;
		bool newUsername = false;
		
		public SetupWindow()
		{
			InitializeComponent();
			pairTimer.Elapsed += PairEvent;
			searchTimer.Elapsed += SearchEvent;
			tutTimer.Elapsed += TutEvent;
			Random randomID = new Random();
			bridgeUser = String.Format("albedo{0}", randomID.Next(10000000, 99999999).ToString());
			this.StartIndex();
		}

		private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			System.Diagnostics.Process.Start(e.Uri.ToString());
		}

		async private void StartIndex()
		{
			await Task.Delay(100); //Makes sure TabControl animation triggers

			string username = Platform.ReadSetting("bridgeUserName");
			if (username.Contains("albedo")) {
				bridgeIP = Platform.ReadSetting("bridgeIP");
				newUsername = true;
				SetupTabs.SelectedIndex = 3;
				return;
			}

			SetupTabs.SelectedIndex = 1;
			return;
		}

		private void Previous_Click(object sender, RoutedEventArgs e)
		{
			this.SetupTabs.SelectedIndex--;
		}

		private void Next_Click(object sender, RoutedEventArgs e)
		{
			this.SetupTabs.SelectedIndex++;
		}

		private void Close_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (Platform.ReadSetting("bridgeIP") == "0.0.0.0") {
				Environment.Exit(0);
			} else {
				Storage.opacityTarget = 1.0;
			}
		}

		private void BridgeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (BridgeCombo.SelectedIndex != -1) {
				BridgeNext.IsEnabled = true;
			} else {
				BridgeNext.IsEnabled = false;
			}
		}

		private void SetupTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (SetupTabs.SelectedIndex == 3) {
				Search(false);
				if (!newUsername) {
					ComboItem bridge = (ComboItem)BridgeCombo.SelectedItem;
					if (BridgeCombo.SelectedIndex != -1) {
						bridgeIP = bridge.idStore;
					} else {
						bridgeIP = ManualIP.Text;
					}
				}

				Pair(true);
			} else if (SetupTabs.SelectedIndex < 3) {
				Pair(false);
				newUsername = false;
				Search(true);
			} else {
				Pair(false);
				Search(false);
			}

			if (SetupTabs.SelectedIndex == 5) {
				Storage.activeTab = 0;
				Storage.accentBackup = 0;
				WindowStorage.newWindow = null;
				Tutorial();
			}

			if (SetupTabs.SelectedIndex == 6) {
				//Prevent overlap issues
				if (WindowStorage.newWindow != null) {
					Point textPoint = this.PointToScreen(new Point(600, 150));
					Point windowPoint = WindowStorage.newWindow.PointToScreen(new Point(0, 0));
					if (textPoint.Y > windowPoint.Y) {
						if (textPoint.X > windowPoint.X) {
							this.Left = 0;
							this.Top = 0;
						}
					}
				}
			}

		}

		bool showingBalloon = false;
		int balloonCount = 0;
		public void HereIAm()
		{
			if (!showingBalloon) {
				WindowStorage.dummyStorage.TaskIcon.ShowBalloonTip("I'm over here!", "Click on this icon to display Albedo's controls.", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.None);
				showingBalloon = true;
				balloonCount = 0;
			} else {
				balloonCount++;
				if (balloonCount > 40) { //Show another balloon after 10 seconds. The reset method doesn't seem to work.
					WindowStorage.dummyStorage.TaskIcon.HideBalloonTip();
					showingBalloon = false;
				}
			}
		}
		
		public void Pair(bool isOn)
		{
			if (isOn) {
				if (!pairTimer.Enabled) {
					pairTimer.Enabled = true;
				}
			} else {
				if (pairTimer.Enabled) {
					pairTimer.Enabled = false;
				}
			}
		}

		public void Search(bool isOn)
		{
			if (isOn) {
				if (!searchTimer.Enabled) {
					searchTimer.Interval = 1000;
					searchTimer.Enabled = true;
				}
			} else {
				if (searchTimer.Enabled) {
					searchTimer.Enabled = false;
				}
			}
		}

		public void Tutorial()
		{
			tutTimer.Enabled = true;
		}

		private void PairEvent(Object source, ElapsedEventArgs e)
		{
			this.Dispatcher.Invoke((Action)(async () =>
			{
				await Setup.PairAttempt(bridgeIP, bridgeUser); //bridgeUser isn't actually used anymore
				if (JsonParser.Read(Storage.tempData, new string[] { "success" }) != null) {
					string generatedName = JsonParser.Read(Storage.tempData, new string[] { "success", "username" });

					Properties.Settings.Default.bridgeIP = bridgeIP;
					Properties.Settings.Default.bridgeUserName = generatedName;
					Properties.Settings.Default.Save();
					this.SetupTabs.SelectedIndex++;
					Storage.InitializeData();
				}
			}));
		}

		private void SearchEvent(Object source, ElapsedEventArgs e)
		{
			this.Dispatcher.Invoke((Action)(async () =>
			{
				searchTimer.Interval = 8000;
				await Setup.FindAttempt();

				for (int i = BridgeCombo.Items.Count; i < Storage.addressArray.Count; i++) {
					string name = String.Format("Bridge {0} ({1})", i.ToString(), Storage.addressArray[i]);
					BridgeCombo.Items.Add(new ComboItem(name, Storage.addressArray[i]));
					if (BridgeCombo.SelectedIndex == -1) {
						BridgeCombo.SelectedIndex = 0;
					}
				}
			}));
		}

		private void TutEvent(Object source, ElapsedEventArgs e)
		{
			this.Dispatcher.Invoke((Action)(() =>
			{
				if (tutStep == 1) {
					if (WindowStorage.newWindow != null) {
						tutStep++;
						this.SetupTabs.SelectedIndex++;
					}
				} else if (tutStep == 2) {
					if (WindowStorage.newWindow != null) {
						if (WindowStorage.newWindow.MainTabs.SelectedIndex == 2) {
							tutStep++;
							this.SetupTabs.SelectedIndex++;
						}
					}
				} else if (tutStep == 3) {
					if (WindowStorage.newWindow != null) {
						if (WindowStorage.newWindow.MainTabs.SelectedIndex == 1) {
							tutStep++;
							this.SetupTabs.SelectedIndex++;
						}
					}
				} else if (tutStep == 4) {
					if (Storage.accentBackup != 0) {
						tutStep++;
						this.SetupTabs.SelectedIndex++;
						tutTimer.Enabled = false;
					}
				}

				if (WindowStorage.newWindow == null && tutStep <= 4) {
					HereIAm();
				} else {
					showingBalloon = false;
				}
			}));
		}

		private void ManualIP_TextChanged_1(object sender, TextChangedEventArgs e)
		{
			if (ManualIP.Text != "") {
				BridgeNext.IsEnabled = true;
			} else if (BridgeCombo.SelectedIndex != -1) {
				BridgeNext.IsEnabled = true;
			} else {
				BridgeNext.IsEnabled = false;
			}
		}
	}
}
