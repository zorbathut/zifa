
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
        Db.Init();

        //DoGCScripAnalysis();
        DoTomestoneAnalysis();
        //DoRecipeAnalysis("weaver", 16);
        //GatheringCalculator.ProcessLongterm(69, 0, 500, 6, false);
        //CraftingCalculator.Process();
    }

    public static void DoGCScripAnalysis()
    {
        var results = new List<Result>();
        var inspected = new HashSet<int>();
        foreach (var scripEntry in Db.GetSheet2<SaintCoinach.Xiv.GCScripShopItem>())
        {
            var item = scripEntry.Item;

            if (item == null || item.Key == 0)
            {
                continue;
            }

            if (item.IsUntradable)
            {
                continue;
            }
            
            int id = item.Key;

            if (!inspected.Contains(id))
            {
                inspected.Add(id);

                float gps = (float)Commerce.ValueSell(id, false) / scripEntry.GCSealsCost;

                results.Add(new Result() { gps = gps, name = item.Name });
            }
        }

        results.Sort((lhs, rhs) => lhs.gps < rhs.gps);

        foreach (var result in results)
        {
            Dbg.Inf($"{result.gps:F2}: {result.name}");
        }
    }

    public static void DoTomestoneAnalysis()
    {
        var results = new List<Result>();
        var inspected = new HashSet<int>();
        foreach (var shop in Db.GetSheet<SaintCoinach.Xiv.SpecialShop>())
        {
            foreach (var listing in shop.Items)
            {
                int tomestones = 0;
                foreach (var cost in listing.Costs)
                {
                    if (cost.Item == null || cost.Item.Key != 28)   // Poetics; break if it's anything else
                    {
                        tomestones = -1;
                        break;
                    }

                    tomestones = cost.Count;
                }

                if (tomestones <= 0)
                {
                    continue;
                }

                float value = 0;
                string label = "";
                foreach (var reward in listing.Rewards)
                {
                    if (reward.Item.Key == 0 || reward.Item.IsUntradable)
                    {
                        continue;
                    }

                    value += Commerce.ValueSell(reward.Item.Key, reward.IsHq) * reward.Count / Commerce.MarketProfitDelayQuotient(reward.Item.Key);
                    label += $"{reward.Item.Name}x{reward.Count}{(reward.IsHq ? "HQ" : "")} ";
                }

                results.Add(new Result() { gps = value / tomestones, name = label });
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
                    readable += "\n" + $"  {Db.Item(itemid).Name}: buy from {source} for {cost:F0}x{itemamount}";

                    tcost += itemamount * cost;
                }
            }

            float profit = expectedRevenue - tcost;
            float profitPerTime = profit > 0 ? profit / Commerce.MarketProfitDelayQuotient(itemId) : profit;
            readable += "\n" + $"  Total cost: {tcost:F0}, total profit {profit:F0}, time-adjusted profit {profitPerTime:F0}";

            results.Add(new Tuple<float, string>(classLevel, readable));
        }

        foreach (var result in results.OrderByDescending(result => result.Item1))
        {
            Dbg.Inf(result.Item2);
        }
    }
}
