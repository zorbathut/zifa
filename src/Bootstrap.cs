
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

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

    public enum SortMethod
    {
        Order,
        Profit,
    }

    public struct CraftingInfo
    {
        public string name;
        public int minlevel;
        public int maxhqlevel;
        public int maxlevel;
    }

    public static void Main(string[] args)
    {
        Cache.Init();
        Api.Init();
        Db.Init();

        {
            var parser = new Def.Parser();
            foreach (var file in new DirectoryInfo(@"../../../xml").GetFiles("*.xml"))
            {
                parser.AddString(File.ReadAllText(file.FullName), file.Name);
            }
            parser.Finish();
        }

        //DoGCScripAnalysis();
        if (true)
            DoRecipeAnalysis(new CraftingInfo[] {
                new CraftingInfo() { name = "carpenter", minlevel = 1, maxhqlevel = 15, maxlevel = 18 },
                new CraftingInfo() { name = "blacksmith", minlevel = 1, maxhqlevel = 18, maxlevel = 21 },
                new CraftingInfo() { name = "armorer", minlevel = 1, maxhqlevel = 16, maxlevel = 19 },
                new CraftingInfo() { name = "goldsmith", minlevel = 1, maxhqlevel = 18, maxlevel = 21 },
                new CraftingInfo() { name = "leatherworker", minlevel = 1, maxhqlevel = 19, maxlevel = 22 },
                new CraftingInfo() { name = "weaver", minlevel = 1, maxhqlevel = 60, maxlevel = 65 },
                new CraftingInfo() { name = "alchemist", minlevel = 1, maxhqlevel = 15, maxlevel = 18 },
                new CraftingInfo() { name = "culinarian", minlevel = 1, maxhqlevel = 16, maxlevel = 19 },
            }, SortMethod.Profit);
        //DoRecipeAnalysis("goldsmith", 1, 0, 5, SortMethod.Order);
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

                float gps = Commerce.MarketProfitAdjuster(Commerce.ValueSell(id, false, Market.Latency.Standard) / scripEntry.GCSealsCost, id, 40000 / scripEntry.GCSealsCost, Market.Latency.Standard);

                results.Add(new Result() { gps = gps, name = item.Name });
            }
        }

        results.Sort((lhs, rhs) => lhs.gps < rhs.gps);

        foreach (var result in results)
        {
            Dbg.Inf($"{result.gps:F2}: {result.name}");
        }
    }

    public static Tuple<float, string> EvaluateItem(SaintCoinach.Xiv.Recipe recipe, bool hq, Market.Latency latency)
    {
        var result = recipe.ResultItem;
        float expectedRevenue = Commerce.ValueSell(result.Key, hq, latency);
        string readable = $"{recipe.ClassJob.Name} {recipe.ResultItem.Name} {(hq ? "HQ" : "NQ")} ({recipe.ResultItem.Key}): expected revenue {Commerce.ValueSell(result.Key, hq, latency):F0}";
        float tcost = 0;
        foreach (var ingredient in recipe.Ingredients)
        {
            float cost = Commerce.ValueBuy(ingredient.Item.Key, false, Commerce.TransactionType.Immediate, latency, out string source);
            readable += "\n" + $"  {ingredient.Item.Name}: buy from {source} for {cost:F0}x{ingredient.Count}";

            tcost += ingredient.Count * cost;
        }

        float profit = expectedRevenue - tcost;

        // Adjust profit
        // HQing things is hard, assume we're willing to sell at most ten per day
        float profitTimeAdjusted = profit * Math.Min(Commerce.MarketSalesPerDay(result.Key, latency), Math.Min(result.StackSize, hq ? 10 : 99));
        
        readable += "\n" + $"  Total cost: {tcost:F0}, total profit {profit:F0}, time-adjusted profit {profitTimeAdjusted:F0}";
        readable += "\n";

        return new Tuple<float, string>(profitTimeAdjusted, readable);
    }

    private struct RecipeData
    {
        public Func<Market.Latency, Tuple<float, string>> evaluator;
        public float estimate;
    }

    public static void DoRecipeAnalysis(CraftingInfo[] craftingInfo, SortMethod sortMethod)
    {
        var evaluators = new List<Func<Market.Latency, Tuple<float, string>>>();

        foreach (var recipe in Db.GetSheet<SaintCoinach.Xiv.Recipe>())
        {
            var result = recipe.ResultItem;
            int resultId = result.Key;
            
            if (resultId == 0)
            {
                continue;
            }

            if (!result.IsMarketable())
            {
                continue;
            }
            
            // filter out ixal
            if (recipe.RequiredItem.Key != 0)
            {
                continue;
            }

            string className = recipe.ClassJob.Name;
            int classLevel = recipe.RecipeLevelTable.ClassJobLevel;

            bool canHq = false;

            {
                // validate availability and see if we're allowed to try HQ'ing it
                bool validated = false;
                foreach (var crafttype in craftingInfo)
                {
                    if (recipe.ClassJob.Name == crafttype.name && classLevel >= crafttype.minlevel && classLevel <= crafttype.maxlevel)
                    {
                        validated = true;
                        if (classLevel <= crafttype.maxhqlevel)
                        {
                            canHq = true;
                        }
                    }
                }

                if (!validated)
                {
                    continue;
                }
            }

            if (canHq && result.CanBeHq)
            {
                evaluators.Add(latency => EvaluateItem(recipe, true, latency));
            }

            evaluators.Add(latency => EvaluateItem(recipe, false, latency));
        }

        if (sortMethod == SortMethod.Order)
        {
            // ToArray forces it to be evaluated before printing so we don't interlace with debug output
            foreach (var output in evaluators.Select(item =>  item(Market.Latency.Standard).Item2).ToArray())
            {
                Dbg.Inf(output);
            }
        }
        else if (sortMethod == SortMethod.Profit)
        {
            var recipeInfo = new List<RecipeData>();

            for (int i = 0; i < evaluators.Count; ++i)
            {
                Dbg.Inf($"{i} / {evaluators.Count}");

                var result = evaluators[i](Market.Latency.Standard);
                recipeInfo.Add(new RecipeData() {evaluator = evaluators[i], estimate = result.Item1});
            }

            recipeInfo = recipeInfo.OrderBy(x => x.estimate).ToList();

            var goodRecipes = new List<RecipeData>();

            int desiredCount = 20;
            while (recipeInfo.Count > 0 && (goodRecipes.Count < desiredCount || goodRecipes[goodRecipes.Count - desiredCount].estimate < recipeInfo[recipeInfo.Count - 1].estimate))
            {
                Dbg.Inf($"Immediate-testing; at {goodRecipes.Count} recipes");

                var process = recipeInfo[recipeInfo.Count - 1];
                recipeInfo.RemoveAt(recipeInfo.Count - 1);

                process.estimate = process.evaluator(Market.Latency.Immediate).Item1;
                goodRecipes.Add(process);

                goodRecipes = goodRecipes.OrderBy(x => x.estimate).ToList();
            }

            foreach (var result in goodRecipes)
            {
                // Standard is safe here because we're already cached
                Dbg.Inf(result.evaluator(Market.Latency.Standard).Item2);
            }
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
                float cost = Commerce.ValueBuy(ingredient.Item.Key, false, Commerce.TransactionType.Immediate, Market.Latency.Standard, out string source);
                readable += "\n" + $"  {ingredient.Item.Name}: buy from {source} for {cost:F0}x{ingredient.Count}";

                tcost += ingredient.Count * cost;
            }
            readable += "\n" + $"  Total cost {tcost}, total xp per gil: {combo.xp / tcost:F2}";

            Dbg.Inf(readable);
        }
    }
}
