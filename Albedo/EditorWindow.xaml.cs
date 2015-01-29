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
using System.Dynamic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Albedo.Core;

namespace Albedo
{
	/// <summary>
	/// Interaction logic for EditorWindow.xaml
	/// </summary>
	public partial class EditorWindow : Window
	{
		private class ComboItem
		{
			public string nameStore;
			public string idStore;
			public int idStore2;
			public ComboItem(string name, string id, int id2 = 0)
			{
				idStore = id;
				idStore2 = id2;
				nameStore = name;
			}

			public override string ToString()
			{
				return nameStore;
			}
		}

		public EditorWindow()
		{
			InitializeComponent();
			InitSceneInfo();
			this.Activate();
			this.Focus();
		}

		public bool slidersAllowed = false; //Prevents SetInfo from changing the actual light states

		public void InitSceneInfo() //This should probably be a lot shorter. Break down into smaller/reusable bits.
		{
			slidersAllowed = false;
			int i = 1;

			//Set slider data
			foreach (string lightLabel in Storage.groupData.lights) {
				if (i <= 5) {
					string labelName = String.Format("SliderName{0}", i);
					string briSliderName = String.Format("BriSlider{0}", i);
					string hueSliderName = String.Format("HueSlider{0}", i);
					string satSliderName = String.Format("SatSlider{0}", i);
					System.Windows.Controls.Label nameRef = (System.Windows.Controls.Label)this.FindName(labelName);
					Slider sliderRef = (Slider)this.FindName(briSliderName);
					Slider sliderRef2 = (Slider)this.FindName(hueSliderName);
					Slider sliderRef3 = (Slider)this.FindName(satSliderName);


					LightProperty.SetLightSource(nameRef, lightLabel);
					LightProperty.SetLightSource(sliderRef, lightLabel);
					LightProperty.SetLightSource(sliderRef2, lightLabel);
					LightProperty.SetLightSource(sliderRef3, lightLabel);

					nameRef.Content = JsonParser.Read(Storage.latestData, new string[] { "lights", lightLabel, "name" });
					sliderRef.Value = JsonParser.Read(Storage.latestData, new string[] { "lights", lightLabel, "state", "bri" });
					sliderRef2.Value = JsonParser.Read(Storage.latestData, new string[] { "lights", lightLabel, "state", "hue" });
					sliderRef3.Value = JsonParser.Read(Storage.latestData, new string[] { "lights", lightLabel, "state", "sat" });
					if (JsonParser.Read(Storage.latestData, new string[] { "lights", lightLabel, "state", "on" }) == true && !Effects.effectsOn) {
						sliderRef.IsEnabled = true;
						sliderRef.Opacity = 0.85;
						sliderRef2.IsEnabled = true;
						sliderRef2.Opacity = 0.85;
						sliderRef3.IsEnabled = true;
						sliderRef3.Opacity = 0.85;
					}
					i++;
				}
			}

			//Set current scene
			this.SceneCombo.Items.Add(new ComboItem("New scene", ""));
			this.SceneCombo.SelectedIndex = 0;
			
			i = 1;
			foreach (dynamic scene in Storage.sceneData) {
				this.SceneCombo.Items.Add(new ComboItem(JsonParser.Read(Storage.sceneData, new string[] { scene.Key, "name" }), scene.Key));
				if (SceneEdit.IsSceneMatch(scene.Key)) {
					this.SceneCombo.SelectedIndex = i;
				}
				i++;
			}

			//Populate group settings
			this.GroupCombo.Items.Clear();
			this.GroupCombo.Items.Add(new ComboItem("All lights", "0"));
			this.GroupCombo.SelectedIndex = 0;
			i = 1;
			foreach (dynamic group in Storage.latestData.groups) {
				this.GroupCombo.Items.Add(new ComboItem(JsonParser.Read(Storage.latestData, new string[] { "groups", group.Key, "name" }), group.Key));
				if (group.Key == Properties.Settings.Default.bridgeGroup) {
					this.GroupCombo.SelectedIndex = i;
				}
				i++;
			}

			//Populate light dropdowns
			for (int n = 1; n <= 5; n++) {
				string comboName = String.Format("GroupLight{0}", n);
				ComboBox comboRef = (ComboBox)this.FindName(comboName);

				if (n != 1) {
					comboRef.Items.Add(new ComboItem("None", ""));
				}

				foreach (dynamic light in Storage.latestData.lights) {
					string jsonName = JsonParser.Read(light.Value, new string[] { "name" });
					string jsonModel = JsonParser.Read(light.Value, new string[] { "modelid" });
					string friendlyModel = "Unknown light type";

					if (jsonModel.Contains("LCT")) {
						friendlyModel = "hue";
					} else if (jsonModel.Contains("LLC")) {
						friendlyModel = "LivingColors";
					} else if (jsonModel.Contains("LWB")) {
						friendlyModel = "hue lux";
					} else if (jsonModel.Contains("LWL")) {
						friendlyModel = "LivingWhites";
					} else if (jsonModel.Contains("LST")) {
						friendlyModel = "LightStrips";
					}

					string name = String.Format("{0} ({1})", jsonName, friendlyModel);
					comboRef.Items.Add(new ComboItem(name, light.Key));

					if (Storage.groupData.lights.Length >= n) {
						if (Storage.groupData.lights[n - 1] == light.Key) {
							comboRef.SelectedIndex = comboRef.Items.Count - 1;
						}
					}
				}

				if (comboRef.SelectedIndex == -1) {
					comboRef.SelectedIndex = 0;
				}
			}

			//Populate auto-effect dropdown
			DefaultCombo.Items.Add(new ComboItem("None", "", 0));
			DefaultCombo.SelectedIndex = 0;
			i = 1;

			DefaultCombo.Items.Add(new ComboItem("Ambient mode", "ambient", 1));
			if (Properties.Settings.Default.autoEffect == 1) { //There's probably a cleaner way to handle this.
				if (Properties.Settings.Default.autoName == "ambient") {
					DefaultCombo.SelectedIndex = i;
				}
			}
			i++;

			DefaultCombo.Items.Add(new ComboItem("Daylight mode", "daylight", 1));
			if (Properties.Settings.Default.autoEffect == 1) {
				if (Properties.Settings.Default.autoName == "daylight") {
					DefaultCombo.SelectedIndex = i;
				}
			}
			i++;

			foreach (dynamic scene in Storage.defaultScenes) {
				DefaultCombo.Items.Add(new ComboItem(JsonParser.Read(Storage.defaultScenes, new string[] { scene.Key, "name" }), scene.Key, -2));
				if (Properties.Settings.Default.autoEffect == -2) {
					if (scene.Key == Properties.Settings.Default.autoName) {
						DefaultCombo.SelectedIndex = i;
					}
				}
				i++;
			}
			foreach (dynamic scene in Storage.sceneData) {
				DefaultCombo.Items.Add(new ComboItem(JsonParser.Read(Storage.sceneData, new string[] { scene.Key, "name" }), scene.Key, -1));
				if (Properties.Settings.Default.autoEffect == -1) {
					if (scene.Key == Properties.Settings.Default.autoName) {
						DefaultCombo.SelectedIndex = i;
					}
				}
				i++;
			}

			//Populate various app setting dropdowns
			int sunrise = SunriseEquation.JulianToTime(SunriseEquation.sunrise);
			int sunset = SunriseEquation.JulianToTime(SunriseEquation.sunset);
			string regional = String.Format(
				"Regional (sunrise at {0}:{1}, sunset at {2}:{3})",
					(sunrise / 60).ToString("D2"),
					(sunrise % 60).ToString("D2"),
					(sunset / 60).ToString("D2"),
					(sunset % 60).ToString("D2"));
			AppCombo2.Items.Add(regional);
			AppCombo2.Items.Add("Equinox (sunrise at 06:00, sunset at 18:00)");

			sunrise = SunriseEquation.JulianToTime(SunriseEquation.startup);
			sunset = SunriseEquation.JulianToTime(SunriseEquation.startup + 0.5);
			string startup = String.Format(
				"Startup (sunrise at {0}:{1}, sunset at {2}:{3})",
					(sunrise / 60).ToString("D2"),
					(sunrise % 60).ToString("D2"),
					(sunset / 60).ToString("D2"),
					(sunset % 60).ToString("D2"));
			AppCombo2.Items.Add(startup);

			AppCombo2.SelectedIndex = Albedo.Properties.Settings.Default.daylightSetting;

			if (Albedo.Properties.Settings.Default.ambientTransition == 80) {
				AppCombo1.SelectedIndex = 0;
			} else if (Albedo.Properties.Settings.Default.ambientTransition == 40) {
				AppCombo1.SelectedIndex = 1;
			} else if (Albedo.Properties.Settings.Default.ambientTransition == 10) {
				AppCombo1.SelectedIndex = 2;
			}

			slidersAllowed = true;
		}

