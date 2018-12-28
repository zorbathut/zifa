
using System.Collections.Generic;
using System.IO;
using System.Net;

public static class Api
{
    private static string Key;

    public static void Init()
    {
        Key = File.ReadAllText(@"../../../../appkey.txt");
    }

    public static string Retrieve(string path)
    {
        return Util.GetURLContents($"https://xivapi.com/{path}?key={Key}");
    }
}
