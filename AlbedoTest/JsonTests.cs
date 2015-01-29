using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Albedo.Core;

namespace AlbedoTest
{
	[TestClass]
	public class JsonTests
	{
		private string JsonReader(string fileReference)
		{
			var assembly = System.Reflection.Assembly.GetExecutingAssembly();
			Stream stream = assembly.GetManifestResourceStream(fileReference);
			StreamReader reader = new StreamReader(stream);
			string fileString = reader.ReadToEnd();
			return fileString;
		}

		//Removes things like line breaks and whitespace
		private string Minimize(string toClean)
		{
			string cleaned = Regex.Replace(toClean,"(\\s)+","");
			return cleaned;
		}

		[TestMethod] //Use to test typical data
		public void StringObjectString_Simple()
		{
			string originalString = JsonReader("AlbedoTest.TestResources.simpleTestJSON.txt");
			dynamic parsedString = JsonParser.Deserialize(originalString);
			string parsedObject = Minimize(JsonParser.Serialize(parsedString));
			originalString = Minimize(originalString);
			Assert.AreEqual(originalString, parsedObject, "JSON does not match");
		}

		[TestMethod] //Use to test blank JSON
		public void StringObjectString_Empty()
		{
			string originalString = "{}";
			dynamic parsedString = JsonParser.Deserialize(originalString);
			string parsedObject = Minimize(JsonParser.Serialize(parsedString));
			Assert.AreEqual(originalString, parsedObject, "JSON does not match");
		}

		[TestMethod] //(Use to test lots of data and empty objects)
		public void StringObjectString_Tricky()
		{
			string originalString = JsonReader("AlbedoTest.TestResources.trickyTestJSON.txt");
			dynamic parsedString = JsonParser.Deserialize(originalString);
			string parsedObject = Minimize(JsonParser.Serialize(parsedString));
			originalString = Minimize(originalString);
			Assert.AreEqual(originalString, parsedObject, "JSON does not match");
		}
		
		[TestMethod] //(Use to test array handling)
		public void StringObjectString_Mockaroo()
		{
			string originalString = JsonReader("AlbedoTest.TestResources.mockarooTestJSON.txt");
			dynamic parsedString = JsonParser.Deserialize(originalString);
			string parsedObject = Minimize(JsonParser.Serialize(parsedString));
			originalString = Minimize(originalString);
			Assert.AreEqual(originalString, parsedObject, "JSON does not match");
		}
	}
}
