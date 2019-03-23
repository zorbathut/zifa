
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

    private const string Prefix = "https://staging.xivapi.com";

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
            var pageData = Retrieve(path, TimeSpan.FromDays(30), new Dictionary<string, string>() { { "page", page.ToString() } });
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

    private static long NextQuery = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    public static JObject Retrieve(string path, TimeSpan invalidation, out DateTimeOffset retrievalTime, Dictionary<string, string> parameters = null)
    {
        retrievalTime = DateTimeOffset.MinValue;
        if (parameters == null)
        {
            parameters = new Dictionary<string, string>();
        }

        var categoryInfo = Category.GetKeyCategoryInfo(path);

        IEnumerable<KeyValuePair<string, string>> paramlist = parameters;

        paramlist = paramlist.Concat(new KeyValuePair<string, string>("private_key", Key));
        if (categoryInfo.columns != null && categoryInfo.columns != "")
        {
            paramlist = paramlist.Concat(new KeyValuePair<string, string>("columns", categoryInfo.columns));
        }

        string paramstr = paramlist.Select(kvp => $"{kvp.Key}={kvp.Value}").Aggregate((lhs, rhs) => $"{lhs}&{rhs}");
        string urlbody = $"{path}?{paramstr}";

        string result = Cache.GetCacheEntry(urlbody, invalidation, out retrievalTime);
        if (result == null)
        {
            // Avoid the ten-query-per-second limit
            while (DateTimeOffset.Now.ToUnixTimeMilliseconds() < NextQuery)
            {
                Thread.Sleep(0);
            }
            NextQuery += 120;

            string url = Prefix + urlbody;
            //Dbg.Inf($"Querying {url}");
            result = Util.GetURLContents(url);
            Cache.StoreCacheEntry(urlbody, result);
        }

        return JObject.Parse(result);
    }

    public static JObject Retrieve(string path, TimeSpan invalidation, Dictionary<string, string> parameters = null)
    {
        DateTimeOffset _;
        return Retrieve(path, invalidation, out _, parameters);
    }
}
