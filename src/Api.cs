
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

        // Avoid the ten-query-per-second limit
        Thread.Sleep(120);

        string paramlist = parameters.Concat(KeyDict).Select(kvp => $"{kvp.Key}={kvp.Value}").Aggregate((lhs, rhs) => $"{lhs}&{rhs}");
        string url = $"https://xivapi.com{path}?{paramlist}"; // missing / between xivapi.com and path is because xivapi really likes its prefixed /'s
        Dbg.Inf($"Querying {url}");
        return JObject.Parse(Util.GetURLContents(url));
    }

    
    public class Element
    {
        public int price;
        public int count;
    }
    public static int EstimateValue(int id)
    {
        var results = Retrieve($"/market/midgardsormr/items/{id}/history", new Dictionary<string, string>() { { "columns", "History.*.PricePerUnit,History.*.Quantity" } });

        // find the median
        var elements = new List<Element>();
        foreach (var item in results["History"].OfType<JObject>())
        {
            elements.Add(new Element() { price = item["PricePerUnit"].Value<int>(), count = item["Quantity"].Value<int>() });
        }
        elements.Sort((lhs, rhs) => lhs.price < rhs.price);

        if (elements.Count == 0)
        {
            // okay then
            return 0;
        }

        while (elements.Count > 1)
        {
            int mid = (elements[0].price + elements[elements.Count - 1].price) / 2;
            int removal = Math.Min(elements[0].count, elements[elements.Count - 1].count);

            elements[0].count -= removal;
            elements[elements.Count - 1].count -= removal;

            if (elements[0].count == 0)
            {
                elements.RemoveAt(0);
            }
            if (elements[elements.Count - 1].count == 0)
            {
                elements.RemoveAt(elements.Count - 1);
            }

            if (elements.Count == 0)
            {
                // whoops
                return mid;
            }
        }

        return elements[0].price;
    }
}
