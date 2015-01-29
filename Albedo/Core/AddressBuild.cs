using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Albedo.Core
{
	public class AddressBuild
	{
		static string IPAddress;
		static string userName;
		public static string selectedGroup;

		public static int InitializeVar(string newIPAddress, string newUserName, string newGroup)
		{
			IPAddress = newIPAddress;
			userName = newUserName;
			selectedGroup = newGroup;

			return 0;
		}

		public static string APIRootSetup(string address)
		{
			return String.Format("http://{0}/api/", address);
		}

		public static string UserRoot()
		{
			return String.Format("http://{0}/api/{1}/", IPAddress, userName);
		}

		public static string LightsRoot()
		{
			return String.Format("http://{0}/api/{1}/lights/", IPAddress, userName);
		}

		public static string GroupsRoot()
		{
			return String.Format("http://{0}/api/{1}/groups/", IPAddress, userName);
		}

		public static string LightState(string light)
		{
			return String.Format("http://{0}/api/{1}/lights/{2}/state/", IPAddress, userName, light);
		}

		public static string GroupUri()
		{
			return String.Format("http://{0}/api/{1}/groups/{2}/", IPAddress, userName, selectedGroup);
		}

		public static string GroupUriSpecify(string group)
		{
			return String.Format("http://{0}/api/{1}/groups/{2}/", IPAddress, userName, group);
		}
	}
}
