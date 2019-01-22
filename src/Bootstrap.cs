
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

    public static void DoRecipeAnalysis(string classid, int levelmin, int hqcutoff, int levelmax)
    {
        var results = new List<Tuple<float, string>>();

        var evaluator = new List<Action>();

        foreach (var recipe in Db.GetSheet<SaintCoinach.Xiv.Recipe>())
        {
            var result = recipe.ResultItem;
            int resultId = result.Key;
            
            if (resultId == 0)
            {
                continue;
            }

            string className = recipe.ClassJob.Name;
            int classLevel = recipe.RecipeLevelTable.ClassJobLevel;

            // we gotta do more, man
            if (className != classid || classLevel < levelmin || classLevel >= levelmax)
            {
                continue;
            }

            // filter out ixal
            if (recipe.RequiredItem.Key != 0)
            {
                continue;
            }

            evaluator.Add(() =>
            {
                float expectedRevenue = Commerce.ValueSell(result.Key, classLevel <= hqcutoff);
                string readable = $"{recipe.ResultItem.Name} ({recipe.ResultItem.Key}): {className} {classLevel}, expected revenue {Commerce.ValueSell(resultId, false):F0}/{Commerce.ValueSell(resultId, true):F0}";
                float tcost = 0;
                foreach (var ingredient in recipe.Ingredients)
                {
                    float cost = Commerce.ValueBuy(ingredient.Item.Key, false, out string source);
                    readable += "\n" + $"  {ingredient.Item.Name}: buy from {source} for {cost:F0}x{ingredient.Count}";

                    tcost += ingredient.Count * cost;
                }

                float profit = expectedRevenue - tcost;
                float profitPerTime = profit > 0 ? profit / Commerce.MarketProfitDelayQuotient(resultId) : profit;
                readable += "\n" + $"  Total cost: {tcost:F0}, total profit {profit:F0}, time-adjusted profit {profitPerTime:F0}";

                results.Add(new Tuple<float, string>(profitPerTime, readable));
            });
        }

        for (int i = 0; i < evaluator.Count; ++i)
        {
            Dbg.Inf($"{i} / {evaluator.Count}");
            evaluator[i]();
        }

        foreach (var result in results.OrderByDescending(result => result.Item1))
        {
            Dbg.Inf(result.Item2);
        }
    }
}
