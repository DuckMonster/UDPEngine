using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace EZUDP.Server
{
	public class Server
	{
		public delegate void ClientConnected(int id);
		public delegate void ClientMessage(int id, MessageBuffer m);
		public delegate void ClientDisconnected(int id);

		List<MessageInfo> inMessages = new List<MessageInfo>(), outMessages = new List<MessageInfo>();

		UdpClient udpSocket;
		TcpListener tcpSocket;
		int tcpPort, udpPort;

		public Server(int tcp, int udp)
		{
			tcpPort = tcp;
			udpPort = udp;

			StartUp();
		}

		public void StartUp()
		{
			udpSocket = new UdpClient(udpPort);
			tcpSocket = new TcpListener(IPAddress.Parse("127.0.0.1"), tcpPort);
		}

		public void AcceptHandle()
		{
			while (true)
			{
				Socket s = tcpSocket.Acc
			}
		}

		public void ReceiveHandle()
		{
		}

		public void SendHandle()
		{
		}
	}
}