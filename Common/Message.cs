using System.Text;

namespace TcpServer.Common;

public record Message
{
    public string Text { get; init; }
    public byte[] Data { get; init; }

    public Message(byte[] packet)
    {
        Text = Encoding.UTF8.GetString(packet, 0, packet.Length).RemoveZeros();
        Data = packet;
    }

    public Message(string rawMessage)
    {
        Text = rawMessage.RemoveZeros();
        Data = Encoding.UTF8.GetBytes(rawMessage);
    }

    public static implicit operator string(Message message)
    {
        return message.Text;
    }

    public static implicit operator byte[](Message message)
    {
        return message.Data;
    }
}

