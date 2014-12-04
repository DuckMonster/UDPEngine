using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace UDP.Server
{
    public class Server
    {
		List<Client> clientList = new List<Client>();
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

			}
		}

		public void AcceptHandle()
		{
			while (true)
			{
				Socket s = listener.AcceptSocket();
				ClientConnected(new Client(s, this));
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

		public void ClientConnected(Client c)
		{
			host.ClientConnected();
			clientList.Add(c);
		}
    }
}
