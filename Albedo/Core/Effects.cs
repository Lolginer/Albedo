using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Timers;
using System.Dynamic;
using System.Drawing;
using System.Threading.Tasks;

namespace Albedo.Core
{
	public class Effects
	{
		static System.Timers.Timer effectTimer = new System.Timers.Timer(150);
		public static bool effectsOn = false;
		static dynamic bufferData = new ExpandoObject(); //Stores buffered EffectPut data
		public static string effectMode = "";
		static Uri[] lightArray; //Used to alternate between lights to update
		static int lastLight;
		static int chosenSunrise = 480;
		static int chosenSunset = 1080;

		//Controls timePutAllowed
		private static void EffectTimerOn()
		{
			bufferData.bri = 0;
			bufferData.hue = 0;
			bufferData.sat = 0;
			
			int arrayLength = Storage.groupData.lights.Length;
			lightArray = new Uri[arrayLength];
			int i = 0;
			foreach (string light in Storage.groupData.lights) {
				lightArray[i] = new Uri(AddressBuild.LightState(light));
				i++;
			}

			lastLight = 0;
			effectTimer.Elapsed += OnTimedEvent;
			effectTimer.Enabled = true;
			ToggleEffectsOn();
		}

		async private static void ToggleEffectsOn()
		{
			//Backup current scene data
			if (!effectsOn) {
				await Storage.RefreshData(true);
				await Task.Delay(500);
			}

			effectsOn = true;
			return;
		}

		async private static void OnTimedEvent(Object source, ElapsedEventArgs e)
		{
			switch (effectMode) {
				case "test":
					TestPut();
					break;
				case "ambient":
					AmbientPut();
					break;
				case "daylight":
					DaylightPut();
					break;
			}

			try {
				HttpClient client = new HttpClient();
				StringContent content = new StringContent(JsonParser.Serialize(bufferData), Encoding.UTF8);
				if (effectsOn) {
					await client.PutAsync(lightArray[lastLight], content);
				}
			} catch (Exception) { return; } finally {
				lastLight++;
				if (lastLight >= lightArray.Length) {
					lastLight = 0;
				}
			}
		}

		private static void AmbientPut()
		{
			dynamic ambient = Platform.AmbientSampling();
			bufferData.bri = ambient.bri;
			bufferData.hue = ambient.hue;
			bufferData.sat = ambient.sat;
			return;
		}

		private static void DaylightPut()
		{
			int[] brightness = { 254, 200 }; //First value is for daytime, second for nighttime.
			int[] temperature = { 200, 370 };
			dynamic daylight = new ExpandoObject();

			int currentTime = DateTime.Now.Hour * 60 + DateTime.Now.Minute;
			double sunlightPercentage =
				(Maths.Clamp(currentTime - chosenSunrise, 0, 60)
				- Maths.Clamp(currentTime - (chosenSunset - 60), 0, 60))
				/ 60;

			if (chosenSunrise > chosenSunset) {
				sunlightPercentage += 1;
			}

			daylight.bri = Maths.Lerp(brightness[1], brightness[0], sunlightPercentage);
			daylight.ct = Maths.Lerp(temperature[1], temperature[0], sunlightPercentage);

			bufferData.bri = daylight.bri;
			bufferData.ct = daylight.ct;

			return;
		}

		private static void TestPut()
		{
			bufferData.bri = 10;
			bufferData.hue = (bufferData.hue + 1638) % 65535;
			bufferData.sat = 255;
		}

		public static void ModeOn(string modeName)
		{
			//Turn on the lights if any of them are disabled
			foreach (string light in Storage.groupData.lights) {
				if (JsonParser.Read(Storage.latestData, new string[] { "lights", light, "state", "on" }) == false) {
					PutEvents.ToggleLight(light, true);
				}
			}

			effectMode = modeName;
			Platform.NotifyCheck(modeName, true);
			
			switch (effectMode) {
				case "test":
				case "ambient":
					effectTimer.Interval = 150;
					bufferData.transitiontime = Platform.ReadSetting("ambientTransition");
					break;
				case "daylight":
					effectTimer.Interval = 1500;
					bufferData.transitiontime = 40;
					if (Platform.ReadSetting("daylightSetting") == 0) { //Regional
						chosenSunrise = SunriseEquation.JulianToTime(SunriseEquation.sunrise);
						chosenSunset = SunriseEquation.JulianToTime(SunriseEquation.sunset);
					} else if (Platform.ReadSetting("daylightSetting") == 1) { //Equinox
						chosenSunrise = 480; //06:00
						chosenSunset = 1080; //18:00
					} else if (Platform.ReadSetting("daylightSetting") == 2) { //Startup
						chosenSunrise = SunriseEquation.JulianToTime(SunriseEquation.startup);
						chosenSunset = SunriseEquation.JulianToTime(SunriseEquation.startup + 0.5);
					}
					break;
			}

			if (!effectsOn) {
				EffectTimerOn();
			}
		}

		public static void EffectsOff()
		{
			if (effectTimer.Enabled) {
				effectMode = "";
				effectTimer.Elapsed -= OnTimedEvent;
				effectTimer.Enabled = false;
				foreach (string light in Storage.groupData.lights) { //Restore last scene data from backup

					JsonParser.Create(Storage.latestData, new string[] { "lights", light, "state" }, JsonParser.Read(Storage.sceneBackup, new string[] { light, "state" }));
					PutEvents.CustomPut(light, JsonParser.Serialize(JsonParser.Read(Storage.sceneBackup, new string[] { light, "state" })), true);
				}
				effectsOn = false;
				Platform.NotifyCheck("", false);
			}
		}

		static bool firstAuto = true; //Prevents AutoEffect from running more than once.

		async public static void AutoEffect()
		{
			if (firstAuto) {
				await Task.Delay(1000); //Gives RefreshData some time to complete successfully.
				if (Ready()) {
					if (Platform.ReadSetting("autoEffect") < 0) { //Scenes
						bool hardCoded = false;
						if (Platform.ReadSetting("autoEffect") == -2) {
							hardCoded = true;
						}
						PutEvents.ChangeScene(Platform.ReadSetting("autoName"), hardCoded);
					} else if (Platform.ReadSetting("autoEffect") > 0) { //Effects
						ModeOn(Platform.ReadSetting("autoName"));
					}
				}
				firstAuto = false;
			}
		}

		private static bool Ready()
		{
			if (Platform.ReadSetting("bridgeIP") == "0.0.0.0") {
				return false;
			} else {
				return true;
			}
		}
	}
}
