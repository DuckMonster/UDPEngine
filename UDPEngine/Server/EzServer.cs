﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace EZUDP.Server
{
	public class EzServer
	{
		public delegate void StartHandle();
		public delegate void ConnectHandle(Client c);
		public delegate void MessageHandle(Client c, MessageBuffer m);
		public delegate void DisconnectHandle(Client c);

		int nmbrOfClients = 0;

		
		class MessageInfo
		{
			MessageBuffer message;
			IPEndPoint adress;
			Server.EzServer server;

			public MessageBuffer Message
			{
				get
				{
					return message;
				}
				set
				{
					message = value;
				}
			}
			public IPEndPoint Adress
			{
				get
				{
					return adress;
				}
				set
				{
					adress = value;
				}
			}

			public MessageInfo(MessageBuffer message, IPEndPoint endPoint, Server.EzServer s)
			{
				server = s;
				Message = message;
				Adress = endPoint;
			}

			public void Send()
			{
				server.udpSocket.Send(message.Array, message.Size, adress);
			}
		}

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

		public event StartHandle OnStart;
		public event ConnectHandle OnConnect;
		public event DisconnectHandle OnDisconnect;
		public event MessageHandle OnMessage;

		List<MessageInfo> inMessages = new List<MessageInfo>(), outMessages = new List<MessageInfo>();

		UdpClient udpSocket;
		TcpListener tcpSocket;
		int tcpPort, udpPort;

		Thread acceptThread, receiveThread, sendThread;

		public EzServer(int tcp, int udp)
		{
			tcpPort = tcp;
			udpPort = udp;
		}

		public void StartUp()
		{
			udpSocket = new UdpClient(udpPort);
			tcpSocket = new TcpListener(IPAddress.Parse("127.0.0.1"), tcpPort);

			acceptThread = new Thread(AcceptThread);
			receiveThread = new Thread(ReceiveThread);
			sendThread = new Thread(SendThread);

			acceptThread.Start();
			receiveThread.Start();
			sendThread.Start();

			OnStart();
		}

		public void Update()
		{
			while (inMessages.Count > 0)
			{
				OnMessage(GetClient(inMessages[0].Adress), inMessages[0].Message);
				inMessages.RemoveAt(0);
			}
		}

		void AcceptThread()
		{
			tcpSocket.Start();

			while (true)
			{
				Socket s = tcpSocket.AcceptSocket();
				ClientConnected(s);
			}
		}

		void ReceiveThread()
		{
			while (true)
			{
				IPEndPoint ip = new IPEndPoint(IPAddress.Any, 0);
				byte[] data = udpSocket.Receive(ref ip);

				Client c = GetClient(ip);

				if (c != null) inMessages.Add(new MessageInfo(new MessageBuffer(data), ip, this));
				else if (data.Length == 4)
				{
					int id = BitConverter.ToInt32(data, 0);
					c = GetClient(id);

					if (c != null)
					{
						c.udpAdress = ip;
						OnConnect(c);
					}
				}
			}
		}

		void SendThread()
		{
			while (true)
			{
				while (outMessages.Count > 0)
				{
					outMessages[0].Send();
					outMessages.RemoveAt(0);
				}

				Thread.Sleep(1);
			}
		}

		void ClientConnected(Socket s)
		{
			Client c = new Client(nmbrOfClients, s, this);
			clientList.Add(c);

			nmbrOfClients++;
		}

		public void ClientDisconnected(Client c)
		{
			clientList.Remove(c);
			OnDisconnect(c);
		}

		public void Send(MessageBuffer msg, int id) { Send(msg, GetClient(id)); }
		public void Send(MessageBuffer msg, Client c)
		{
			if (c != null && c.udpAdress != null)
				outMessages.Add(new MessageInfo(msg, c.udpAdress, this));
		}
	}
}