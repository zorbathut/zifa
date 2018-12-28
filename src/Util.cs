
using System.IO;
using System.Net;

public static class Util
{
    public static string GetURLContents(string url)
    {
        var request = WebRequest.Create(url);
        var response = request.GetResponse();
        var stream = response.GetResponseStream();
        var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
