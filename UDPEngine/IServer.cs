using System;

namespace UDP.Server
{
	public interface IServer
	{
		public void ClientConnected();
		public void ClientMessage();
		public void ClientDisconnected();
	}
}