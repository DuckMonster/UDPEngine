namespace UDP.Client
{
	public interface IClient
	{
		void ServerConnected();
		void ServerMessage(MessageBuffer msg);
		void ServerDisconnected();
	}
}