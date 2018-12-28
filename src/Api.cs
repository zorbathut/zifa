
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;

public static class Api
{
    private static string Key;

    public static void Init()
    {
        Key = File.ReadAllText(@"../../../appkey.txt");
    }

    public static JObject Retrieve(string path)
    {
        return JObject.Parse(Util.GetURLContents($"https://xivapi.com/{path}?key={Key}"));
    }
}
