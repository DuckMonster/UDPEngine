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
			foreach (Client c in clientList) if (c.adress.Equals(ip)) return c;
			return null;
		}

		List<MessageInfo> inMessages = new List<MessageInfo>();
		List<MessageInfo> outMessages = new List<MessageInfo>();

		IServer host;
		TcpListener listener;
		Thread listenThread;

		int port;

		public Server(int port, IServer h)
		{
			host = h;
			this.port = port;
			listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
			listener.Start();

			listenThread = new Thread(AcceptHandle);
			listenThread.Start();
		}

		public void Update()
		{
			while (inMessages.Count > 0)
			{
				host.ClientMessage(GetClient(inMessages[0].Adress).ID, inMessages[0].Message);
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
			UdpClient client = new UdpClient(port);

			while (true)
			{
				IPEndPoint ip = new IPEndPoint(IPAddress.Any, 0);
				byte[] data = client.Receive(ref ip);

				MessageInfo info = new MessageInfo(new MessageBuffer(data), ip);
				inMessages.Add(info);
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

		public void ClientConnected(Socket s)
		{
			Client c = new Client(clientsConnected, s, this);

			host.ClientConnected(c.ID);
			clientList.Add(c);

			clientsConnected++;
		}

		public void ClientDisconnected(Client c)
		{
			host.ClientDisconnected(c.ID);
			clientList.Remove(c);
		}
    }
}
