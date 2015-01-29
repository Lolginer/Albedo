using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Albedo.Core
{
	public class SceneEdit
	{
		public static bool IsSceneMatch(string scene)
		{
			int i = 1, light = 1, margin = 2;
			foreach (string lightLabel in Storage.groupData.lights) {
				if (JsonParser.Read(Storage.sceneData, new string[] { scene, "state", i.ToString() }) != null) {
					light = i;
				}

				dynamic sceneData = JsonParser.Read(Storage.sceneData, new string[] { scene, "state", light.ToString() });
				dynamic latestData = JsonParser.Read(Storage.latestData, new string[] { "lights", lightLabel, "state" });

				if (Math.Abs( sceneData.bri - latestData.bri ) > margin) {
					return false;
				}

				if (sceneData.hue != null) {
					if (Math.Abs(sceneData.hue - latestData.hue) > margin) {
						return false;
					}
					if (Math.Abs( sceneData.sat - latestData.sat ) > margin) {
						return false;
					}
				} else if (sceneData.ct != null) {
					if (Math.Abs( sceneData.ct - latestData.ct ) > margin) {
						return false;
					}
				}

				i++;
			}

			return true;
		}
	}
}
