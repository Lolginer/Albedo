using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Dynamic;
using System.IO;

namespace Albedo.Core
{
	public class Storage
	{
		public static dynamic latestData = new ExpandoObject();
		public static dynamic backupData;
		public static dynamic groupData = new ExpandoObject();
		public static dynamic groupBackupData;
		public static dynamic sceneData;
		public static List<string> addressArray = new List<string>(); //Used for bridge finding
		public static dynamic tempData; //Used for certain responses
		public static dynamic defaultScenes;
		public static dynamic sceneBackup;
		public static int accentBackup = 0;
		public static string currentScene = "";
		public static int activeTab = 0; //0 == lights, 1 == scenes, 2 == effects
		public static double opacityTarget = 1.0; //Controls the opacity of MainWindow.
		public static int[] fileVersion = new int[4];

		//Downloads latest bridge data, and stores it in latestData.
		async public static Task RefreshData(bool doSceneBackup = false, int retry = 0)
		{
			bool finalize = false;
			try {
				HttpClient client = new HttpClient();
				Task<HttpResponseMessage> getLightData = client.GetAsync(AddressBuild.LightsRoot());
				Task<HttpResponseMessage> getGroupsData = client.GetAsync(AddressBuild.GroupsRoot());
				Task<HttpResponseMessage> getGroupData = client.GetAsync(AddressBuild.GroupUri());
				HttpResponseMessage lightString = await getLightData;
				HttpResponseMessage groupsString = await getGroupsData;
				HttpResponseMessage groupString = await getGroupData;

				dynamic latestTemp = new ExpandoObject();
				dynamic groupTemp = new ExpandoObject();

				latestTemp.lights = JsonParser.Deserialize(await lightString.Content.ReadAsStringAsync());
				latestTemp.groups = JsonParser.Deserialize(await groupsString.Content.ReadAsStringAsync());
				groupTemp = JsonParser.Deserialize(await groupString.Content.ReadAsStringAsync());

				if (latestTemp.lights == null || groupTemp.lights == null) {
					throw new Exception();
				}

				latestData = latestTemp;
				groupData = groupTemp;
				backupData = latestData;
				groupBackupData = groupData;
				finalize = true;
				return;
			} catch (Exception) {
				if (retry < 2) {
					var result = RefreshData(doSceneBackup, retry + 1);
				} else {
					latestData = backupData;
					groupData = groupBackupData;
					finalize = true;
					
					return;
				}
			} finally {
				if (finalize) {
					if (doSceneBackup) {
						sceneBackup = latestData.lights;
					}

					Platform.FinalizeRefresh();
				}
			}
		}

		public static int InitializeData()
		{
			Storage.IntializeDefaultScenes();
			AddressBuild.InitializeVar(Platform.ReadSetting("bridgeIP"), Platform.ReadSetting("bridgeUserName"), Platform.ReadSetting("bridgeGroup"));
			activeTab = Platform.ReadSetting("lastActiveTab");
			sceneData = JsonParser.Deserialize(Platform.ReadSetting("customSceneJSON"));

			latestData = JsonParser.Deserialize(Platform.ReadSetting("backupMainJSON"));
			groupData = JsonParser.Deserialize(Platform.ReadSetting("backupGroupJSON"));
			backupData = latestData;
			groupBackupData = groupData;

			SunriseEquation.SetCoordinates();
			SunriseEquation.SunriseSunset();

			System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
			System.Diagnostics.FileVersionInfo versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
			fileVersion[0] = versionInfo.ProductMajorPart;
			fileVersion[1] = versionInfo.ProductMinorPart;
			fileVersion[2] = versionInfo.ProductBuildPart;
			fileVersion[3] = versionInfo.ProductPrivatePart;
			
			var result = RefreshData();

			var bridgeResult = FindNewBridgeIP();

			Effects.AutoEffect();
			var bridgeResult2 = StoreBridgeID();

			return 0;
		}

		public static int ReinitializeData() //Should be called when changing groups.
		{
			AddressBuild.InitializeVar(Platform.ReadSetting("bridgeIP"), Platform.ReadSetting("bridgeUserName"), Platform.ReadSetting("bridgeGroup"));
			var result = RefreshData();
			return 0;
		}

		//Attempt to store bridge ID
		async public static Task StoreBridgeID()
		{
			if (Platform.ReadSetting("bridgeIdentity") == "") {
				try {
					HttpClient client = new HttpClient();
					Task<HttpResponseMessage> getBridgeData = client.GetAsync("https://www.meethue.com/api/nupnp");
					HttpResponseMessage bridgeData = await getBridgeData;
					string bridgeString = await bridgeData.Content.ReadAsStringAsync();
					dynamic bridgeJson = JsonParser.Deserialize(bridgeString);
					foreach (dynamic bridgeInfo in bridgeJson) {
						string bridgeIdentity = bridgeInfo.id;
						string bridgeAddress = bridgeInfo.internalipaddress;
						bridgeIdentity = bridgeIdentity.ToLowerInvariant();
						if (bridgeAddress == Platform.ReadSetting("bridgeIP")) {
							Platform.WriteSetting("bridgeIdentity", bridgeIdentity);
							Platform.SaveSettings();
						}
						return;
					}
				} catch { }
			}
		}

