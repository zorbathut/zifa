
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

public class Bootstrap
{
    public class Result
    {
        public float gps;
        public string name;
    }

    public static void Main(string[] args)
    {
        Cache.Init();
        Api.Init();

        DoGCScripAnalysis();
    }

    public static void DoGCScripAnalysis()
    {
        var results = new List<Result>();
        var inspected = new HashSet<int>();
        foreach (var item in Api.List("/GCScripShopItem"))
        {
            var itemData = Api.Retrieve(item["Url"].ToString());

            if (!itemData.ContainsKey("Item") || itemData["Item"]["ID"].Type == JTokenType.Null)
            {
                continue;
            }

            if (itemData["Item"]["IsUntradable"].Value<string>() == "1")
            {
                continue;
            }
            
            int id = itemData["Item"]["ID"].Value<int>();

            if (!inspected.Contains(id))
            {
                inspected.Add(id);

                var val = Commerce.ValueSell(id, false);
                var seals = itemData["CostGCSeals"].Value<int>();

                string name = itemData["Item"]["Name"].Value<string>();
                float gps = (float)val / seals;

                results.Add(new Result() { gps = gps, name = name });
            }
        }

        results.Sort((lhs, rhs) => lhs.gps < rhs.gps);

        foreach (var result in results)
        {
            Dbg.Inf($"{result.gps:F2}: {result.name}");
        }
    }
}
