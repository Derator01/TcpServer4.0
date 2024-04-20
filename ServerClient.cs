using System.Net;
using TcpServer.Common;
using TcpServer.ServerSide;

namespace TcpServer;

public class ServerClient
{
    public bool IsRunning { get; private set; }

    private ClientSide.Client _client;
    private Server _server;

    private Random _random = new();

    private int _randomInt;

    private string _name;
    private IPAddress _ip;
    private int _port;

    public delegate void OnConnected();
    public event OnConnected Connected;

    public ServerClient(string name, int port = 7000)
    {
        _name = name;
        _port = port;

        _client = new(name, port);
        _server = new(name, port);

        _client.ConnectionSucceded += (n) => _client.SendMessage(_randomInt.ToString());
        _server.MessageCame += CheckConnection;

        _randomInt = _random.Next();
    }

    public ServerClient(string name, IPAddress ip, int port = 7000)
    {
        _name = name;
        _ip = ip;
        _port = port;

        _client = new(name, ip, port);
        _server = new(name, port);

        _client.ConnectionSucceded += (n) => _client.SendMessage(_randomInt.ToString());
        _server.MessageCame += CheckConnection;

        _randomInt = _random.Next();
    }

    private void CheckConnection(string name, Message message)
    {
        if (message == _randomInt.ToString())
        {
            if (_ip == null)
            {
                _client.Dispose();
                _client = new(_name, _port);
            }
            else
            {
                _client.Dispose();
                _client = new(_name, _ip, _port);
            }
        }
        else
        {
            _server.MessageCame -= CheckConnection;

            if (_randomInt > int.Parse(message))
                _client.Dispose();
            else
                _server.Stop();


        }
    }

    public async Task StartAsync()
    {
        if (IsRunning)
            return;

        _server.Start();
        await _client.ConnectAsync();

        IsRunning = true;
    }

    public void Stop()
    {
        if (!IsRunning)
            return;

        IsRunning = false;
    }
}