		//Called when selecting a scene. Changes slider values.
		public void NewSceneInfo(string scene, bool init = false)
		{
			if (!init) {
				slidersAllowed = false;
			}

			int i = 1, light = 1;
			foreach (string lightLabel in Storage.groupData.lights) {
				if (i <= 5) {
					//Get sliders
					string briSliderName = String.Format("BriSlider{0}", i);
					string hueSliderName = String.Format("HueSlider{0}", i);
					string satSliderName = String.Format("SatSlider{0}", i);
					Slider sliderRef = (Slider)this.FindName(briSliderName);
					Slider sliderRef2 = (Slider)this.FindName(hueSliderName);
					Slider sliderRef3 = (Slider)this.FindName(satSliderName);

					//Make last defined light repeat infinitely.
					if (JsonParser.Read(Storage.sceneData, new string[] { scene, "state", i.ToString() }) != null) {
						light = i;
					}

					//Brightness
					sliderRef.Value = JsonParser.Read(Storage.sceneData, new string[] { scene, "state", light.ToString(), "bri" });

					//HSB mode
					if (JsonParser.Read(Storage.sceneData, new string[] { scene, "state", light.ToString(), "hue" }) != null) {
						sliderRef2.Value = JsonParser.Read(Storage.sceneData, new string[] { scene, "state", light.ToString(), "hue" });
						sliderRef3.Value = JsonParser.Read(Storage.sceneData, new string[] { scene, "state", light.ToString(), "sat" });
					}
					i++;
				}
			}

			if (!init) {
				slidersAllowed = true;
			}
		}

