using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace UDP.Server
{
	public class Client
	{
		public int ID;
		public Server server;
		public IPEndPoint tcpAdress, udpAdress;
		Socket socket;

		public Client(int id, Socket sock, Server serv)
		{
			ID = id;
			server = serv;
			socket = sock;

			tcpAdress = (IPEndPoint)sock.RemoteEndPoint;
			Thread t = new Thread(DisconnectCheckHandle);
			t.Start();
		}

		public void SendAcceptPoll()
		{
			socket.Send(BitConverter.GetBytes(ID));
		}

		public void DisconnectCheckHandle()
		{
			while (socket.Connected)
			{
				try
				{
					socket.Send(new byte[] { });
				}
				catch (Exception e)
				{

				}
			}

			server.ClientDisconnected(this);
		}
	}
}