
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

public static class Bootstrap
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
        //DoRecipeAnalysis("weaver", 1, 60, 55);
        //GatheringCalculator.ProcessLongterm(81, 7, 500, 4, true);
        //CraftingCalculator.Process();
        /*
        DoCollectibleCombinationMath(new CollectibleCombination[] {
            new CollectibleCombination() { item = "Chimerical Felt Hose of Aiming", xp = 279936 },
            new CollectibleCombination() { item = "Chimerical Felt Klobuk of Healing", xp = 205632 },
            new CollectibleCombination() { item = "Hallowed Ramie Sash of Casting", xp = 155520 },
            new CollectibleCombination() { item = "Ramie Cloth", xp = 166320 },
            new CollectibleCombination() { item = "Holy Rainbow Shoes", xp = 140400 },
            new CollectibleCombination() { item = "Holy Rainbow Shirt of Scouting", xp = 116640 },
            new CollectibleCombination() { item = "Rainbow Sash of Healing", xp = 79380 },
        });
        */

        Prompt.Run();
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

                float gps = Commerce.MarketProfitAdjuster(Commerce.ValueSell(id, false) / scripEntry.GCSealsCost, id, 40000 / scripEntry.GCSealsCost);

                results.Add(new Result() { gps = gps, name = item.Name });
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

        // Adjust profit
        // HQing things is hard, assume we're willing to sell at most ten per day
        float profitTimeAdjusted = profit * Math.Min(Commerce.MarketSalesPerDay(result.Key), Math.Min(result.StackSize, hq ? 10 : 99));
        
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
