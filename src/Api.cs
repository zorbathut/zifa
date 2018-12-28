
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

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

            if (!(pageData["Pagination"] as JObject).ContainsKey("PageNext"))
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
        string url = $"https://xivapi.com/{path}?{paramlist}";
        Dbg.Inf($"Querying {url}");
        return JObject.Parse(Util.GetURLContents(url));
    }
}