		//Toggles when switching between new scene and saved scenes.
		private void ButtonUpdate(string scene)
		{
			if (scene != "") {
				WriteButton.IsEnabled = true;
				WriteButton.Opacity = 1;
				NameButton.IsEnabled = true;
				NameButton.Opacity = 1;
				DeleteButton.IsEnabled = true;
				DeleteButton.Opacity = 1;
			} else {
				WriteButton.IsEnabled = false;
				WriteButton.Opacity = 0.65;
				NameButton.IsEnabled = false;
				NameButton.Opacity = 0.65;
				DeleteButton.IsEnabled = false;
				DeleteButton.Opacity = 0.65;
			}
		}

		//Same thing for groups.
		private void ButtonUpdate2(string group)
		{
			if (group != "0") {
				Write2Button.IsEnabled = true;
				Write2Button.Opacity = 1;
				Name2Button.IsEnabled = true;
				Name2Button.Opacity = 1;
				Delete2Button.IsEnabled = true;
				Delete2Button.Opacity = 1;
			} else {
				Write2Button.IsEnabled = false;
				Write2Button.Opacity = 0.65;
				Name2Button.IsEnabled = false;
				Name2Button.Opacity = 0.65;
				Delete2Button.IsEnabled = false;
				Delete2Button.Opacity = 0.65;
			}
		}

