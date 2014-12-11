﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace EZUDP.Server
{
	public class EzServer
	{
		public static class DebugInfo
		{
			public static bool Data = false;
		}

		public static byte pingByte = byte.MaxValue;

		static int upByteBuffer, downByteBuffer;
		public static int UpBytes
		{
			get
			{
				int n = upByteBuffer;
				upByteBuffer = 0;

				return n;
			}
		}

		public static int DownBytes
		{
			get
			{
				int n = downByteBuffer;
				downByteBuffer = 0;

				return n;
			}
		}

		public delegate void StartHandle();
		public delegate void ConnectHandle(Client c);
		public delegate void MessageHandle(Client c, MessageBuffer m);
		public delegate void DisconnectHandle(Client c);
		public delegate void ExceptionHandle(Exception e);
		public delegate void PingHandle(Client c, int millis);
		public delegate void DebugHandle(string msg);

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

		List<Client> disconnectedList = new List<Client>(), connectedList = new List<Client>();
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
		public event ExceptionHandle OnException;
		public event PingHandle OnPing;
		public event DebugHandle OnDebug;

		List<string> debugMessageList = new List<string>();
		void Debug(string s) { debugMessageList.Add(s); }
		List<MessageInfo> inMessages = new List<MessageInfo>(), outMessages = new List<MessageInfo>();

		UdpClient udpSocket;
		TcpListener tcpSocket;
		int tcpPort, udpPort;

		Thread acceptThread, receiveThread, sendThread;

		public bool Active
		{
			get
			{
				return udpSocket != null && tcpSocket != null;
			}
		}

		public string LocalIP
		{
			get
			{
				IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
				string localIP = "";

				foreach (IPAddress ip in host.AddressList)
				{
					if (ip.AddressFamily == AddressFamily.InterNetwork)
					{
						localIP = ip.ToString();
						break;
					}
				}

				return localIP;
			}
		}

		public EzServer(int tcp, int udp)
		{
			tcpPort = tcp;
			udpPort = udp;
		}

		public void StartUp() { StartUp(LocalIP); }
		public void StartUp(string ip)
		{
			udpSocket = new UdpClient(udpPort);
			tcpSocket = new TcpListener(IPAddress.Parse(ip), tcpPort);

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

			while (connectedList.Count > 0)
			{
				OnConnect(connectedList[0]);
				connectedList.RemoveAt(0);
			}

			while (disconnectedList.Count > 0)
			{
				OnDisconnect(disconnectedList[0]);
				disconnectedList.RemoveAt(0);
			}

			string[] debug = debugMessageList.ToArray();
			foreach (string s in debug) if (OnDebug != null) OnDebug(s);
			debugMessageList.Clear();
		}

		void AcceptThread()
		{
			tcpSocket.Start();

			while (Active)
			{
				try
				{
					Socket s = tcpSocket.AcceptSocket();
					ClientConnected(s);
				}
				catch (Exception e)
				{
					CatchException(e);
					break;
				}
			}
		}

		void ReceiveThread()
		{
			while (Active)
			{
				try
				{
					IPEndPoint ip = new IPEndPoint(IPAddress.Any, 0);
					byte[] data = udpSocket.Receive(ref ip);

					if (DebugInfo.Data) Debug("Received " + data.Length);

					Client c = GetClient(ip);

					//Pinged
					if (c != null && data.Length == 1 && data[0] == pingByte)
					{
						if (c.Pinging)
						{
							c.Ping();
						}
						else
						{
							Send(new MessageBuffer(data), c);
						}

						continue;
					}

					if (c != null) inMessages.Add(new MessageInfo(new MessageBuffer(data), ip, this));
					else if (data.Length == 4)
					{
						int id = BitConverter.ToInt32(data, 0);
						c = GetClient(id);

						if (c != null)
						{
							c.udpAdress = ip;
							connectedList.Add(c);
						}
					}

					downByteBuffer += data.Length;
				}
				catch (Exception e)
				{
					CatchException(e);
				}
			}
		}

		void SendThread()
		{
			while (Active)
			{
				while (outMessages.Count > 0)
				{
					if (DebugInfo.Data) Debug("Sent " + outMessages[0].Message.Size);

					outMessages[0].Send();
					upByteBuffer += outMessages[0].Message.Size;
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
			disconnectedList.Add(c);
		}

		public void Send(MessageBuffer msg, int id) { Send(msg, GetClient(id)); }
		public void Send(MessageBuffer msg, Client c)
		{
			if (c != null && c.udpAdress != null)
				outMessages.Add(new MessageInfo(msg, c.udpAdress, this));
		}

		public void Close()
		{
			if (tcpSocket == null || udpSocket == null) return;

			tcpSocket.Stop();
			udpSocket.Close();

			tcpSocket = null;
			udpSocket = null;

			acceptThread.Abort();
			sendThread.Abort();
			receiveThread.Abort();

			acceptThread = null;
			sendThread = null;
			receiveThread = null;

			List<Client> list = new List<Client>();
			list.AddRange(clientList);

			foreach (Client c in list) c.Disconnect();
		}

		void CatchException(Exception e)
		{
			if (OnException != null) OnException(e);
		}

		public void PingResult(Client c, int millis)
		{
			if (OnPing != null) OnPing(c, millis);
		}
	}
}