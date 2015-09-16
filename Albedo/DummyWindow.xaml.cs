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
using System.Windows.Forms;
using System.Windows.Interop;
using System.Drawing;
using Albedo.Core;

namespace Albedo
{
	/// <summary>
	/// Interaction logic for DummyWindow.xaml
	/// </summary>
	public partial class DummyWindow : Window
	{
		public static bool dummyInitialized = false; //Prevents ToolTipUpdate from triggering too early
		Icon taskbarIcon = new System.Drawing.Icon(Albedo.Properties.Resources.taskbarIcon, SystemInformation.SmallIconSize);

		public DummyWindow()
		{
			InitializeComponent();
			WindowStorage.dummyStorage = this;
			dummyInitialized = true;
			ToolTipUpdate();
			TaskIcon.Icon = taskbarIcon;
			WindowStorage.newWindow = new MainWindow(); //Speeds up first MainWindow launch
		}

		public static void ToolTipUpdate()
		{
			if (dummyInitialized) {
				string groupName;
				if (AddressBuild.selectedGroup == "0") {
					groupName = "All lights";
				} else {
					if (Storage.latestData != null) {
						groupName = JsonParser.Read(Storage.latestData, new string[] { "groups", AddressBuild.selectedGroup, "name" });
					} else {
						groupName = AddressBuild.selectedGroup;
					}
				}

				WindowStorage.dummyStorage.TaskIcon.ToolTipText = String.Format("Albedo\nGroup: {0}", groupName);
			}
		}

		private bool Ready()
		{
			string username = Platform.ReadSetting("bridgeUserName");
			if (Platform.ReadSetting("bridgeIP") != "0.0.0.0" && !username.Contains("albedo")) {
				return false;
			} else {
				return true;
			}
		}

		private void LightsOnItem_Click(object sender, RoutedEventArgs e)
		{
			if (Ready()) {
				foreach (string light in Storage.groupData.lights) {
					PutEvents.ToggleLight(light, true);
					PutEvents.ChangeAccent(Storage.accentBackup);
				}
			}
		}

		private void LightsOffItem_Click(object sender, RoutedEventArgs e)
		{
			if (Ready()) {
				foreach (string light in Storage.groupData.lights) {
					PutEvents.ToggleLight(light, false);
					PutEvents.ChangeAccent(9, false);
				}
			}
		}

		private void ExitItem_Click(object sender, RoutedEventArgs e)
		{
			this.TaskIcon.Visibility = System.Windows.Visibility.Collapsed; //Prevents task icon from remaining in tray
			Environment.Exit(0);
		}

		public void Exit()
		{
			this.TaskIcon.Visibility = System.Windows.Visibility.Collapsed; //Prevents task icon from remaining in tray
			Environment.Exit(0);
		}

		private void AmbientItem_Click(object sender, RoutedEventArgs e)
		{
			if (Ready()) {
				if (AmbientItem.IsChecked) {
					Effects.ModeOn("ambient");
				} else {
					Effects.EffectsOff();
				}
			}
		}

		private void DaylightItem_Click(object sender, RoutedEventArgs e)
		{
			if (Ready()) {
				if (DaylightItem.IsChecked) {
					Effects.ModeOn("daylight");
				} else {
					Effects.EffectsOff();
				}
			}
		}

		private void TaskIcon_TrayBalloonTipClicked(object sender, RoutedEventArgs e)
		{
			ShowClass.ShowMain();
		}
		
	}

	public class ShowWindowCommand : ICommand
	{
		//Displays MainWindow after clicking on TaskIcon
		public void Execute(object parameter)
		{
			ShowClass.ShowMain();
		}

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public event EventHandler CanExecuteChanged { add{} remove{} }
	}

	public static class ShowClass
	{
		static bool showing = false; //Prevents two windows from opening simultaneously

		async public static void ShowMain()
		{
			if (!showing) {
				showing = true;
				if (WindowStorage.newWindow != null) {
					WindowStorage.newWindow.Close();
				}

				if (Platform.ReadSetting("bridgeIP") != "0.0.0.0") {
					WindowStorage.newWindow = new MainWindow();
					WindowStorage.newWindow.Show();
				}

				await Task.Delay(500);
				showing = false;
			}
		}
	}

	//Stores public references to DummyWindow and latest MainWindow
	public static class WindowStorage
	{
		public static MainWindow newWindow = null;
		public static DummyWindow dummyStorage = null;
		public static EditorWindow editorStorage = null;
		public static SetupWindow setupStorage = null;
	}
}
