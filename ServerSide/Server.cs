using System.Net;
using System.Net.Sockets;
using TcpServer.Common;

namespace TcpServer.ServerSide;

public class Server
{
    public int MaxAllowedConnections = 100;

    public string Name;

    public bool IsRunning { get; set; }

    private TcpListener _listener;
    private readonly List<Client> _clients = new();

    #region Events
    public delegate void OnClientConnected(string client);
    public event OnClientConnected? ClientConnected;

    public delegate void OnMessageCame(string client, Message message);
    public event OnMessageCame? MessageCame;

    public delegate void OnClientDisconnected(string client);
    public event OnClientDisconnected? ClientDisconnected;
    #endregion

    public Server(string name, int port = 7000)
    {
        Name = name;
        _listener = new TcpListener(IPAddress.IPv6Any, port);
        _listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
    }

    public void Start()
    {
        if (IsRunning)
            return;

        _listener.Start();
        IsRunning = true;

        new Thread(ListeningLoop).Start();
        new Thread(MessageRecieveLoop).Start();
        new Thread(SendConnectionVerificationMessageLoop).Start();


    }

    private void ListeningLoop()
    {
        while (IsRunning)
        {
            if (_clients.Count == MaxAllowedConnections)
            {
                Thread.Sleep(100);
                continue;
            }

            Client client = new(_listener.AcceptTcpClient());

            client.Name = client.RecieveMessage(); // Handshake in
            client.SendMessage(Name); // Handshake out
            _clients.Add(client);

            ClientConnected?.Invoke(client.Name);
        }
    }

    private void MessageRecieveLoop()
    {
        while (IsRunning)
        {
            for (int i = 0; i < _clients.Count; i++)
            {
                Client? client = _clients[i];

                if (client is null)
                {
                    //throw new Exception("Client is suddenly null.");
                    break;
                }

                if (!client.Connected)
                {
                    Disconnect(client);
                    continue;
                }

                if (client.IsMessagePending())
                {
                    RecieveMessageFrom(client);
                }
            }
        }
    }

    private void SendConnectionVerificationMessageLoop()
    {
        while (IsRunning)
        {
            SendMessage(string.Empty);
            Thread.Sleep(100);
        }
    }

    private void RecieveMessageFrom(Client client)
    {
        if (!client.Connected)
            return;

        Message message = client.RecieveMessage();

        if (string.IsNullOrEmpty(message))
            return;

        MessageCame?.Invoke(client.Name, message);
    }

    public void SendMessage(byte[] bytes)
    {
        for (int i = 0; i < _clients.Count; i++)
        {
            var client = _clients[i];

            client?.SendMessage(bytes);
        }
    }
    public void SendMessage(string text)
    {
        for (int i = 0; i < _clients.Count; i++)
        {
            var client = _clients[i];

            client?.SendMessage(text);
        }
    }
    public void SendMessage(string name, string text)
    {
        var client = _clients.FirstOrDefault(c => c.Name == name);

        client?.SendMessage(text);
    }

    public void Disconnect()
    {
        for (int i = 0; i < _clients.Count; i++)
        {
            var client = _clients[i];

            client?.Close();

            ClientDisconnected?.Invoke(client.Name);
        }
        _clients.Clear();
    }
    public void Disconnect(string name)
    {
        var client = _clients.FirstOrDefault(x => x.Name == name);

        if (client is not null)
            Disconnect(client);
    }
    private void Disconnect(Client client)
    {
        ClientDisconnected?.Invoke(client.Name);

        _clients.Remove(client);
        client?.Close();
    }

    public void Stop()
    {
        IsRunning = false;

        foreach (var client in _clients)
            client.Dispose();
        _clients.Clear();

        _listener.Stop();
    }
}