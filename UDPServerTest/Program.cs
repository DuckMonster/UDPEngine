using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using EZUDP;
using EZUDP.Server;

namespace UDPServerTest
{
	class Program
	{
		static void Main(string[] args)
		{
			Program p = new Program();
			while (true)
			{
				p.Logic();
				Thread.Sleep(10);
			}
		}

		EzServer server;
		List<Client> clientList = new List<Client>();
		Dictionary<Client, string> userList = new Dictionary<Client, string>();

		public Program()
		{
			server = new EzServer(1255, 1337);

			server.OnStart += OnStart;
			server.OnConnect += OnConnect;
			server.OnMessage += OnMessage;
			server.OnDisconnect += OnDisconnect;

			Thread t = new Thread(InputThread);
			t.Start();
		}

		public void Logic()
		{
			server.Update();
		}

		public void OnStart()
		{
			Console.WriteLine("Server started! Zip Zop!");
		}

		public void OnConnect(Client c)
		{
			Console.WriteLine("{0}[{1}] connected!", c.ID, c.udpAdress);
			clientList.Add(c);

			MessageBuffer msg = new MessageBuffer();
			msg.WriteString("Whats your name?");

			c.Send(msg);
		}

		public void OnMessage(Client c, MessageBuffer msg)
		{
			if (userList.ContainsKey(c))
			{
				string msgStr = String.Format("<{0}>: {1}", userList[c], msg.ReadString());
				Console.WriteLine(msgStr);

				MessageBuffer msgStrBuffer = new MessageBuffer();
				msgStrBuffer.WriteString(msgStr);
				foreach (Client cc in clientList) cc.Send(msgStrBuffer);
			}
			else
			{
				string name = msg.ReadString();
				userList.Add(c, name);

				MessageBuffer connectedMsg = new MessageBuffer();
				connectedMsg.WriteString("<" + name + ">[" + c.udpAdress + "]" + " connected!");

				foreach (Client cc in clientList) cc.Send(connectedMsg);
			}
		}

		public void OnDisconnect(Client c)
		{
			Console.WriteLine("{0}[{1}] disconnected!", c.ID, c.tcpAdress);
			clientList.Remove(c);
		}

		public void InputThread()
		{
			while (true)
			{
				string input = Console.ReadLine();

				if (server.Active)
				{
					string[] inputArgs = input.Split(' ');
					if (inputArgs[0] == "quit") server.Close();
					if (inputArgs[0] == "kick") server.GetClient(int.Parse(inputArgs[1])).Disconnect();
				}
				else
				{
					if (input == "start") server.StartUp();
				}
			}
		}
	}
}
