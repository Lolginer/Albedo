//I realize there are existing libraries for JSON parsing, but I decided to write my own class for two reasons.
//1: I might create an Android port of Albedo using Xamarin, and Json.NET's size would push it over the Starter limits.
//2: It seemed like a good idea at the time.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;
using System.Text.RegularExpressions;

namespace Albedo.Core
{
	public class JsonParser
	{
		public static string Serialize(dynamic dynamicData, int nestLevel = 0)
		{

			StringBuilder stringData = new StringBuilder("{");
			stringData.AppendLine();

			foreach (dynamic element in dynamicData) {
				if (element.GetType() == typeof(System.Collections.Generic.KeyValuePair<string, object>)) {
					if (element.Value == null) { //Do not serialize null values
						continue;
					}
				}
				
				for (int i = 0; i <= nestLevel; i++) {
					stringData.Append("\t");
				}
				
				if (element.GetType() == typeof(System.Collections.Generic.KeyValuePair<string, object>)) {
					stringData.AppendFormat("\"{0}\": ", element.Key);
				} else {
					throw new Exception(String.Format("Unknown object type {0}",element.GetType().ToString()));
				}

				if (element.Value.GetType() == typeof(System.Dynamic.ExpandoObject)) {
					stringData.Append(Serialize(element.Value, nestLevel + 1));
				} else if (element.Value.GetType() == typeof(System.Object[]) || element.Value.GetType() == typeof(System.String[])) {
					stringData.Append("[");
					foreach (dynamic arrayObject in element.Value) {
						if (arrayObject.GetType() == typeof(string)) {
							stringData.AppendFormat("\"{0}\"", arrayObject);
						} else if (arrayObject.GetType() == typeof(bool)) {
							stringData.Append(arrayObject.ToString().ToLower());
						} else {
							stringData.Append(arrayObject.ToString(System.Globalization.CultureInfo.InvariantCulture));
						}
						stringData.Append(", ");
					}
					stringData.Remove(stringData.Length - 2, 2);
					stringData.Append("]");
				} else if (element.Value.GetType() == typeof(string)) {
					stringData.AppendFormat("\"{0}\"", element.Value);
				} else if (element.Value.GetType() == typeof(bool)) {
					stringData.Append(element.Value.ToString().ToLower());
				} else {
					stringData.Append(element.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
				}

				stringData.AppendLine(",");
			}

			int comma = stringData.ToString().LastIndexOf(",");
			if (comma == -1) { comma = stringData.Length; }
			stringData.Remove(comma, stringData.Length - comma);
			stringData.AppendLine();
			for (int i = 0; i < nestLevel; i++) {
				stringData.Append("\t");
			}
			stringData.Append("}");

			return stringData.ToString();
		}

		public static dynamic Deserialize(string stringData)
		{
			string subString = stringData;
			dynamic dynamicData = new ExpandoObject();
			var dataRef = (IDictionary<string, object>)dynamicData;
			subString = Regex.Replace(subString, "^(\\s)+", ""); //Remove leading whitespace

			//Check if it's an array
			if (subString.StartsWith("[")) {
				subString = Regex.Replace(subString, "(\\s)+$", ""); //Remove trailing whitespace
				subString = ReplaceComma(subString);
				subString = subString.Substring(1, subString.Length - 2); //Remove brackets

				List<dynamic> dynamicList = new List<dynamic>();

				string[] subStringArray = subString.Split('α');

				foreach (string subSubString in subStringArray) {
					dynamicList.Add(Deserialize(subSubString));
				}

				dynamic[] dynamicArray = dynamicList.ToArray();
				return dynamicArray;
			}

			subString = subString.Remove(0, 1); //Remove first curly brace
			subString = Regex.Replace(subString, "(\\s)+$", ""); //Remove trailing whitespace
			if (subString.Substring(subString.Length - 1, 1) == "}") {
				subString = subString.Remove(subString.Length - 1, 1);
			} else {
				throw new Exception("Malformed JSON Error 1");
			}

			while (subString.Contains("\"")) {
				try {
					//Parse name
					subString = subString.Remove(0, subString.IndexOf("\"") + 1);
					string elementName = subString.Substring(0, subString.IndexOf("\""));

					//Parse object
					dynamic elementObject;
					int deleteIndex;
					subString = subString.Remove(0, subString.IndexOf(":") + 1);
					subString = Regex.Replace(subString, "^(\\s)+", ""); //Remove leading whitespace
					if (subString.IndexOf("{") == 0) { //Object parsing
						subString = subString.Remove(0, subString.IndexOf("{"));

						int nestedEnd = 0;
						for (int i = 1; i > 0; ) {
							nestedEnd = subString.IndexOfAny(new char[] { '{', '}' }, nestedEnd + 1);
							if (subString.Substring(nestedEnd, 1) == "{") {
								i++;
							} else {
								i--;
							}
						}
						nestedEnd++;

						elementObject = Deserialize(subString.Substring(0, nestedEnd));
						deleteIndex = subString.IndexOf(",", nestedEnd);
					} else if (subString.IndexOf("[") == 0) { //Array parsing (does not handle objects or nesting)
						subString = subString.Remove(0, subString.IndexOf("[") + 1);
						string arraySubStr = subString.Substring(0, subString.IndexOf("]"));
						int arrayLength = arraySubStr.Count(x => x == ',') + 1;

						elementObject = new dynamic[arrayLength];
						int arraySubIndex = 0;
						for (int i = 0; i < elementObject.Length; i++) {
							arraySubIndex = subString.IndexOfAny(new char[] { ',', ']' });
							elementObject[i] = DeserializeValue(subString.Substring(0, arraySubIndex));
							subString = subString.Remove(0, arraySubIndex + 1);
						}

						deleteIndex = subString.IndexOf(",");
					} else { //All other values
						int subIndex = subString.IndexOf(",");
						if (subIndex == -1) {
							subIndex = subString.Length;
						} else if (subString.IndexOf("\"") == 0) { //(In case of strings containing commas)
							subIndex = subString.IndexOf(",", subString.IndexOf("\"", 1));
						}
						elementObject = DeserializeValue(subString.Substring(0, subIndex));

						deleteIndex = subString.IndexOf(",");
						if (subString.IndexOf("\"") == 0) { //(In case of strings containing commas)
							deleteIndex = subString.IndexOf(",", subString.IndexOf("\"", 1));
						}
					}

					//Create pair
					dataRef[elementName] = elementObject;

					//Erase parsed data
					if (deleteIndex == -1) {
						deleteIndex = subString.Length - 1;
					}
					subString = subString.Remove(0, deleteIndex + 1);
				} catch {
					string catchOutput = String.Format("Malformed JSON Error 2; string data: {0}", subString);
					throw new Exception(catchOutput);
				}
			}

			return dynamicData;
		}

		public static dynamic DeserializeValue(string subString)
		{
			dynamic elementObject;

			if (subString.IndexOf("\"") != -1) { //String parsing
				subString = subString.Remove(0, subString.IndexOf("\"") + 1);
				elementObject = (string)subString.Substring(0, subString.IndexOf("\""));
			} else if (subString.Contains("true")) { //Bool parsing
				elementObject = true;
			} else if (subString.Contains("false")) {
				elementObject = false;
			} else if (subString.Contains("null")) { //Null parsing
				elementObject = null;
			} else { //Int / float parsing
				int subIndex = subString.IndexOfAny(new char[] { ',', ']', '}' });
				if (subIndex == -1) {
					subIndex = subString.Length;
				}

				if (subString.IndexOf(".") != -1) {
					elementObject = Convert.ToDouble(subString.Substring(0, subIndex), System.Globalization.CultureInfo.InvariantCulture);
				} else {
					elementObject = Convert.ToInt32(subString.Substring(0, subIndex));
				}
			}

			return elementObject;
		}

		public static int IndexOfComma(string line)
		{
			StringBuilder lineBuild = new StringBuilder(line);
			int parDepth = 0;

			for (int i = 0; i < lineBuild.Length; i++) {
				if (lineBuild[i] == '}') { parDepth--; }
				if (parDepth > 0) { lineBuild[i] = 'Ä'; }
				if (lineBuild[i] == '{') { parDepth++; }
			}

			string lineClean = lineBuild.ToString();

			return lineClean.IndexOf(',');
		}

		public static string ReplaceComma(string line)
		{
			string lineClean = line;

			while (IndexOfComma(lineClean) >= 0) {
				int index = IndexOfComma(lineClean);
				StringBuilder build = new StringBuilder(lineClean);
				build[index] = 'α';
				lineClean = build.ToString();
			}

			return lineClean;
		}

		public static void JsonTest()
		{
			var assembly = System.Reflection.Assembly.GetExecutingAssembly();
			System.IO.Stream stream = assembly.GetManifestResourceStream("Albedo.Resources.testJSON.txt");
			System.IO.StreamReader reader = new System.IO.StreamReader(stream);
			string scenesString = reader.ReadToEnd();
			
			dynamic testData = Deserialize(scenesString);
			Platform.ShowDialog(Serialize(testData));
		}

        //Reads data deserialized by this class.
		public static dynamic Read(dynamic data, string[] path)
		{
			bool exception = false;
			bool notNullable = false;
			if (path[path.Length - 1] == "hue" ||
				path[path.Length - 1] == "sat") {
				notNullable = true;
			}

			foreach (string subPath in path) {
				var dataRef = (IDictionary<string, object>)data;
				try {
					data = dataRef[subPath];
				} catch {
					exception = true;
					break;
				}
			}

			if (data == null && notNullable) {
				exception = true;
			}

			if (exception) {
				if (notNullable) {
					double zero = 0;
					return zero;
				} else {
					return null;
				}
			}

			return data;
		}

		//Checks if the key exists and isn't null.
		public static dynamic Exists(dynamic data, string[] path)
		{
			foreach (string subPath in path) {
				var dataRef = (IDictionary<string, object>)data;
				try {
					data = dataRef[subPath];
				} catch {
					return false;
				}
			}

			if (data == null) {
				return false;
			}

			return true;
		}

        //Adds new data. Cannot be used to overwrite existing values.
		public static void Create(dynamic data, string[] path, dynamic value)
		{
			int i = 1;
			foreach (string subPath in path) {
				var dataRef = (IDictionary<string, object>)data;

				dynamic pathValue;
				if (!dataRef.TryGetValue(subPath, out pathValue)) {
					if (i < path.Length) {
						dataRef[subPath] = new ExpandoObject();
					} else {
						dataRef[subPath] = value;
						return;
					}
				}
				data = dataRef[subPath];
				i++;
			}

			data = value;
			return;
		}

        //Deletes data.
		public static void Delete(dynamic data, string[] path)
		{
			string lastSubPath = "missingpath";
			dynamic lastData = "missingdata";
			foreach (string subPath in path) {
				var dataRef = (IDictionary<string, object>)data;
				try {
					lastData = data;
					data = dataRef[subPath];
					lastSubPath = subPath;
				} catch {
					return;
				}
			}

			var lastDataRef = (IDictionary<string, object>)lastData;
			lastDataRef[lastSubPath] = null;
			return;
		}

        //Overwrites existing values. Can be used to create new data if there's no nesting involved.
		public static void Modify(dynamic data, string[] path, dynamic content)
		{
			string lastSubPath = "missingpath";
			dynamic lastData = "missingdata";
			foreach (string subPath in path) {
				var dataRef = (IDictionary<string, object>)data;
				try {
					lastData = data;
					data = dataRef[subPath];
					lastSubPath = subPath;
				} catch {
					return;
				}
			}

			var lastDataRef = (IDictionary<string, object>)lastData;
			lastDataRef[lastSubPath] = content;
			return;
		}
	}
}
