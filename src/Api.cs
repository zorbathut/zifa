
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

public static class Api
{
    private static string Key;
    private static Dictionary<string, string> KeyDict;

    public static void Init()
    {
        Key = File.ReadAllText(@"../../../appkey.txt");
        KeyDict = new Dictionary<string, string>
        {
            ["key"] = Key
        };
    }

    public static IEnumerable<JObject> List(string path, Dictionary<string, string> parameters = null)
    {
        int page = 1;
        while (true)
        {
            var pageData = Retrieve(path, new Dictionary<string, string>() { { "page", page.ToString() }, { "columns", "Url" } });
            foreach (var item in pageData["Results"].OfType<JObject>())
            {
                yield return item;
            }

            if (pageData["Pagination"]["PageTotal"].Value<int>() == page)
            {
                break;
            }
            else
            {
                ++page;
            }
        }
    }

    public static JObject Retrieve(string path, Dictionary<string, string> parameters = null)
    {
        if (parameters == null)
        {
            parameters = new Dictionary<string, string>();
        }

        string paramlist = parameters.Concat(KeyDict).Select(kvp => $"{kvp.Key}={kvp.Value}").Aggregate((lhs, rhs) => $"{lhs}&{rhs}");
        string url = $"https://xivapi.com{path}?{paramlist}"; // missing / between xivapi.com and path is because xivapi really likes its prefixed /'s

        string result = Cache.GetCacheEntry(url);
        if (result == null)
        {
            // Avoid the ten-query-per-second limit
            Thread.Sleep(110);

            Dbg.Inf($"Querying {url}");
            result = Util.GetURLContents(url);
            Cache.StoreCacheEntry(url, result);
        }

        return JObject.Parse(result);
    }
}
