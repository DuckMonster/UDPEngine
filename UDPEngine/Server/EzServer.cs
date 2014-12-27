using System;
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
			public static bool upData = false;
			public static bool downData = false;
			public static bool acceptData = false;
		}

		public static byte pingByte = byte.MaxValue;

		static int upByteBuffer, downByteBuffer;
		static int upByteTotal, downByteTotal;
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

		public static int UpBytesTotal
		{
			get
			{
				return upByteTotal;
			}
		}

		public static int DownBytesTotal
		{
			get
			{
				return downByteTotal;
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
			EzServer server;

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

			public MessageInfo(MessageBuffer message, IPEndPoint endPoint, EzServer s)
			{
				server = s;
				Message = message;
				Adress = endPoint;
			}

			public void Send()
			{
				new Thread(SendThread).Start();
			}

			void SendThread()
			{
				server.SendData(message.Array, adress);
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
		public void Debug(string s) { debugMessageList.Add(s); }
		List<MessageInfo> inMessages = new List<MessageInfo>(), outMessages = new List<MessageInfo>();
		List<Tuple<byte[], IPEndPoint>> inMessagesRaw = new List<Tuple<byte[], IPEndPoint>>();

		UdpClient udpSocket;
		TcpListener tcpSocket;
		int tcpPort, udpPort;

		Thread acceptThread, receiveThread, receiveDataThread, sendThread;

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
			try
			{
				udpSocket = new UdpClient(udpPort);
				tcpSocket = new TcpListener(IPAddress.Parse(ip), tcpPort);

				acceptThread = new Thread(AcceptThread);
				receiveThread = new Thread(ReceiveThread);
				receiveDataThread = new Thread(ReceiveDataThread);
				sendThread = new Thread(SendThread);

				acceptThread.Start();
				receiveThread.Start();
				receiveDataThread.Start();
				sendThread.Start();

				OnStart();
			}
			catch (Exception e)
			{
				CatchException(e);
			}
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

			while (debugMessageList.Count > 0)
			{
				OnDebug(debugMessageList[0]);
				debugMessageList.RemoveAt(0);
			}
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

					inMessagesRaw.Add(Tuple.Create(data, ip));
				}
				catch (SocketException e)
				{

				}
				catch (Exception e)
				{
					CatchException(e);
				}
			}
		}

		void ReceiveDataThread()
		{
			while (Active)
			{
				try
				{
					while (inMessagesRaw.Count > 0)
					{
						ReceiveData(inMessagesRaw[0].Item1, inMessagesRaw[0].Item2);
						inMessagesRaw.RemoveAt(0);
					}

					Thread.Sleep(5);
				}
				catch (Exception e)
				{
					CatchException(e);
				}
			}
		}

		void ReceiveData(byte[] data, IPEndPoint ip)
		{
			if (DebugInfo.downData && data.Length > 1)
			{
				string dataString = "";
				foreach (byte b in data) dataString += b + " ";
				Debug("Received " + data.Length + "[" + dataString + "]");
			}

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

				return;
			}

			if (c != null)
			{
				inMessages.Add(new MessageInfo(new MessageBuffer(data), ip, this));
			}
			else if (data.Length == 4)
			{
				int id = BitConverter.ToInt32(data, 0);
				c = GetClient(id);

				if (c != null)
				{
					if (DebugInfo.acceptData) Debug("Received udp ip for ID " + id);

					c.udpAdress = ip;
					connectedList.Add(c);
				}
			}

			if (c == null)
			{
				Debug("Received data from unknown client...");
			}

			downByteBuffer += data.Length;
			downByteTotal += data.Length;
		}

		void SendThread()
		{
			while (Active)
			{
				while (outMessages.Count > 0)
				{
					if (outMessages[0] != null)
					{
						if (DebugInfo.downData && outMessages[0].Message.Size > 1)
						{
							string dataString = "";
							foreach (byte b in outMessages[0].Message.Array) dataString += b + " ";
							Debug("Sending " + outMessages[0].Message.Size + "[" + dataString + "]");
						}

						outMessages[0].Send();
						upByteBuffer += outMessages[0].Message.Size;
						upByteTotal += outMessages[0].Message.Size;

						outMessages.RemoveAt(0);
					}

					Thread.Sleep(1);
				}
			}
		}

		void ClientConnected(Socket s)
		{
			Client c = new Client(nmbrOfClients, s, this);
			clientList.Add(c);
			c.SendAcceptPoll();

			nmbrOfClients++;
		}

		public void ClientDisconnected(Client c)
		{
			clientList.Remove(c);
			disconnectedList.Add(c);
		}

		void SendData(byte[] data, IPEndPoint ip)
		{
			udpSocket.Send(data, data.Length, ip);
		}
		public void Send(MessageBuffer msg, int id) { Send(msg, GetClient(id)); }
		public void Send(MessageBuffer msg, Client c)
		{
			/*
			if (c != null && c.udpAdress != null)
				outMessages.Add(new MessageInfo(msg, c.udpAdress, this));
			 * */

			new MessageInfo(msg, c.udpAdress, this).Send();
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
			receiveDataThread.Abort();

			acceptThread = null;
			sendThread = null;
			receiveThread = null;
			receiveDataThread = null;

			List<Client> list = new List<Client>();
			list.AddRange(clientList);

			foreach (Client c in list) c.Disconnect();
		}

		public void CatchException(Exception e)
		{
			if (OnException != null) OnException(e);
		}

		public void PingResult(Client c, int millis)
		{
			if (OnPing != null) OnPing(c, millis);
		}
	}
}