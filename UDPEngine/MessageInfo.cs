using System;
using System.Net;
using System.Net.Sockets;

namespace UDP
{
	public class MessageInfo
	{
		MessageBuffer message;
		IPEndPoint adress;

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

		public MessageInfo(MessageBuffer message, IPEndPoint endPoint)
		{
			Message = message;
			Adress = endPoint;
		}

		public void Send()
		{
			using (UdpClient udp = new UdpClient(adress))
			{
				udp.Send(message.Array, message.Size);
			}
		}
	}
}