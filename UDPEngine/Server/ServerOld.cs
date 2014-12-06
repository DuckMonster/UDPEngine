using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace UDP.Server
{
    public class Server
    {
		int clientsConnected = 0;

		List<Client> clientList = new List<Client>();
		public Client GetClient(int id)
		{
			foreach (Client c in clientList) if (c.ID == id) return c;
			return null;
		}
		public Client GetClient(IPEndPoint ip)
		{
			foreach (Client c in clientList) if (c.udpAdress != null && c.udpAdress.Equals(ip)) return c;
			return null;
		}

		List<MessageInfo> inMessages = new List<MessageInfo>();
		List<MessageInfo> outMessages = new List<MessageInfo>();

		IServer host;
		TcpListener listener;
		public UdpClient udpClient;
		Thread acceptThread, listenThread, sendThread;

		int tcpPort, udpPort;

		public Server(int tcpPort, int udpPort, IServer h)
		{
			host = h;
			this.tcpPort = tcpPort;
			this.udpPort = udpPort;
			listener = new TcpListener(IPAddress.Parse("127.0.0.1"), tcpPort);
			listener.Start();

			udpClient = new UdpClient(udpPort);

			acceptThread = new Thread(AcceptHandle);
			acceptThread.Start();

			listenThread = new Thread(ListenHandle);
			listenThread.Start();

			sendThread = new Thread(SendHandle);
			sendThread.Start();
		}

		public void Update()
		{
			while (inMessages.Count > 0)
			{
				Client c = GetClient(inMessages[0].Adress);
				if (c != null) host.ClientMessage(c.ID, inMessages[0].Message);

				inMessages.RemoveAt(0);
			}
		}

		public void AcceptHandle()
		{
			while (true)
			{
				Socket s = listener.AcceptSocket();
				ClientConnected(s);
			}
		}

		public void ListenHandle()
		{
			while (true)
			{
				IPEndPoint ip = new IPEndPoint(IPAddress.Any, 0);
				byte[] data = udpClient.Receive(ref ip);

				Client c = GetClient(ip);
				 
				if (c == null && data.Length == 4)
				{
					Client c2 = GetClient(BitConverter.ToInt32(data, 0));
					if (c2 != null)
					{
						c2.udpAdress = ip;
						Send(new MessageBuffer(data), c2.ID);
					}
				}

				if (c != null)
				{
					MessageInfo info = new MessageInfo(new MessageBuffer(data), ip, this);
					inMessages.Add(info);
				}
			}
		}

		public void SendHandle()
		{
			while (true)
			{
				while (outMessages.Count > 0)
				{
					outMessages[0].Send();
					outMessages.RemoveAt(0);
				}
				Thread.Sleep(5);
			}
		}

		void ClientConnected(Socket s)
		{
			Client c = new Client(clientsConnected, s, this);

			host.ClientConnected(c.ID);
			clientList.Add(c);
			c.SendAcceptPoll();

			clientsConnected++;
		}

		public void ClientDisconnected(Client c)
		{
			host.ClientDisconnected(c.ID);
			clientList.Remove(c);
		}

		public void Send(MessageBuffer msg, int id)
		{
			Client c = GetClient(id);
			if (c != null && c.udpAdress != null)
				outMessages.Add(new MessageInfo(msg, GetClient(id).udpAdress, this));
		}
    }
}
