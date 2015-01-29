using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Timers;
using System.Dynamic;

namespace Albedo.Core
{
	public class PutEvents
	{
		public static bool timePutAllowed = true;

		//Controls timePutAllowed
		private static void PutTimer()
		{
			System.Timers.Timer eventTimer = new System.Timers.Timer(100);
			eventTimer.AutoReset = false;
			eventTimer.Elapsed += OnTimedEvent;
			eventTimer.Enabled = true;
		}

		static int sendCount = 0; //Sending last data multiple times helps with race conditions
		async private static void OnTimedEvent(Object source, ElapsedEventArgs e)
		{
			timePutAllowed = true;
			
			if (sendCount > 2) {
				bufferUris.Clear();
				bufferStrings.Clear();
				sendCount = 0;
			} else if (bufferUris.Count > 0) {
				await TimePut(null, "");
			}
		}

		static List<Uri> bufferUris = new List<Uri>();
		static List<string> bufferStrings = new List<string>();

		//Sends buffered TimePut data in 100ms intervals
		async private static Task TimePut(Uri dataUri, string dataString)
		{
			if (dataString != "") {
				if (bufferUris.Contains(dataUri)) {
					bufferStrings[bufferUris.IndexOf(dataUri)] = dataString;
				} else {
					bufferUris.Add(dataUri);
					bufferStrings.Add(dataString);
				}
				sendCount = 0;
			}

			if (timePutAllowed) {
				//PUT the data
				timePutAllowed = false;
				Effects.EffectsOff();
				try {
					for (int i = 0; i < bufferUris.Count(); i++) {
						PutData(bufferUris[i], bufferStrings[i]);
						await Task.Delay(100 / bufferUris.Count()); //Improves UI performance slightly
					}
					return;
				} catch (Exception) { return; } finally {
					sendCount++;
					PutTimer();
				}
			}
			return;
		}

		//Sends data immediately
		static bool waitForWait = false; //Prevents issues with restoring scenes immediately after disabling an effect
		async private static void PutData(Uri dataUri, string dataString, bool forceDelay = false)
		{
			try {
				if (Effects.effectsOn || forceDelay) {
					waitForWait = true;
					Effects.EffectsOff();
					await Task.Delay(750);
					waitForWait = false;
				}
				if (waitForWait) {
					await Task.Delay(750);
				}
				HttpClient putClient = new HttpClient();
				var result = await putClient.PutAsync(dataUri, new StringContent(dataString, Encoding.UTF8));
				return;
			} catch (Exception) { return; }
		}

		public static int CustomPut(string light, string dataString, bool forceDelay = false)
		{
			Uri dataUri = new Uri(AddressBuild.LightState(light));
			PutData(dataUri, dataString, forceDelay);
			return 0;
		}

		public static int ToggleLight(string light, bool isOn)
		{
			dynamic jsonToggle = new ExpandoObject();
			if (isOn == true) {
				jsonToggle.on = true;
			} else {
				jsonToggle.on = false;
			}

			Uri dataUri = new Uri(AddressBuild.LightState(light));
			JsonParser.Modify(Storage.latestData, new string[] { "lights", light, "state", "on" }, isOn);
			PutData(dataUri, JsonParser.Serialize(jsonToggle));
			return 0;
		}

		public static void ChangeBrightness(string light, int brightness)
		{
			ChangeValue(light, brightness, "bri");
		}

		public static void ChangeHue(string light, int hue)
		{
			ChangeValue(light, hue, "hue");
		}

		public static void ChangeSaturation(string light, int saturation)
		{
			ChangeValue(light, saturation, "sat");
		}

		private static void ChangeValue(string light, int value, string valueName)
		{
			dynamic jsonValue = new ExpandoObject();
			var jsonRef = (IDictionary<string, object>)jsonValue;
			jsonRef[valueName] = value;

			Uri dataUri = new Uri(AddressBuild.LightState(light));
			TimePut(dataUri, JsonParser.Serialize(jsonValue));
			JsonParser.Modify(Storage.latestData, new string[] { "lights", light, "state", valueName }, (int)value);
		}

		public static int ChangeScene(string scene, bool hardCoded)
		{
			//Turn on the lights if any of them are disabled
			foreach (string light in Storage.groupData.lights) {
				if (JsonParser.Read(Storage.latestData, new string[] { "lights", light, "state", "on" }) == false) {
					ToggleLight(light, true);
				}
			}

			dynamic tileColor;
			int tileColorValue;
			if (hardCoded) {
				foreach (string light in Storage.groupData.lights) {
					Uri dataUri = new Uri(AddressBuild.LightState(light));
					PutData(dataUri, JsonParser.Serialize(JsonParser.Read(Storage.defaultScenes, new string[] {scene, "state", "1"})));
				}
				tileColorValue = (int)JsonParser.Read(Storage.defaultScenes, new string[] {scene, "tilecolor"});
				tileColor = Storage.tileColors(tileColorValue);
			} else {
				if (JsonParser.Read(Storage.sceneData, new string[] { scene }) != null) {
					int i = 1;
					int limit = 0;

					foreach (dynamic light in JsonParser.Read(Storage.sceneData, new string[] { scene, "state" })) {
						limit++;
					}

					foreach (string light in Storage.groupData.lights) {
						Uri dataUri = new Uri(AddressBuild.LightState(light));
						PutData(dataUri, JsonParser.Serialize(JsonParser.Read(Storage.sceneData, new string[] { scene, "state", i.ToString() })));
						if (i < limit) { i++; }
					}

					tileColorValue = (int)JsonParser.Read(Storage.sceneData, new string[] { scene, "tilecolor" });
					tileColor = Storage.tileColors(tileColorValue);


				} else {
					//Error handling for missing scene data 
					tileColorValue = 0;
					tileColor = Storage.tileColors(tileColorValue);

				}
			}

			//Change accent to match tile background
			ChangeAccent(tileColorValue);

			//Store scene name
			Storage.currentScene = scene;

			return 0;
		}

		public static void ChangeAccent(int tileColorValue, bool backupValue = true)
		{
			Platform.ChangeAccent(tileColorValue, backupValue);
			return;
		}
	}
}
