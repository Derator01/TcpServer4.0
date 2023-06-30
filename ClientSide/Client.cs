using System.Net;
using System.Net.Sockets;
using TcpServer.Common;

namespace TcpServer.ClientSide;

public class Client
{
    public const int PACKET_SIZE = 1024;

    public string Name { get; private set; }

    public bool Connected { get => TcpClient.Connected; }

    private TcpClient TcpClient { get; set; }
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
    /// With this constructor client will search through all local ips.
    /// </summary>
    /// <param name="name">Name of the client that will be known to server</param>
    public Client(string name, int port = 7000)
    {
        Name = name;
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
        if (EndPoint is null)
        {
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());

            foreach (IPAddress localIP in localIPs)
            {
                if (await TryConnectAsync(new IPEndPoint(localIP, 7000)))
                    return true;
            }

            ConnectionFailed?.Invoke("Connection failed for all local IP addresses.");
            return false;
        }
        return await TryConnectAsync(EndPoint);
    }

    private async Task<bool> TryConnectAsync(IPEndPoint endPoint)
    {
        TcpClient = new TcpClient();

        int maxRetryAttempts = 3;
        int retryDelayMilliseconds = 100;

        for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
        {
            try
            {
                Task connectTask = TcpClient.ConnectAsync(endPoint);
                Task completedTask = await Task.WhenAny(connectTask, Task.Delay(retryDelayMilliseconds));

                if (completedTask == connectTask)
                {
                    await connectTask;
                    SendMessage(Name); // Handshake

                    if (Connected)
                    {
                        ConnectionSucceded?.Invoke(RecieveHandshake());

                        new Thread(MessageReceiveLoop).Start();
                        new Thread(SendConnectionVerificationMessageLoop).Start();
                        return true; // Connection established successfully
                    }
                }
                else
                {
                    TcpClient.Close();
                    break;
                }
            }
            catch (Exception ex)
            {
                ConnectionFailed?.Invoke($"Connection attempt {attempt} failed: {ex.Message}");
            }
        }
        ConnectionFailed?.Invoke("Connection failed after all retry attempts.");

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
        //Disconnected?.Invoke();
    }

    private void RecieveMessage()
    {
        var stream = TcpClient.GetStream();

        byte[] bufferSize = new byte[2];
        stream.Read(bufferSize, 0, bufferSize.Length);
        //Array.Reverse(bufferSize);

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
        //Array.Reverse(bufferSize);

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

            buffer[0] = (byte)(bytes.Length >> 8);
            buffer[1] = (byte)bytes.Length;

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
