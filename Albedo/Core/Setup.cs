using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Dynamic;
using System.IO;

namespace Albedo.Core
{
	public class Setup
	{
		async public static Task PairAttempt(string bridgeIP, string bridgeUser) {
			HttpClient client = new HttpClient();
			string address = AddressBuild.APIRootSetup(bridgeIP);
			try {
				dynamic dynContent = new ExpandoObject();
				string devicetype = String.Format("Albedo#{0}.{1}", Environment.MachineName, DateTime.Now.ToString());
				if (devicetype.Length > 40) {
					devicetype = devicetype.Substring(0, 40);
				}
				dynContent.devicetype = devicetype;
				//dynContent.username = bridgeUser; //OBSOLETE. Username is now assigned by the bridge.
				StringContent content = new StringContent(JsonParser.Serialize(dynContent));

				Task<HttpResponseMessage> postData = client.PostAsync(address, content);
				HttpResponseMessage response = await postData;
				string responseString = await response.Content.ReadAsStringAsync();

				//Ugly hack; to be removed once parser handles arrays properly
				responseString = responseString.Substring(1, responseString.Length - 2);

				Storage.tempData = JsonParser.Deserialize(responseString);
				return;
			} catch (Exception) {
				dynamic failData = new ExpandoObject();
				Storage.tempData = failData;
				return;
			}
		}

		private static bool finish = false;
		async private static void FinishTimer()
		{
			await Task.Delay(3000);
			finish = true;
			client.Close();
			return;
		}

		class UdpState
		{
			public UdpClient u = null;
			public IPEndPoint e = null;
		}

		public static void ReceiveCallback(IAsyncResult ar)
		{
			UdpClient u = (UdpClient)((UdpState)(ar.AsyncState)).u;
			IPEndPoint e = (IPEndPoint)((UdpState)(ar.AsyncState)).e;

			Byte[] receiveBytes = u.EndReceive(ar, ref e); string receiveString = Encoding.ASCII.GetString(receiveBytes); Console.WriteLine("Received: {0}", receiveString);
		}

		static UdpClient client = new UdpClient();

		async public static Task FindAttempt()
		{
			await Task.Run(() => FindAttemptSync()); //REPLACE WITH REAL ASYNC LATER
		}

		public static void FindAttemptSync()
		{
			client = new UdpClient();
			client.ExclusiveAddressUse = false; //Important #1
			client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true); //Important #2

			Byte[] request = Encoding.ASCII.GetBytes("M-SEARCH * HTTP/1.1\r\n");
			IPAddress ssdp = IPAddress.Parse("239.255.255.250");
			IPEndPoint endpoint = new IPEndPoint(ssdp, 1900);
			IPEndPoint endpointReply = new IPEndPoint(IPAddress.Any, 1900);

			try {
				client.JoinMulticastGroup(ssdp);
				client.Client.ReceiveTimeout = 3000;
				client.Send(request, request.Length, endpoint);

				finish = false;
				FinishTimer();
				while (!finish) {
					Byte[] replyBytes = client.Receive(ref endpointReply);
					string replyData = Encoding.ASCII.GetString(replyBytes);

					if (replyData.Contains("IpBridge")) {
						if (!Storage.addressArray.Contains(endpointReply.Address.ToString())) {
							Storage.addressArray.Add(endpointReply.Address.ToString());
						}
					}
				}

				return;
			} catch (Exception) {
				return;
			}
		}
	}
}
