using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

namespace UDP.Client
{
	public class Client
	{
		int myID;

		List<MessageBuffer> inMessages = new List<MessageBuffer>();
		List<MessageBuffer> outMessages = new List<MessageBuffer>();

		IClient host;
		IPEndPoint serverTCP, serverUDP;
		Socket TCPSocket;
		UdpClient UDPSocket;

		Thread listenThread, sendThread, disconnectThread;

		public Client(IClient h)
		{
			host = h;
		}
		public Client(string ip, int tcpPort, int udpPort, IClient h)
		{
			host = h;
			Connect(ip, tcpPort, udpPort);
		}

		public void Connect(string ip, int tcpPort, int udpPort)
		{
			serverTCP = new IPEndPoint(IPAddress.Parse(ip), tcpPort);
			serverUDP = new IPEndPoint(IPAddress.Parse(ip), udpPort);

			TCPSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			UDPSocket = new UdpClient();

			try
			{
				TCPSocket.Connect(serverTCP);
				UDPSocket.Connect(serverUDP);

				Thread accept = new Thread(AcceptHandle);
				listenThread = new Thread(ListenHandle);
				sendThread = new Thread(SendHandle);
				disconnectThread = new Thread(CheckDisconnectHandle);

				listenThread.Start();
				sendThread.Start();
				disconnectThread.Start();
				accept.Start();

				host.ServerConnected();
			}
			catch (Exception e)
			{
				Disconnect();
			}
		}

		void Disconnect()
		{
			host.ServerDisconnected();
		}

		public void Update()
		{
			while (inMessages.Count > 0)
			{
				host.ServerMessage(inMessages[0]);
				inMessages.RemoveAt(0);
			}
		}

		void AcceptHandle()
		{
			byte[] buff = new byte[4];
			TCPSocket.Receive(buff);

			myID = BitConverter.ToInt32(buff, 0);
			Send(new MessageBuffer(buff));
		}

		void ListenHandle()
		{
			while (TCPSocket.Connected)
			{
				if (serverUDP == null)
				{
					IPEndPoint ip = serverUDP;
					var data = UDPSocket.Receive(ref serverTCP);

					if (data.Length == 4 && BitConverter.ToInt32(data, 0) == myID)
					{
						serverUDP = ip;
						UDPSocket.Connect(serverUDP);
					}
				}
				else
				{
					IPEndPoint ip = serverUDP;
					var data = UDPSocket.Receive(ref serverTCP);

					inMessages.Add(new MessageBuffer(data));
				}
			}
		}

		void SendHandle()
		{
			while (TCPSocket.Connected)
			{
				while (outMessages.Count > 0)
				{
					UDPSocket.Send(outMessages[0].Array, outMessages[0].Size);
					outMessages.RemoveAt(0);
				}
				Thread.Sleep(5);
			}
		}

		void CheckDisconnectHandle()
		{
			while (TCPSocket.Connected)
			{
				try
				{
					TCPSocket.Send(new byte[] { });
				}
				catch (Exception e) {}
			}

			Disconnect();
		}

		public void Send(MessageBuffer msg)
		{
			outMessages.Add(msg);
		}
	}
}