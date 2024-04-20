using System.Net.Sockets;
using TcpServer.Common;

namespace TcpServer.ServerSide;

public class Client : IDisposable
{
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
        var stream = TcpClient.GetStream();

        byte[] bufferSize = new byte[2];
        stream.Read(bufferSize, 0, bufferSize.Length);

        if (bufferSize[0] == 0 && bufferSize[1] == 0)
            return new Message("");

        byte[] buffer = new byte[BitConverter.ToUInt16(bufferSize)];
        stream.Read(buffer, 0, buffer.Length);

        return new Message(buffer);
    }

    internal void SendMessage(byte[] bytes)
    {
        if (!Connected)
            return;

        try
        {
            NetworkStream networkStream = TcpClient.GetStream();

            byte[] buffer = new byte[bytes.Length + 2];

            buffer[0] = (byte)bytes.Length;
            buffer[1] = (byte)(bytes.Length >> 8);

            Buffer.BlockCopy(bytes, 0, buffer, 2, bytes.Length);

            networkStream.Write(buffer, 0, buffer.Length);
        }
        catch (IOException ex)
        {
            Dispose();
        }
    }
    internal void SendMessage(string text)
    {
        if (!Connected)
            return;

        try
        {
            NetworkStream networkStream = TcpClient.GetStream();

            byte[] bytes = text.ToBytes();
            byte[] buffer = new byte[bytes.Length + 2];

            buffer[0] = (byte)bytes.Length;
            buffer[1] = (byte)(bytes.Length >> 8);

            Buffer.BlockCopy(bytes, 0, buffer, 2, bytes.Length);

            networkStream.Write(buffer, 0, buffer.Length);
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
}
