using System;

namespace UDP.Server
{
	public interface IServer
	{
		public void ClientConnected(int id);
		public void ClientMessage(int id, MessageBuffer msg);
		public void ClientDisconnected(int id);
	}
}