using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace EZUDP.Client
{
	public class EzClient
	{
		public static class DebugInfo
		{
			public static bool Data = false;
		}

		static readonly byte pingByte = byte.MaxValue;
		Stopwatch pingWatch;

		bool Pinging
		{
			get
			{
				return pingWatch != null;
			}
		}

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
				return DownBytesTotal;
			}
		}

		public delegate void ConnectHandle();
		public delegate void MessageHandle(MessageBuffer msg);
		public delegate void DisconnectHandle();
		public delegate void ExceptionHandle(Exception e);
		public delegate void PingHandle(int m);
		public delegate void DebugHandle(string msg);

		int myID;

		List<string> debugMessageList = new List<string>();
		void Debug(string s) { debugMessageList.Add(s); }
		List<MessageBuffer> inMessages = new List<MessageBuffer>(), outMessages = new List<MessageBuffer>();

		public event ConnectHandle OnConnect;
		public event DisconnectHandle OnDisconnect;
		public event MessageHandle OnMessage;
		public event ExceptionHandle OnException;
		public event PingHandle OnPing;
		public event DebugHandle OnDebug;

		IPEndPoint tcpAdress, udpAdress;
		TcpClient tcpSocket;
		UdpClient udpSocket;

		Thread receiveThread, sendThread, aliveThread;

		public bool Connected
		{
			get
			{
				if (tcpSocket == null) return false;
				return tcpSocket.Connected;
			}
		}

		public EzClient()
		{
		}

		public void Connect(string ip, int tcpPort, int udpPort)
		{
			if (Connected) return;

			tcpAdress = new IPEndPoint(IPAddress.Parse(ip), tcpPort);
			udpAdress = new IPEndPoint(IPAddress.Parse(ip), udpPort);

			Thread connect = new Thread(ConnectThread);
			connect.Start();
		}

		public void Update()
		{
			while (inMessages.Count > 0)
			{
				OnMessage(inMessages[0]);
				inMessages.RemoveAt(0);
			}

			string[] debug = debugMessageList.ToArray();
			foreach (string s in debug) if (OnDebug != null) OnDebug(s);
			debugMessageList.Clear();
		}

		void ConnectThread()
		{
			try
			{
				tcpSocket = new TcpClient();
				udpSocket = new UdpClient();

				tcpSocket.Connect(tcpAdress);
				udpSocket.Connect(udpAdress);

				//Read accepted ID
				byte[] buff = new byte[4];
				NetworkStream s = tcpSocket.GetStream();

				for (int i = 0; i < buff.Length; i++)
					buff[i] = (byte)s.ReadByte();

				//Send it back
				myID = BitConverter.ToInt32(buff, 0);
				udpSocket.Send(buff, 4);

				receiveThread = new Thread(ReceiveThread);
				sendThread = new Thread(SendThread);
				aliveThread = new Thread(AliveThread);

				receiveThread.Start();
				sendThread.Start();
				aliveThread.Start();

				OnConnect();
			}
			catch (Exception e)
			{
				CatchException(e);
				Disconnect();
			}
		}

		void ReceiveThread()
		{
			while (Connected)
			{
				try
				{
					IPEndPoint ip = udpAdress;
					byte[] data = udpSocket.Receive(ref ip);

					if (DebugInfo.Data) Debug("Received " + data.Length);

					if (data.Length == 1 && data[0] == pingByte)
					{
						if (Pinging)
						{
							if (OnPing != null)
							{
								OnPing(pingWatch.Elapsed.Milliseconds);
							}

							pingWatch = null;
						}
						else
						{
							udpSocket.Send(data, data.Length);
						}

						continue;
					}

					inMessages.Add(new MessageBuffer(data));
					downByteBuffer += data.Length;
					downByteTotal += data.Length;
				}
				catch (Exception e)
				{
					CatchException(e);
				}
			}
		}

		void SendThread()
		{
			while (Connected)
			{
				while (outMessages.Count > 0)
				{
					if (DebugInfo.Data) Debug("Sent " + outMessages[0].Size);

					udpSocket.Send(outMessages[0].Array, outMessages[0].Size);
					upByteBuffer += outMessages[0].Size;
					upByteTotal += outMessages[0].Size;

					outMessages.RemoveAt(0);
				}

				Thread.Sleep(1);
			}
		}

		void AliveThread()
		{
			while (Connected)
			{
				try
				{
					tcpSocket.GetStream().Write(new byte[] { 1 }, 0, 1);
				}
				catch (Exception e)
				{
					CatchException(e);
				}

				Thread.Sleep(1000);
			}

			Disconnect();
		}

		public void Disconnect()
		{
			if (tcpSocket == null || udpSocket == null) return;

			if (tcpSocket.Connected) tcpSocket.GetStream().Close();
			tcpSocket.Close();
			udpSocket.Close();

			tcpSocket = null;
			udpSocket = null;

			OnDisconnect();
		}

		public void Send(MessageBuffer msg)
		{
			outMessages.Add(msg);
		}

		void CatchException(Exception e)
		{
			if (OnException != null) OnException(e);
		}

		public void Ping()
		{
			if (!Pinging)
			{
				pingWatch = Stopwatch.StartNew();
				udpSocket.Send(new byte[] { pingByte }, 1);
			}
		}
	}
}