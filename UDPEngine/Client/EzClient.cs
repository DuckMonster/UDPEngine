using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace EZUDP.Client
{
	public class EzClient
	{
		public delegate void ConnectHandle();
		public delegate void MessageHandle(MessageBuffer msg);
		public delegate void DisconnectHandle();

		int myID;

		List<MessageBuffer> inMessages = new List<MessageBuffer>(), outMessages = new List<MessageBuffer>();

		public event ConnectHandle OnConnect;
		public event DisconnectHandle OnDisconnect;
		public event MessageHandle OnMessage;

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
		}

		void ConnectThread()
		{
			try
			{
				tcpSocket = new TcpClient();
				udpSocket = new UdpClient();

				tcpSocket.Connect(tcpAdress);
				udpSocket.Connect(udpAdress);

				byte[] buff = new byte[4];
				tcpSocket.GetStream().Read(buff, 0, 4);

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
				Console.WriteLine(e);
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

					inMessages.Add(new MessageBuffer(data));
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
			}
		}

		void SendThread()
		{
			while (Connected)
			{
				while (outMessages.Count > 0)
				{
					udpSocket.Send(outMessages[0].Array, outMessages[0].Size);
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
					Console.WriteLine(e);
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
	}
}