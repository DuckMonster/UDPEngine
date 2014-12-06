using System;
using System.Net;
using System.Net.Sockets;

namespace EZUDP
{
	public class MessageInfo
	{
		MessageBuffer message;
		IPEndPoint adress;
		Server.Server server;

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

		public MessageInfo(MessageBuffer message, IPEndPoint endPoint, Server.Server s)
		{
			server = s;
			Message = message;
			Adress = endPoint;
		}

		public void Send()
		{
			server.udpClient.Send(message.Array, message.Size, adress);
		}
	}
}