using System;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace UDP.Server
{
	public class Client
	{
		public int ID;
		public Server server;
		public IPEndPoint adress;
		Socket socket;

		public Client(int id, Socket sock, Server serv)
		{
			ID = id;
			server = serv;
			socket = sock;

			adress = (IPEndPoint)sock.RemoteEndPoint;
		}

		public void DisconnectCheckHandle()
		{
			int msg = 0;
			NetworkStream stream = new NetworkStream(socket);

			do
			{
				msg = stream.Read(new byte[1], 0, 1);
			} while (msg != -1);
		}
	}
}