using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace EZUDP.Server
{
	public class Client
	{
		public int ID;
		public IPEndPoint tcpAdress, udpAdress;
		EzServer server;
		Socket socket;

		public Client(int id, Socket sock, EzServer serv)
		{
			ID = id;
			server = serv;
			socket = sock;

			tcpAdress = (IPEndPoint)sock.RemoteEndPoint;
			Thread t = new Thread(AliveThread);
			t.Start();

			SendAcceptPoll();
		}

		void SendAcceptPoll()
		{
			socket.Send(BitConverter.GetBytes(ID));
		}

		void AliveThread()
		{
			while (IsConnected() && server.Active)
			{
				Thread.Sleep(50);
			}
		}

		bool IsConnected()
		{
			try
			{
				return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
			}
			catch (Exception e)
			{
				return false;
			}
		}

		public void Send(MessageBuffer msg)
		{
			server.Send(msg, this);
		}

		public void Disconnect()
		{
			socket.Dispose();
			server.ClientDisconnected(this);
		}
	}
}