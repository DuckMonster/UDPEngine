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
				Thread.Sleep(1000);
			}

			Disconnect();
		}

		bool IsConnected()
		{
			try
			{
				socket.Send(new byte[] { 0 });
				return true;
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
			if (socket == null) return;

			socket.Close();
			socket = null;
			server.ClientDisconnected(this);
		}
	}
}