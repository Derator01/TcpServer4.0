using System.Net.Sockets;
using TcpServer.Common;

namespace TcpServer.ServerSide;

public class Client : IDisposable
{
    public const int PACKET_SIZE = 1024;

    public bool Connected { get => TcpClient.Connected; }

    public string Name { get; set; }

    private TcpClient TcpClient { get; }

    public Client(TcpClient client)
    {
        TcpClient = client;
    }

    public bool IsMessagePending()
    {
        if (Connected)
            return TcpClient.GetStream().DataAvailable;
        return false;
    }

    internal Message RecieveMessage()
    {
        byte[] buffer = new byte[PACKET_SIZE];
        TcpClient.GetStream().Read(buffer, 0, buffer.Length);

        return new Message(buffer);
    }

    internal void SendMessage(string text)
    {
        if (!Connected)
            return;

        try
        {
            TcpClient.GetStream().Write(text.ToBytes());
        }
        catch (IOException ex)
        {
            Dispose();
        }
    }

    public void Dispose()
    {
        TcpClient?.Close();
    }

    internal void Close()
    {
        TcpClient?.Close();
    }

    // TODO: Handshake
}
