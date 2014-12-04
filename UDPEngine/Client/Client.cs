using System;
using System.Net;
using System.Net.Sockets;

namespace UDP.Client
{
	public class Client
	{
		IClient host;
		Socket serverSocket;

		public Client(IClient h)
		{
			host = h;
		}
		public Client(IPEndPoint ip, IClient h)
		{
			host = h;
			Connect(ip);
		}
		public Client(string ip, int port, IClient h)
		{
			host = h;
			Connect(new IPEndPoint(IPAddress.Parse(ip), port));
		}

		public void Connect(IPEndPoint ip)
		{
			serverSocket.Connect(ip);
		}
	}
}