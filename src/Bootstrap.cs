
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

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

        //DoGCScripAnalysis();
        DoRecipeAnalysis("blacksmith", 1);
        //GatheringCalculator.ProcessLongterm(69, 0, 500, 6, false);
        //CraftingCalculator.Process();
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

    public static void DoRecipeAnalysis(string classid, int levelmin)
    {
        var results = new List<Tuple<float, string>>();

        foreach (var item in Api.List("/Recipe"))
        {
            var recipeUrl = item["Url"].ToString();
            var recipeData = Api.Retrieve(recipeUrl);

            string recipeName = recipeData["Name"].Value<string>();
            int itemId = recipeData["ItemResultTargetID"].Value<int>();

            if (itemId == 0)
            {
                continue;
            }

            string className = recipeData["ClassJob"]["Name"].Value<string>();
            int classLevel = recipeData["RecipeLevelTable"]["ClassJobLevel"].Value<int>();

            // we gotta do more, man
            if (className != classid || classLevel < levelmin || classLevel >= levelmin + 5)
            {
                continue;
            }

            // filter out ixal
            if (recipeData["ItemRequired"]["ID"].Type != JTokenType.Null)
            {
                continue;
            }

            float expectedRevenue = Commerce.ValueSell(itemId, false);
            string readable = $"{recipeName} ({itemId}): {className} {classLevel}, expected revenue {Commerce.ValueSell(itemId, false):F0}/{Commerce.ValueSell(itemId, true):F0}";
            float tcost = 0;
            for (int i = 0; i < 9; ++i)
            {
                int itemamount = recipeData[$"AmountIngredient{i}"].Value<int>();
                int itemid = recipeData[$"ItemIngredient{i}TargetID"].Value<int>();

                if (itemamount > 0)
                {
                    string source;
                    float cost = Commerce.ValueBuy(itemid, false, out source);
                    readable += "\n" + $"  {Db.Item(itemid).name}: buy from {source} for {cost:F0}x{itemamount}";

                    tcost += itemamount * cost;
                }
            }

            float profit = expectedRevenue - tcost;
            float profitPerTime = profit > 0 ? profit / Commerce.MarketProfitDelayQuotient(itemId) : profit;
            readable += "\n" + $"  Total cost: {tcost:F0}, total profit {profit:F0}, time-adjusted profit {profitPerTime:F0}";

            results.Add(new Tuple<float, string>(profitPerTime, readable));
        }

        foreach (var result in results.OrderByDescending(result => result.Item1))
        {
            Dbg.Inf(result.Item2);
        }
    }
}
