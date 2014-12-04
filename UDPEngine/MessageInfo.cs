using System;
using System.Net;
using System.Net.Sockets;

namespace UDP
{
	public class MessageInfo
	{
		MessageBuffer message;
		IPEndPoint endPoint;

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
		public IPEndPoint EndPoint
		{
			get
			{
				return endPoint;
			}
			set
			{
				endPoint = value;
			}
		}

		public MessageInfo(MessageBuffer message, IPEndPoint endPoint)
		{
			Message = message;
			EndPoint = endPoint;
		}

		public void Send()
		{
			using (UdpClient udp = new UdpClient(endPoint))
			{
				udp.Send(message.Array, message.Size);
			}
		}
	}
}