		private void EditorWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Properties.Settings.Default.customSceneJSON = JsonParser.Serialize(Storage.sceneData);
			Properties.Settings.Default.Save();
			Storage.InitializeData();
			WindowStorage.editorStorage = null;
		}

		private void BriSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (slidersAllowed) {
				PutEvents.ChangeBrightness(LightProperty.GetLightSource(((Slider)sender)), (int)((Slider)sender).Value);
			}
		}

		private void HueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (slidersAllowed) {
				PutEvents.ChangeHue(LightProperty.GetLightSource(((Slider)sender)), (int)((Slider)sender).Value);
			}
		}

		private void SatSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (slidersAllowed) {
				PutEvents.ChangeSaturation(LightProperty.GetLightSource(((Slider)sender)), (int)((Slider)sender).Value);
			}
		}

		private void SceneCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ComboItem temp = (ComboItem)SceneCombo.SelectedItem;
			if (temp != null) {
				if (slidersAllowed && temp.idStore != "") {
					PutEvents.ChangeScene(temp.idStore, false);
					NewSceneInfo(temp.idStore);
				}
				ButtonUpdate(temp.idStore);
			} else {
				this.SceneCombo.SelectedIndex = 0;
			}
		}

		private void GroupCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ComboItem temp = (ComboItem)GroupCombo.SelectedItem;
			if (temp != null) {
				if (slidersAllowed && temp.idStore != "") {
					Properties.Settings.Default.bridgeGroup = temp.idStore;
					Storage.ReinitializeData();
					
					//Change dropdown selections
					for (int n = 1; n <= 5; n++) {
						string comboName = String.Format("GroupLight{0}", n);
						ComboBox comboRef = (ComboBox)this.FindName(comboName);
						comboRef.SelectedIndex = 0;

						int i = 0;
						if (n != 1) { //Account for None option
							i++;
						}

						dynamic group = JsonParser.Read(Storage.latestData, new string[] { "groups", temp.idStore });
						if (temp.idStore != "0") {
							if (group.lights.Length >= n) {
								foreach (dynamic light in Storage.latestData.lights) {
									if (group.lights[n - 1] == light.Key) {
										comboRef.SelectedIndex = i;
									}
									i++;
								}
							}
						} else {
							if (n == 1) {
								comboRef.SelectedIndex = 0;
							} else if (comboRef.Items.Count > n) {
								comboRef.SelectedIndex = n;
							} else {
								comboRef.SelectedIndex = 0;
							}
						}
					}
				}
				ButtonUpdate2(temp.idStore);
			} else {
				this.GroupCombo.SelectedIndex = 0;
			}
		}

		private dynamic SceneCreation(string name, int color)
		{
			dynamic newScene = new ExpandoObject();
			newScene.state = new ExpandoObject();
			var state = (IDictionary<string, object>)newScene.state;
			newScene.name = name;
			newScene.tilecolor = color;

			int i = 1;
			foreach (string lightLabel in Storage.groupData.lights) {
				if (i <= 5) {
					state[i.ToString()] = new ExpandoObject();
					var lightstate = (IDictionary<string, object>)state[i.ToString()];

					string briSliderName = String.Format("BriSlider{0}", i);
					string hueSliderName = String.Format("HueSlider{0}", i);
					string satSliderName = String.Format("SatSlider{0}", i);
					Slider sliderRef = (Slider)this.FindName(briSliderName);
					Slider sliderRef2 = (Slider)this.FindName(hueSliderName);
					Slider sliderRef3 = (Slider)this.FindName(satSliderName);

					lightstate["bri"] = (int)sliderRef.Value;
					lightstate["hue"] = (int)sliderRef2.Value;
					lightstate["sat"] = (int)sliderRef3.Value;

					i++;
				}
			}

			return newScene;
		}

		private dynamic GroupCreation(string name)
		{
			dynamic newGroup = new ExpandoObject();
			newGroup.name = name;
			
			List<string> lights = new List<string>();

			int i = 0;
			for (int n = 1; n <= 5; n++) {
				string comboName = String.Format("GroupLight{0}", n);
				ComboBox comboRef = (ComboBox)this.FindName(comboName);
				ComboItem item = (ComboItem)comboRef.SelectedItem;

				if (item.idStore != "") {
					if (!lights.Contains(item.idStore)) { //The bridge rejects groups with duplicate lights
						lights.Add(item.idStore);
						i++;
					}
				}
			}

			string[] lightsArray = lights.ToArray();
			newGroup.lights = lightsArray;

			return newGroup;
		}


		//Scene buttons
		public static string sceneName = "";
		public static int sceneColor = 0;

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			sceneName = "";
			sceneColor = 0;
			SceneNameWindow nameWindow = new SceneNameWindow();
			nameWindow.Owner = this;
			nameWindow.ShowDialog();

			if (sceneName != "") {
				Random randomID = new Random();
				string sceneID = String.Format("scene_{0}", randomID.Next(10000000, 99999999).ToString());
				if (JsonParser.Read(Storage.sceneData, new string[] { sceneID, sceneName }) == null) {
					dynamic newScene = SceneCreation(sceneName, sceneColor);

					JsonParser.Create(Storage.sceneData, new string[] { sceneID }, newScene);

					//Add new scene to MainWindow
					for (int i = 1; i <= 8; i++) {
						string setting = String.Format("customSelected{0}", i.ToString());
						if ((string)Properties.Settings.Default[setting] == "") {
							Properties.Settings.Default[setting] = sceneID;
							break;
						}
					}

					//Append list of scenes
					this.SceneCombo.Items.Add(new ComboItem(JsonParser.Read(Storage.sceneData, new string[] { sceneID, "name" }), sceneID));
					this.SceneCombo.SelectedIndex = this.SceneCombo.Items.Count - 1;

				} else {
					MessageBox.Show("ID already in use. (This error should not occur. Try saving the scene again.)");
				}
			}
		}

		private void DeleteButton_Click(object sender, RoutedEventArgs e)
		{
			MessageBoxResult deleteMessage = System.Windows.MessageBox.Show("Are you sure you want to delete this scene?", "Scene Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
			ComboItem temp = (ComboItem)SceneCombo.SelectedItem;

			if (deleteMessage == MessageBoxResult.Yes) {
				JsonParser.Delete(Storage.sceneData, new string[] { temp.idStore });
				int tempIndex = this.SceneCombo.SelectedIndex;
				this.SceneCombo.SelectedIndex = 0;
				this.SceneCombo.Items.RemoveAt(tempIndex);
				
				//Delete scene from MainWindow
				for (int i = 1; i <= 8; i++) {
					string setting = String.Format("customSelected{0}", i.ToString());
					if ((string)Properties.Settings.Default[setting] == temp.idStore) {
						Properties.Settings.Default[setting] = "";
					}
				}
			}
		}

		private void NameButton_Click(object sender, RoutedEventArgs e)
		{
			sceneName = "";
			sceneColor = 0;
			SceneNameWindow nameWindow = new SceneNameWindow(this.SceneCombo.SelectedItem.ToString());
			nameWindow.Owner = this;
			nameWindow.ShowDialog();

			if (sceneName != "") {
				ComboItem tempItem = (ComboItem)SceneCombo.SelectedItem;
				JsonParser.Modify(Storage.sceneData, new string[] { tempItem.idStore, "name" }, sceneName);
				JsonParser.Modify(Storage.sceneData, new string[] { tempItem.idStore, "tilecolor" }, sceneColor);

				int tempIndex = this.SceneCombo.SelectedIndex;
				this.SceneCombo.Items[tempIndex] = new ComboItem(sceneName, tempItem.idStore);
				this.SceneCombo.SelectedIndex = tempIndex;
			}
		}

		private void WriteButton_Click(object sender, RoutedEventArgs e)
		{
			MessageBoxResult writeMessage = System.Windows.MessageBox.Show("Are you sure you want to overwrite this scene?", "Scene Editing", MessageBoxButton.YesNo, MessageBoxImage.Warning);

			if (writeMessage == MessageBoxResult.Yes) {
				ComboItem tempItem = (ComboItem)SceneCombo.SelectedItem;
				string writeName = JsonParser.Read(Storage.sceneData, new string[] { tempItem.idStore, "name" });
				int writeColor = JsonParser.Read(Storage.sceneData, new string[] { tempItem.idStore, "tilecolor" });
				string sceneID = tempItem.idStore;

				dynamic newScene = SceneCreation(writeName, writeColor);
				JsonParser.Modify(Storage.sceneData, new string[] { sceneID }, newScene);
			}
		}


		//Group buttons
		public static string groupName = "";

		async private void Save2Button_Click(object sender, RoutedEventArgs e) //Should be synchronous after refactoring
		{
			int count = 0;
			foreach (dynamic group in Storage.latestData.groups) {
				count++;
			}
			if (count >= 16) {
				MessageBox.Show("Group table is full. Only 16 groups can be stored at a time.", "Too Many Groups", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			groupName = "";
			GroupNameWindow nameWindow = new GroupNameWindow();
			nameWindow.Owner = this;
			nameWindow.ShowDialog();

			if (groupName != "") {
				foreach (dynamic group in Storage.latestData.groups) {
					if (group.Value.name == groupName) {
						MessageBox.Show("Group name already in use.", "Name Error", MessageBoxButton.OK, MessageBoxImage.Error);
						return;
					}
				}

				dynamic newGroup = GroupCreation(groupName);

				HttpClient client = new HttpClient();
				string address = AddressBuild.GroupsRoot();
				StringContent content = new StringContent(JsonParser.Serialize(newGroup));
				Task<HttpResponseMessage> postData = client.PostAsync(address, content);
				HttpResponseMessage response = await postData;

				string responseString = await response.Content.ReadAsStringAsync();
				if (responseString.Contains("success")) { //Extremely ugly hack. Use JsonParser after refactoring.
					responseString = responseString.Remove(0, responseString.IndexOf("groups/") + 7);
					responseString = responseString.Remove(responseString.IndexOf("\""), responseString.Length - responseString.IndexOf("\""));

					//Add group to latestData
					JsonParser.Create(Storage.latestData, new string[] { "groups", responseString }, newGroup);

					//Append list of groups
					this.GroupCombo.Items.Add(new ComboItem(groupName, responseString));
					this.GroupCombo.SelectedIndex = this.GroupCombo.Items.Count - 1;
				}
			}
		}

		async private void Delete2Button_Click(object sender, RoutedEventArgs e)
		{
			MessageBoxResult deleteMessage = System.Windows.MessageBox.Show("Are you sure you want to delete this group?", "Group Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
			ComboItem temp = (ComboItem)GroupCombo.SelectedItem;

			if (deleteMessage == MessageBoxResult.Yes) {
				HttpClient client = new HttpClient();
				string address = AddressBuild.GroupUriSpecify(temp.idStore);
				Task<HttpResponseMessage> postData = client.DeleteAsync(address);
				HttpResponseMessage response = await postData;

				string responseString = await response.Content.ReadAsStringAsync();
				if (responseString.Contains("success")) { //Extremely ugly hack. Use JsonParser after refactoring.
					JsonParser.Delete(Storage.latestData, new string[] { "groups", temp.idStore });
					int tempIndex = this.GroupCombo.SelectedIndex;
					this.GroupCombo.SelectedIndex = 0;
					this.GroupCombo.Items.RemoveAt(tempIndex);
				}
			}
		}

		async private void Name2Button_Click(object sender, RoutedEventArgs e)
		{
			ComboItem temp = (ComboItem)GroupCombo.SelectedItem;
			groupName = "";
			GroupNameWindow nameWindow = new GroupNameWindow();
			nameWindow.Owner = this;
			nameWindow.ShowDialog();

			if (groupName != "") {
				foreach (dynamic group in Storage.latestData.groups) {
					if (group.Value.name == groupName) {
						if (temp.nameStore != groupName) {
							MessageBox.Show("Group name already in use.", "Name Error", MessageBoxButton.OK, MessageBoxImage.Error);
						}
						return;
					}
				}

				dynamic groupData = new ExpandoObject();
				groupData.name = groupName;

				HttpClient client = new HttpClient();
				string address = AddressBuild.GroupUriSpecify(temp.idStore);
				StringContent content = new StringContent(JsonParser.Serialize(groupData));
				Task<HttpResponseMessage> postData = client.PutAsync(address, content);
				HttpResponseMessage response = await postData;

				string responseString = await response.Content.ReadAsStringAsync();
				if (responseString.Contains("success")) { //Extremely ugly hack. Use JsonParser after refactoring.
					//Edit group in latestData
					JsonParser.Modify(Storage.latestData, new string[] { "groups", temp.idStore, "name" }, groupName);

					//Edit list entry
					int tempIndex = this.GroupCombo.SelectedIndex;
					this.GroupCombo.Items[tempIndex] = new ComboItem(groupName, temp.idStore);
					this.GroupCombo.SelectedIndex = tempIndex;
				}
			}
		}

		async private void Write2Button_Click(object sender, RoutedEventArgs e)
		{
			MessageBoxResult writeMessage = System.Windows.MessageBox.Show("Are you sure you want to overwrite this group?", "Group Editing", MessageBoxButton.YesNo, MessageBoxImage.Warning);

			if (writeMessage == MessageBoxResult.Yes) {
				ComboItem temp = (ComboItem)GroupCombo.SelectedItem;
				dynamic groupData = GroupCreation(temp.nameStore);

				HttpClient client = new HttpClient();
				string address = AddressBuild.GroupUriSpecify(temp.idStore);
				StringContent content = new StringContent(JsonParser.Serialize(groupData));
				Task<HttpResponseMessage> postData = client.PutAsync(address, content);
				HttpResponseMessage response = await postData;

				string responseString = await response.Content.ReadAsStringAsync();
				if (responseString.Contains("success")) { //Extremely ugly hack. Use JsonParser after refactoring.
					//Edit group in latestData
					JsonParser.Modify(Storage.latestData, new string[] { "groups", temp.idStore, "lights" }, groupData.lights);
					JsonParser.Modify(Storage.groupData, new string[] { "lights" }, groupData.lights);
				}
			}
		}


		//App settings

		private void DefaultCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) //Startup scene / effect
		{
			if (slidersAllowed && DefaultCombo.SelectedIndex != -1) {
				ComboItem temp = (ComboItem)DefaultCombo.SelectedItem;
				Properties.Settings.Default.autoEffect = temp.idStore2;
				Properties.Settings.Default.autoName = temp.idStore;
			}
		}

		private void AppCombo1_SelectionChanged(object sender, SelectionChangedEventArgs e) //Transition time for Ambient mode
		{
			if (slidersAllowed && AppCombo1.SelectedIndex != -1) {
				if (AppCombo1.SelectedIndex == 0) {
					Albedo.Properties.Settings.Default.ambientTransition = 80;
				} else if (AppCombo1.SelectedIndex == 1) {
					Albedo.Properties.Settings.Default.ambientTransition = 40;
				} else if (AppCombo1.SelectedIndex == 2) {
					Albedo.Properties.Settings.Default.ambientTransition = 10;
				}
			}
		}

		private void AppCombo2_SelectionChanged(object sender, SelectionChangedEventArgs e) //Sunrise setting for Daylight mode
		{
			if (slidersAllowed && AppCombo2.SelectedIndex != -1) {
				Albedo.Properties.Settings.Default.daylightSetting = AppCombo2.SelectedIndex;
			}
		}

	}
}
