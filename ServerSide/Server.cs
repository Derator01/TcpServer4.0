using System.Net.Sockets;

namespace TcpServer.ServerSide;

public class Server
{
    public string Name;

    public bool IsRunning { get; set; }

    private TcpListener _listener;
    public int MaxAllawedConnections = 100;
    private readonly List<Client> _clients = new();

    #region Events
    public delegate void OnClientConnected(string client);
    public event OnClientConnected? ClientConnected;

    public delegate void OnMessageCame(string client, string message);
    public event OnMessageCame? MessageCame;

    public delegate void OnClientDisconnected(string client);
    public event OnClientDisconnected? ClientDisconnected;
    #endregion

    public Server(string name, int port = 7000)
    {
        Name = name;
        _listener = new TcpListener(System.Net.IPAddress.Any, port);
    }

    public void Start()
    {
        _listener.Start();
        IsRunning = true;

        new Thread(ListeningLoop).Start();
        new Thread(MessageRecieveLoop).Start();
    }

    public void Stop()
    {
        _listener.Stop();
        IsRunning = false;
    }

    private void ListeningLoop()
    {
        while (IsRunning)
        {
            if (_clients.Count == MaxAllawedConnections)
                return;

            Client client = new(_listener.AcceptTcpClient());

            //StartHandshakeTimer(client);

            Thread.Sleep(5);

            if (client.IsMessagePending())
            {
                client.Name = client.RecieveMessage();
                _clients.Add(client);
                ClientConnected?.Invoke(client.Name);

                client.SendMessage(Name); // handshake
            }
            else
                client.Dispose();
        }
    }

    private void StartHandshakeTimer(Client client)
    {
        new Timer((s) => { if (string.IsNullOrEmpty(client.Name)) client.Close(); else _clients.Add(client); }, null, 10, Timeout.Infinite);
    }

    private void MessageRecieveLoop()
    {
        while (IsRunning)
        {
            for (int i = 0; i < _clients.Count; i++)
            {
                Client? client = _clients[i];

                client?.SendMessage(string.Empty);

                if (client is null)
                {
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

    private void RecieveMessageFrom(Client client)
    {
        if (!client.Connected)
            return;

        Common.Message message = client.RecieveMessage();

        if (string.IsNullOrEmpty(message))
            return;

        MessageCame?.Invoke(client.Name, message);
    }

    public void SendMessage(string message)
    {
        for (int i = 0; i < _clients.Count; i++)
        {
            var client = _clients[i];

            client?.SendMessage(message);
        }
    }
    public void SendMessage(string name, string message)
    {
        var client = _clients.FirstOrDefault(c => c.Name == name);

        client?.SendMessage(message);
    }

    public void Disconnect()
    {
        for (int i = 0; i < _clients.Count; i++)
        {
            var client = _clients[i];

            client?.Close();

            ClientDisconnected?.Invoke(client.Name);
        }
        //_clients.Clear();
    }
    public void Disconnect(string clientName)
    {
        var client = _clients.FirstOrDefault(x => x.Name == clientName);

        if (client is not null)
            Disconnect(client);
    }
    private void Disconnect(Client client)
    {
        ClientDisconnected?.Invoke(client.Name);

        _clients.Remove(client);
        client?.Close();
    }
}