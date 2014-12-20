using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using EZUDP;
using EZUDP.Client;

namespace UDPClientTest
{
	class Program
	{
		public static void Main(string[] args)
		{
			Program p = new Program();
			while (true)
			{
				p.Logic();
				Thread.Sleep(10);
			}
		}

		EzClient client;

		public Program()
		{
			Thread t = new Thread(InputThread);
			t.Start();

			client = new EzClient();
			client.OnConnect += OnConnect;
			client.OnDisconnect += OnDisconnect;
			client.OnMessage += OnMessage;
		}

		public void Logic()
		{
			client.Update();
		}

		public void OnConnect()
		{
			Console.WriteLine("Connected to server!");
		}

		public void OnMessage(MessageBuffer msg)
		{
			Console.WriteLine(msg.ReadString());
		}

		public void OnDisconnect()
		{
			Console.WriteLine("Disconnected from server!");
		}

		public void InputThread()
		{
			while (true)
			{
				string m = Console.ReadLine();

				if (client.Connected)
				{
					if (m == "quit")
					{
						client.Disconnect();
						Console.Clear();
					}
					else
					{
						MessageBuffer msg = new MessageBuffer();
						msg.WriteString(m);
						if (client.Connected) client.Send(msg);
					}
				}
				else
				{
					if (m == "connect")
					{
						Console.Clear();
						Console.WriteLine("Connecting....");
						client.Connect("127.0.0.1", 1255, 1337);
					}
				}
			}
		}
	}
}
