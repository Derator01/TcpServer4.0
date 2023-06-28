using System.Text;

public static class MessageHandling
{
    public static string RemoveZeros(this string self)
    {
        for (int i = 0; i < self.Length; i++)

            if (self[i] == '\0')
                return self.Remove(i);
        return self;
    }

    public static byte[] ToBytes(this string text)
    {
        return Encoding.UTF8.GetBytes(text);
    }
}