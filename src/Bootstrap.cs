
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

    public class CollectibleCombination
    {
        public string item;
        public int xp;
    }

    public static void Main(string[] args)
    {
        Cache.Init();
        Api.Init();
        Db.Init();

        //DoGCScripAnalysis();
        //DoPurchasableAnalysis(Db.Item("Red Gatherers' Scrip").Key);
        //DoRecipeAnalysis("weaver", 1, 60, 55);
        //GatheringCalculator.ProcessLongterm(81, 7, 500, 4, true);
        //CraftingCalculator.Process();
        /*
        DoCollectibleCombinationMath(new CollectibleCombination[] {
            new CollectibleCombination() { item = "Holy Rainbow Shoes", xp = 168480 },
            new CollectibleCombination() { item = "Holy Rainbow Shirt of Scouting", xp = 116640 },
            new CollectibleCombination() { item = "Rainbow Sash of Healing", xp = 79380 },
        });*/
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

    public static void DoPurchasableAnalysis(int itemId)
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
                    if (cost.Item == null || cost.Item.Key != itemId)
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

    public static Tuple<float, string> EvaluateItem(SaintCoinach.Xiv.Recipe recipe, SaintCoinach.Xiv.Item result, bool hq)
    {
        float expectedRevenue = Commerce.ValueSell(result.Key, hq);
        string readable = $"{recipe.ClassJob.Name} {recipe.ResultItem.Name} {(hq ? "HQ" : "NQ")} ({recipe.ResultItem.Key}): expected revenue {Commerce.ValueSell(result.Key, hq):F0}";
        float tcost = 0;
        foreach (var ingredient in recipe.Ingredients)
        {
            float cost = Commerce.ValueBuy(ingredient.Item.Key, false, Commerce.TransactionType.Immediate, out string source);
            readable += "\n" + $"  {ingredient.Item.Name}: buy from {source} for {cost:F0}x{ingredient.Count}";

            tcost += ingredient.Count * cost;
        }

        float profit = expectedRevenue - tcost;
        float profitTimeAdjusted;

        if (profit > 0)
        {
            // Modify weighting for profitable things that sell slowly
            float delay = Commerce.MarketProfitDelayQuotient(result.Key);
            if (hq)
            {
                // HQing things is hard; ramp the delay way up
                delay = Math.Max(delay, 0.2f);
            }

            profitTimeAdjusted = profit / delay;
        }
        else
        {
            profitTimeAdjusted = profit;
        }
        
        readable += "\n" + $"  Total cost: {tcost:F0}, total profit {profit:F0}, time-adjusted profit {profitTimeAdjusted:F0}";
        readable += "\n";

        return new Tuple<float, string>(profitTimeAdjusted, readable);
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
            if (recipe.ClassJob.Name != classid || classLevel < levelmin || classLevel >= levelmax)
            {
                continue;
            }

            // filter out ixal
            if (recipe.RequiredItem.Key != 0)
            {
                continue;
            }

            if (classLevel <= hqcutoff && result.CanBeHq)
            {
                evaluator.Add(() => results.Add(EvaluateItem(recipe, result, true)));
            }

            evaluator.Add(() => results.Add(EvaluateItem(recipe, result, false)));
        }

        for (int i = 0; i < evaluator.Count; ++i)
        {
            Dbg.Inf($"{i} / {evaluator.Count}");
            evaluator[i]();
        }

        foreach (var result in results.OrderBy(result => result.Item1))
        {
            Dbg.Inf(result.Item2);
        }
    }

    public static void DoCollectibleCombinationMath(CollectibleCombination[] combos)
    {
        foreach (var combo in combos)
        {
            var recipe = Db.Recipe(Db.Item(combo.item));

            string readable = $"{combo.item}:";
            float tcost = 0;
            foreach (var ingredient in recipe.Ingredients)
            {
                float cost = Commerce.ValueBuy(ingredient.Item.Key, false, Commerce.TransactionType.Immediate, out string source);
                readable += "\n" + $"  {ingredient.Item.Name}: buy from {source} for {cost:F0}x{ingredient.Count}";

                tcost += ingredient.Count * cost;
            }
            readable += "\n" + $"  Total cost {tcost}, total xp per gil: {combo.xp / tcost:F2}";

            Dbg.Inf(readable);
        }
    }
}
