using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Drawing;
using System.Dynamic;
using Albedo.Core;
using SharpDX;

namespace Albedo
{
	class Platform
	{
		public static dynamic ReadSetting(string settingName)
		{
			return Properties.Settings.Default[settingName];
		}

		public static void WriteSetting(string settingName, dynamic settingData)
		{
			Properties.Settings.Default[settingName] = settingData;
			return;
		}

		public static void UpgradeSettings()
		{
			Properties.Settings.Default.Upgrade();
			return;
		}

		public static void SaveSettings()
		{
			Properties.Settings.Default.Save();
			return;
		}

		public static void ShowDialog(string content) 
		{
			System.Windows.MessageBox.Show(content);
		}

		public static void ChangeAccent(int tileColorValue, bool backupValue)
		{
			dynamic tileColorStore = Storage.tileColors(tileColorValue);
			SolidColorBrush newHighlight, newAccent, newAccent2, newAccent3, newAccent4;
			newHighlight = new BrushConverter().ConvertFromString(tileColorStore.border);
			newAccent = new BrushConverter().ConvertFromString(tileColorStore.border);
			newAccent2 = new BrushConverter().ConvertFromString(tileColorStore.border);
			newAccent3 = new BrushConverter().ConvertFromString(tileColorStore.border);
			newAccent4 = new BrushConverter().ConvertFromString(tileColorStore.border);

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

			if (backupValue) {
				Storage.accentBackup = tileColorValue;
			}
			return;
		}

		public static void FinalizeRefresh()
		{
			DummyWindow.ToolTipUpdate();
			if (WindowStorage.newWindow != null) {
				WindowStorage.newWindow.SetInfo();
			}

			Properties.Settings.Default.backupMainJSON = JsonParser.Serialize(Storage.latestData);
			Properties.Settings.Default.backupGroupJSON = JsonParser.Serialize(Storage.groupData);
			Properties.Settings.Default.Save();

			return;
		}

		public static void NotifyCheck(string toToggle, bool isOn)
		{
			WindowStorage.dummyStorage.AmbientItem.IsChecked = false;
			WindowStorage.dummyStorage.DaylightItem.IsChecked = false;
			switch (toToggle) {
				case "ambient":
					WindowStorage.dummyStorage.AmbientItem.IsChecked = isOn;
					break;
				case "daylight":
					WindowStorage.dummyStorage.DaylightItem.IsChecked = isOn;
					break;
			}
			return;
		}

		private static int AmbientD3D()
		{
			//Uses SharpDX for faster sampling on Windows 8 and up. Might fix W10 issues.
			//(Not done, obviously)
			return 0;
		}

		public static dynamic AmbientSampling()
		{
			try { //Prevents crash when user is on the secure desktop
				int numberOfSamples = 6;
				Bitmap[] screenSample = new Bitmap[numberOfSamples];
				System.Drawing.Color[] screenPixels = new System.Drawing.Color[numberOfSamples];

				for (int i = 0; i < screenSample.Length; i++) {
					screenSample[i] = new Bitmap(1, 1);
					Graphics screen = Graphics.FromImage(screenSample[i]);
					int width = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
					int height = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
					screen.CopyFromScreen(128 + (((width - 128) / (screenSample.Length - 1)) * i), 128 + (((height - 128) / (screenSample.Length - 1)) * i), 0, 0, screenSample[i].Size);
					screenPixels[i] = screenSample[i].GetPixel(0, 0);
				}

				//HSB values for colours do not directly correspond to the way hue handles light.
				//For instance, pink would show up as a vibrant red, while actual red would look extremely dark.
				float brightnessAverage = (screenPixels.Average(x => x.GetBrightness()) + screenPixels.Average(x => x.GetSaturation()) / 3) * 255;
				if (brightnessAverage > 255) {
					brightnessAverage = 255;
				}

				float saturationAverage = 2 - 2 * screenPixels.Average(x => x.GetBrightness());
				if (saturationAverage > 1) {
					saturationAverage = 1;
				}
				saturationAverage = screenPixels.Average(x => x.GetSaturation()) * saturationAverage * 255;

				//Desaturation
				saturationAverage *= 0.5F;

				//Saturation in darkness
				float contrastBoost = (127 - (brightnessAverage * 10));
				if (contrastBoost < 0) {
					contrastBoost = 0;
				}
				saturationAverage += contrastBoost;
				if (saturationAverage > 255) {
					saturationAverage = 255;
				}

				//Hue is an angle, and can't be averaged directly.
				double hueSin = 0;
				double hueCos = 0;
				foreach (System.Drawing.Color pixel in screenPixels) {
					hueSin += Math.Sin(pixel.GetHue() * Math.PI / 180);
					hueCos += Math.Cos(pixel.GetHue() * Math.PI / 180);
				}
				double hueAverage = Math.Atan2(hueSin, hueCos) * (180 / Math.PI);
				if (hueAverage < 0) {
					hueAverage = 360 + hueAverage;
				}
				hueAverage *= 182;

				dynamic ambient = new ExpandoObject();
				ambient.bri = (int)brightnessAverage;
				ambient.hue = (int)hueAverage;
				ambient.sat = (int)saturationAverage;
				return ambient;
			} catch (Exception) {
				dynamic ambient = new ExpandoObject();
				ambient.bri = 128;
				ambient.hue = 1;
				ambient.sat = 1;
				return ambient;
			}
		}
	}
}
