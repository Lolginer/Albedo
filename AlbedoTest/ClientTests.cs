using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Albedo.Core;

namespace AlbedoTest
{
	[TestClass]
	public class ClientTests
	{
		[TestMethod]
		public void UdpRequest_DoesNotExplode()
		{
			Setup.FindAttempt();
		}
	}
}
