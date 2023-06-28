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
    public delegate void OnConnectedToServer();
    public event OnConnectedToServer? ConnectedToServer;

    public delegate void OnMessageCame(string message);
    public event OnMessageCame? MessageCame;

    public delegate void OnDisconnected();
    public event OnDisconnected? Disconnected;
    #endregion

    public Client(string name, long ip, int port)
    {
        Name = name;
        EndPoint = new IPEndPoint(ip, port);
    }
    public Client(string name, IPAddress ip, int port)
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
        TcpClient = new TcpClient();

        CancellationTokenSource cts = new();
        CancellationToken ct = cts.Token;

        TcpClient.ConnectAsync(EndPoint, ct);

        //Thread.Sleep(10);
        //if (!Connected)
        //{
        //    cts.Cancel();
        //    return false;
        //}

        SendMessage(Name); // HandShake

        if (Connected)
        {
            if (IsMessagePending())
                if (RecieveHandshake() != "")
                {
                    TcpClient.Close();
                    Disconnected?.Invoke();

                    return false;
                }

            new Thread(MessageRecieveLoop).Start();
            new Thread(SendConnectionVerificationMessageLoop).Start();

            ConnectedToServer?.Invoke();
        }
        return Connected;
    }

    public bool IsMessagePending()
    {
        if (Connected)
            return TcpClient.GetStream().DataAvailable;
        return false;
    }

    private void MessageRecieveLoop()
    {
        while (Connected)
        {
            if (IsMessagePending())
                RecieveMessage();
            Thread.Sleep(3);  // To note
        }
        Disconnected?.Invoke();
    }

    private void RecieveMessage()
    {
        byte[] buffer = new byte[PACKET_SIZE];
        TcpClient.GetStream().Read(buffer, 0, buffer.Length);


        Message message = new Message(buffer);

        if (string.IsNullOrEmpty(message))
            return;

        MessageCame?.Invoke(message);
    }
    private Message RecieveHandshake()
    {
        byte[] buffer = new byte[PACKET_SIZE];
        TcpClient.GetStream().Read(buffer, 0, buffer.Length);


        Message message = new Message(buffer);

        return message;
    }

    public void SendMessage(string text)
    {
        if (!Connected)
            return;
        try
        {
            TcpClient.GetStream().Write(text.ToBytes());
        }
        catch (IOException ex)
        {
            TcpClient.Close();
        }
    }

    public void SendConnectionVerificationMessageLoop()
    {
        while (Connected)
        {
            SendMessage(string.Empty);
            Thread.Sleep(3);
        }
    }
}
