using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using MahApps.Metro;
using MahApps.Metro.Controls;
using Albedo.Core;

namespace Albedo
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	/// 

	//LightSource is actually used to define scenes in Tile controls.
	public class LightProperty : DependencyObject
	{
		public static string GetLightSource(DependencyObject obj)
		{
			return (string)obj.GetValue(SourceProperty);
		}
		public static void SetLightSource(DependencyObject obj, string value)
		{
			obj.SetValue(SourceProperty, value);
		}

		public static readonly DependencyProperty SourceProperty = DependencyProperty.RegisterAttached("LightSource",
						 typeof(string), typeof(LightProperty), new UIPropertyMetadata(""));
	}

	public partial class MainWindow : MetroWindow, IDisposable
	{
		public MainWindow()
		{
			InitializeComponent();
			if (Platform.ReadSetting("bridgeIP") != "0.0.0.0") {
				this.Opacity = 0.01;
				this.AllowsTransparency = true;
				this.MainTabs.SelectedIndex = Storage.activeTab;
				SetInfo();
				FadeIn();
				WindowStorage.dummyStorage.TaskIcon.HideBalloonTip();
			}
		}

		public bool slidersAllowed = false; //Prevents SetInfo from changing the actual light states

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

		public void SetInfo()
		{
			slidersAllowed = false;
			int i = 1;

			//Populate Lights tab
			foreach (string lightLabel in Storage.groupData.lights) {
				if (i <= 5) {
					string labelName = String.Format("SliderName{0}", i);
					string sliderName = String.Format("Slider{0}", i);
					System.Windows.Controls.Label nameRef = (System.Windows.Controls.Label)this.FindName(labelName);
					Slider sliderRef = (Slider)this.FindName(sliderName);

					LightProperty.SetLightSource(nameRef, lightLabel);
					LightProperty.SetLightSource(sliderRef, lightLabel);

					nameRef.Content = JsonParser.Read(Storage.latestData, new string[] { "lights", lightLabel, "name" });
					sliderRef.Value = JsonParser.Read(Storage.latestData, new string[] { "lights", lightLabel, "state", "bri" });
					if (JsonParser.Read(Storage.latestData, new string[] { "lights", lightLabel, "state", "on" }) == true && !Effects.effectsOn) {
						sliderRef.IsEnabled = true;
						sliderRef.Opacity = 0.85;
						MegaSlider.IsEnabled = true;
						MegaSlider.Opacity = 0.85;
					}
					i++;
				}
			}
			UpdateMega();

			//Set effect switches
			if (Effects.effectsOn) {
				switch (Effects.effectMode) {
					case "test":
						break;
					case "ambient":
						this.AmbientSwitch.IsChecked = true;
						break;
					case "daylight":
						this.DaylightSwitch.IsChecked = true;
						break;
				}
			}
			
			
			//Populate group settings
			this.GroupCombo.Items.Clear();
			this.GroupCombo.Items.Add(new ComboItem("All lights","0"));
			this.GroupCombo.SelectedIndex = 0;
			i = 1;
			foreach (dynamic group in Storage.latestData.groups) {
				this.GroupCombo.Items.Add(new ComboItem(JsonParser.Read(Storage.latestData, new string[] { "groups", group.Key, "name" }), group.Key));
				if (group.Key == Properties.Settings.Default.bridgeGroup) {
					this.GroupCombo.SelectedIndex = i;
				}
				i++;
			}

			//Populate scene settings
			for (int i2 = 1; i2 <= 8; i2++) {
				string comboName = String.Format("Tile{0}Combo", i2);
				string settingName = String.Format("customSelected{0}", i2);
				System.Windows.Controls.ComboBox comboRef = (System.Windows.Controls.ComboBox)this.FindName(comboName);

				comboRef.Items.Clear();
				comboRef.Items.Add(new ComboItem("None", ""));
				comboRef.SelectedIndex = 0;
				i = 1;
                foreach (dynamic scene in Storage.defaultScenes)
                {
                    comboRef.Items.Add(new ComboItem(JsonParser.Read(Storage.defaultScenes, new string[] { scene.Key, "name" }), scene.Key));
                    if (scene.Key == (string)Properties.Settings.Default[settingName])
                    {
                        comboRef.SelectedIndex = i;
                    }
                    i++;
                }
				foreach (dynamic scene in Storage.sceneData) {
					comboRef.Items.Add(new ComboItem(JsonParser.Read(Storage.sceneData, new string[] { scene.Key, "name" }), scene.Key));
					if (scene.Key == (string)Properties.Settings.Default[settingName]) {
						comboRef.SelectedIndex = i;
					}
					i++;
				}
			}

			//Populate Scenes tab
			for (int i2 = 1; i2 <= 8; i2++) {
				Tile tileRef = (Tile)this.FindName(String.Format("Tile{0}", i2));
				string tileName = (string)Properties.Settings.Default[String.Format("customSelected{0}", i2)];
				if (tileName != "") {
					if (JsonParser.Read(Storage.sceneData, new string[] { tileName }) != null) {
						tileRef.Title = JsonParser.Read(Storage.sceneData, new string[] { tileName, "name" });
						LightProperty.SetLightSource(tileRef, tileName);
						dynamic tileColor = Storage.tileColors((int)JsonParser.Read(Storage.sceneData, new string[] { tileName, "tilecolor" }));

						tileRef.Background = new BrushConverter().ConvertFromString(tileColor.background);
						tileRef.BorderBrush = new BrushConverter().ConvertFromString(tileColor.border);
						if (tileColor.blackText) {
							Brush black = new SolidColorBrush(Color.FromArgb(255,0,0,0));
							tileRef.Foreground = black;
						}
					} else if (JsonParser.Read(Storage.defaultScenes, new string[] { tileName }) != null) {
						tileRef.Title = JsonParser.Read(Storage.defaultScenes, new string[] { tileName, "name" });
						LightProperty.SetLightSource(tileRef, tileName);
						dynamic tileColor = Storage.tileColors((int)JsonParser.Read(Storage.defaultScenes, new string[] { tileName, "tilecolor" }));

						tileRef.Background = new BrushConverter().ConvertFromString(tileColor.background);
						tileRef.BorderBrush = new BrushConverter().ConvertFromString(tileColor.border);
						if (tileColor.blackText) {
							Brush black = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
							tileRef.Foreground = black;
						}
					} else {
						Properties.Settings.Default[String.Format("customSelected{0}", i2)] = "";
						tileRef.IsEnabled = false;
					}
				} else {
					tileRef.IsEnabled = false;
				}
			}

			slidersAllowed = true;
		}

		System.Timers.Timer opacityTimer = new System.Timers.Timer(10);
		public void FadeIn()
		{
			opacityTimer.Elapsed += FadeInEvent;
			opacityTimer.Enabled = true;
		}

		private void FadeInEvent(Object source, ElapsedEventArgs e)
		{
			this.Dispatcher.Invoke((Action)(() => {
				this.Opacity += 0.1;
				this.Activate();
				this.Focus();
				if (this.Opacity >= Storage.opacityTarget) {
					opacityTimer.Enabled = false;
					this.Opacity = Storage.opacityTarget;
				}
			}));
		}

		//Used to close windows after selecting scenes
		System.Timers.Timer fadeOutTimer = new System.Timers.Timer(10);
		public void FadeOut()
		{
			fadeOutTimer.Elapsed += FadeOutEvent;
			fadeOutTimer.Enabled = true;
		}

		private void FadeOutEvent(Object source, ElapsedEventArgs e)
		{
			this.Dispatcher.Invoke((Action)(() =>
			{
				this.Opacity -= 0.1;
				if (this.Opacity < 0.1) {
					fadeOutTimer.Enabled = false;
					this.Activate();
					this.Close();
				}
			}));
		}

		//Disposes of timers
		private void DisposeOfTimers()
		{
			fadeOutTimer.Dispose();
			opacityTimer.Dispose();
		}

		//Moves MainWindow into the bottom-right corner
		private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
		{
			var desktopArea = System.Windows.SystemParameters.WorkArea;
			this.Left = desktopArea.Right - this.Width;
			this.Top = desktopArea.Bottom - this.Height;
		}

		private void LightsOff_Click(object sender, RoutedEventArgs e)
		{
			foreach (string light in Storage.groupData.lights) {
				PutEvents.ToggleLight(light, false);
			}
			ToggleAllSliders(false);
			PutEvents.ChangeAccent(9, false);
			FadeOut();
		}

		private void LightsOn_Click(object sender, RoutedEventArgs e)
		{
			foreach (string light in Storage.groupData.lights) {
				PutEvents.ToggleLight(light, true);
			}
			ToggleAllSliders(true);
			PutEvents.ChangeAccent(Storage.accentBackup);
			FadeOut();
		}

		//Disables/enables sliders when toggling lights
		private void ToggleAllSliders(bool isEnabled)
		{
			for (int i = 1; i <= Storage.groupData.lights.Length; i++) {
				string sliderName = String.Format("Slider{0}", i);
				Slider sliderRef = (Slider)this.FindName(sliderName);
				
				sliderRef.IsEnabled = isEnabled;
				if (isEnabled) {
					sliderRef.Opacity = 0.85;
				} else {
					sliderRef.Opacity = 0.35;
				}
			}

			MegaSlider.IsEnabled = isEnabled;
			if (isEnabled) {
				MegaSlider.Opacity = 0.85;
			} else {
				MegaSlider.Opacity = 0.35;
			}
		}

		static bool usingSlider = false;
		static bool usingMega = false;

		private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (slidersAllowed && !usingMega) {
				PutEvents.ChangeBrightness(LightProperty.GetLightSource((Slider)sender), (int)((Slider)sender).Value);
				UpdateMega();
			}
		}

		static double oldMega = 1;
		private void MegaSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			double fraction = MegaSlider.Value / oldMega;

			if (slidersAllowed && !usingSlider) {
				usingMega = true;

				double approachFraction;
				if (fraction > 1) {
					approachFraction = (MegaSlider.Value - oldMega) / (MegaSlider.Maximum - oldMega);
				} else { approachFraction = 1; }

				foreach (string light in Storage.groupData.lights) {
					double brightness = JsonParser.Read(Storage.latestData, new string[] { "lights", light, "state", "bri" });
					if (fraction > 1) {
						brightness += (254 - brightness) * approachFraction;
					} else {
						brightness *= fraction;
					}
					PutEvents.ChangeBrightness(light, (int)brightness);
				}

				//Visual change only
				for (int i = 1; i <= 5; i++) {
					string sliderName = String.Format("Slider{0}", i);
					Slider sliderRef = (Slider)this.FindName(sliderName);

					if (sliderRef.IsEnabled) {
						if (fraction > 1) {
							sliderRef.Value += (sliderRef.Maximum - sliderRef.Value) * approachFraction;
						} else {
							sliderRef.Value *= fraction;
						}
					}
				}

				usingMega = false;
			}

			oldMega = MegaSlider.Value;
		}

		private void UpdateMega()
		{
			usingSlider = true;

			double valueSum = 0;
			int sliderCount = 5, off = 0;

			for (int i = 1; i <= sliderCount; i++) {
				string sliderName = String.Format("Slider{0}", i);
				Slider sliderRef = (Slider)this.FindName(sliderName);

				if (sliderRef.IsEnabled) {
					valueSum += sliderRef.Value;
				} else { off++; }
			}

			MegaSlider.Value = valueSum / (sliderCount - (off == sliderCount ? 1 : off)); //Average value; conditional prevents division by zero
			usingSlider = false;
		}

		private bool CanDeactivate = true; //Prevents Deactivated event from trying to close a closed window

		private void MetroWindow_Deactivated(object sender, EventArgs e)
		{
			if (CanDeactivate == true) {
				WindowStorage.newWindow = null;
				this.Activate();
				this.Close();
			}
		}

		private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Storage.activeTab = this.MainTabs.SelectedIndex;
			Properties.Settings.Default.lastActiveTab = Storage.activeTab;
			Properties.Settings.Default.Save();
			WindowStorage.newWindow = null;
			CanDeactivate = false;
			var result = Storage.RefreshData();
		}

		bool isAbout = false;
		private void Settings_Click(object sender, RoutedEventArgs e)
		{
			if (isAbout) {
				AboutWindow about = new AboutWindow();
				about.Show();
			} else {
				this.SettingsFlyout.IsOpen = !this.SettingsFlyout.IsOpen;
				this.SettingsButton.Content = String.Format("About (version {0}.{1})", Storage.fileVersion[0].ToString(), Storage.fileVersion[1].ToString());
				isAbout = true;
			}
		}

		private void Tile_Click(object sender, RoutedEventArgs e)
		{
			bool hardCoded;

			if (LightProperty.GetLightSource(((Tile)sender)).Substring(0, 6) == "scene_") {
				hardCoded = false;
			} else {
				hardCoded = true;
			}

			PutEvents.ChangeScene(LightProperty.GetLightSource(((Tile)sender)), hardCoded);
			SetInfo();
			FadeOut();
		}

		private void AmbientSwitch_Click(object sender, RoutedEventArgs e)
		{
			if ((bool)AmbientSwitch.IsChecked) {
				Effects.ModeOn("ambient");
				FadeOut();
			} else {
				Effects.EffectsOff();
				FadeOut();
			}
		}

		private void DaylightSwitch_Click(object sender, RoutedEventArgs e)
		{
			if ((bool)DaylightSwitch.IsChecked) {
				Effects.ModeOn("daylight");
				FadeOut();
			} else {
				Effects.EffectsOff();
				FadeOut();
			}
		}

		private bool CloseAfterSettings = false;

		private void GroupCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (slidersAllowed) {
				ComboItem temp = (ComboItem)GroupCombo.SelectedItem;
				Properties.Settings.Default.bridgeGroup = temp.idStore;
				Storage.InitializeData();
				CloseAfterSettings = true;
			}
		}

		private void SettingsFlyout_IsOpenChanged(object sender, RoutedEventArgs e)
		{
			if (!this.SettingsFlyout.IsOpen) {
				this.SettingsButton.Content = "Settings";
				isAbout = false;
			}
			if (CloseAfterSettings) {
				if (CanDeactivate == true) {
					WindowStorage.newWindow = null;
					this.Activate();
					this.Close();
				}
			}
		}

		private void TileCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (slidersAllowed) {
				System.Windows.Controls.ComboBox tileRef = (System.Windows.Controls.ComboBox)sender;
				string settingName = LightProperty.GetLightSource(tileRef);
				ComboItem temp = (ComboItem)tileRef.SelectedItem;
				Properties.Settings.Default[settingName] = temp.idStore;
				CloseAfterSettings = true;
			}
		}

		private void EditorButton_Click(object sender, RoutedEventArgs e)
		{
			if (WindowStorage.editorStorage == null) {
				CanDeactivate = false;
				WindowStorage.newWindow = null;
				this.Close();
				WindowStorage.editorStorage = new EditorWindow();
				WindowStorage.editorStorage.Show();
			} else {
				WindowStorage.editorStorage.Activate();
				WindowStorage.editorStorage.Focus();
			}

			if (CanDeactivate == true) {
				CanDeactivate = false;
				WindowStorage.newWindow = null;
				this.Close();
			}
		}

		public interface IDisposable
		{
			void Dispose();
		}

		public void Dispose()
		{
			DisposeOfTimers();
		}
	}
}
