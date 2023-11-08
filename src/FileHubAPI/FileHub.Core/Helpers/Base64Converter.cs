using System.Text;

namespace FileHub.Core.Helpers;

public static class Base64Converter
{
    public static string EncodeToBase64(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return Convert.ToBase64String(bytes);
    }

    public static string DecodeFromBase64(string base64String)
    {
        var bytes = Convert.FromBase64String(base64String);
        return Encoding.UTF8.GetString(bytes);
    }
}