		//Find the bridge again if the local IP changes
		async public static Task FindNewBridgeIP()
		{
			bool syncFailed = false;
			try {
				HttpClient client = new HttpClient();
				Task<HttpResponseMessage> getTestData = client.GetAsync(AddressBuild.LightsRoot());
				HttpResponseMessage testData = await getTestData;
				string testString = await testData.Content.ReadAsStringAsync();
				dynamic testJson = JsonParser.Deserialize(testString);
			} catch { syncFailed = true; }

			if (!syncFailed) { return; }

			try {
				HttpClient client = new HttpClient();
				Task<HttpResponseMessage> getBridgeData = client.GetAsync("https://www.meethue.com/api/nupnp");
				HttpResponseMessage bridgeData = await getBridgeData;
				string bridgeString = await bridgeData.Content.ReadAsStringAsync();
				dynamic bridgeJson = JsonParser.Deserialize(bridgeString);
				foreach (dynamic bridgeInfo in bridgeJson) {
					string bridgeIdentity = bridgeInfo.id;
					string bridgeAddress = bridgeInfo.internalipaddress;
					bridgeIdentity = bridgeIdentity.ToLowerInvariant();
					if (bridgeIdentity == Platform.ReadSetting("bridgeIdentity")) {
						if (bridgeAddress != Platform.ReadSetting("bridgeIP")) {
							Platform.WriteSetting("bridgeIP", bridgeAddress);
							Platform.SaveSettings();
						}
					}
				}
			} catch { }
		}

		public static void IntializeDefaultScenes()
		{
			var assembly = System.Reflection.Assembly.GetExecutingAssembly();
			Stream stream = assembly.GetManifestResourceStream("Albedo.Resources.defaultScenesJSON.txt");
			StreamReader reader = new StreamReader(stream);
			string scenesString = reader.ReadToEnd();
			defaultScenes = JsonParser.Deserialize(scenesString);
		}

		public static dynamic tileColors(int value)
		{
			dynamic colorStore = new ExpandoObject();
			switch (value) {
				case -1:
					//Energize
					colorStore.background = "#FF3BC3E6";
					colorStore.border = "#FF30A6C5";
					colorStore.blackText = false;
					break;
				case -2:
					//Concentrate
					colorStore.background = "#FF7ADBEA";
					colorStore.border = "#FF38B8D4";
					colorStore.blackText = true;
					break;
				case -3:
					//Reading
					colorStore.background = "#FFF0DC5C";
					colorStore.border = "#FFD1BF4B";
					colorStore.blackText = true;
					break;
				case -4:
					//Relax
					colorStore.background = "#FFEAB03C";
					colorStore.border = "#FFB98C2F";
					colorStore.blackText = true;
					break;
				case -5:
					//Nighttime
					colorStore.background = "#FF8866AA";
					colorStore.border = "#FF554495";
					colorStore.blackText = false;
					break;
				case 1:
					//Red
					colorStore.background = "#FFE63B3B";
					colorStore.border = "#FFC50F0F";
					colorStore.blackText = false;
					break;
				case 2:
					//Green
					colorStore.background = "#FF7ED357";
					colorStore.border = "#FF4CA442";
					colorStore.blackText = true;
					break;
				case 3:
					//Blue
					colorStore.background = "#FF6473D1";
					colorStore.border = "#FF3444AA";
					colorStore.blackText = false;
					break;
				case 4:
					//Cyan
					colorStore.background = "#FF2BC7C0";
					colorStore.border = "#FF349FAA";
					colorStore.blackText = false;
					break;
				case 5:
					//Purple
					colorStore.background = "#FFAE60E4";
					colorStore.border = "#FF924DAC";
					colorStore.blackText = false;
					break;
				case 6:
					//Pink
					colorStore.background = "#FFFB88E1";
					colorStore.border = "#FFB968B2";
					colorStore.blackText = false;
					break;
				case 7:
					//Orange
					colorStore.background = "#FFFF850A";
					colorStore.border = "#FFD4603D";
					colorStore.blackText = false;
					break;
				case 8:
					//Silver
					colorStore.background = "#FFAFAFAF";
					colorStore.border = "#FF7F7FBB";
					colorStore.blackText = true;
					break;
				case 9:
					//Black
					colorStore.background = "#FF515151";
					colorStore.border = "#FF343434";
					colorStore.blackText = false;
					break;
				default:
					//Default magenta
					colorStore.background = "#FFE63B81";
					colorStore.border = "#FFAA325E";
					colorStore.blackText = false;
					break;
			}


			return colorStore;
		}
	}
}
