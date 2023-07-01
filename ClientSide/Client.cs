using System.Net;
using System.Net.Sockets;
using TcpServer.Common;

namespace TcpServer.ClientSide;

public class Client
{
    public const int PACKET_SIZE = 1024;

    public string Name { get; private set; }

    public bool Connected { get => TcpClient.Connected; }

    private TcpClient TcpClient { get; set; } = new TcpClient();
    private IPEndPoint EndPoint { get; }

    #region Events
    public delegate void OnConnectionFailed(string message);
    public event OnConnectionFailed? ConnectionFailed;

    public delegate void OnConnectionSucceded(string serverName);
    public event OnConnectionSucceded? ConnectionSucceded;

    public delegate void OnMessageCame(string message);
    public event OnMessageCame? MessageCame;

    public delegate void OnDisconnected();
    public event OnDisconnected? Disconnected;
    #endregion


    /// <summary>
    /// With this constructor client will search through all local IPs.
    /// </summary>
    /// <param name="name">Name of the client that will be known to server</param>
    public Client(string name, int port = 7000)
    {
        Name = name;
        EndPoint = new IPEndPoint(IPAddress.Any, port);
    }
    public Client(string name, IPAddress ip, int port = 7000)
    {
        Name = name;
        EndPoint = new IPEndPoint(ip, port);
    }
    public Client(string name, IPEndPoint endPoint)
    {
        Name = name;
        EndPoint = endPoint;
    }

    public async Task<bool> ConnectAsync()
    {
        if (EndPoint.Address == IPAddress.Any)
        {
            for (byte b = 101; b < 255; b++)
            {
                IPAddress ipAddress = new(new byte[] { 192, 168, 1, b });

                if (await TryConnectAsync(new IPEndPoint(ipAddress, EndPoint.Port)))
                {
                    return true;
                }
            }
            for (byte b = 0; b < 100; b++)
            {
                IPAddress ipAddress = new(new byte[] { 192, 168, 1, b });

                if (await TryConnectAsync(new IPEndPoint(ipAddress, EndPoint.Port)))
                {
                    return true;
                }
            }

            ConnectionFailed?.Invoke("Connection failed for all IP addresses in the range.");
            return false;
        }
        else
        {
            return await TryConnectAsync(EndPoint);
        }
    }

    private async Task<bool> TryConnectAsync(IPEndPoint endPoint)
    {
        var tcpClient = new TcpClient();

        int retryDelayMilliseconds = 20;

        try
        {
            Task connectTask = tcpClient.ConnectAsync(endPoint);

            if (await Task.WhenAny(connectTask, Task.Delay(retryDelayMilliseconds)) == connectTask)
            {
                await connectTask;

                TcpClient = tcpClient;
                SendMessage(Name); // Handshake

                if (Connected)
                {
                    ConnectionSucceded?.Invoke(RecieveHandshake());

                    new Thread(MessageReceiveLoop).Start();
                    new Thread(SendConnectionVerificationMessageLoop).Start();
                    return true;
                }
            }

            tcpClient.Close();
        }
        catch (Exception ex)
        {
            ConnectionFailed?.Invoke("Connection failed after trying.");
        }

        return false;
    }


    public bool IsMessagePending()
    {
        if (Connected)
            return TcpClient.GetStream().DataAvailable;
        return false;
    }

    private void MessageReceiveLoop()
    {
        while (Connected)
        {
            if (IsMessagePending())
                RecieveMessage();
            Thread.Sleep(3);  // To note
        }
    }

    private void RecieveMessage()
    {
        var stream = TcpClient.GetStream();

        byte[] bufferSize = new byte[2];
        stream.Read(bufferSize, 0, bufferSize.Length);

        if (bufferSize[0] == 0 && bufferSize[1] == 0)
            return;

        byte[] buffer = new byte[BitConverter.ToUInt16(bufferSize)];
        stream.Read(buffer, 0, buffer.Length);

        MessageCame?.Invoke(new Message(buffer));
    }
    private Message RecieveHandshake()
    {
        var stream = TcpClient.GetStream();

        byte[] bufferSize = new byte[2];
        stream.Read(bufferSize, 0, bufferSize.Length);

        byte[] buffer = new byte[BitConverter.ToUInt16(bufferSize)];
        stream.Read(buffer, 0, buffer.Length);

        return new Message(buffer);
    }

    public void SendMessage(string text)
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
            TcpClient.Close();
            Disconnected?.Invoke();
        }
    }

    private void SendConnectionVerificationMessageLoop()
    {
        while (Connected)
        {
            SendMessage(string.Empty);
            Thread.Sleep(100);
        }
    }
}
