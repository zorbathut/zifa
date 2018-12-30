
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

        //DoGCScripAnalysis();
        DoRecipeAnalysis("blacksmith", 1);
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
        foreach (var item in Api.List("/Recipe"))
        {
            var recipeData = Api.Retrieve(item["Url"].ToString());

            string recipeName = recipeData["Name"].Value<string>();
            int itemId = recipeData["ItemResultTargetID"].Value<int>();

            string className = recipeData["ClassJob"]["Name"].Value<string>();
            int classLevel = recipeData["RecipeLevelTable"]["ClassJobLevel"].Value<int>();

            // we gotta do more, man
            if (className != classid || classLevel < levelmin || classLevel >= levelmin + 5)
            {
                continue;
            }

            int expectedRevenue = Commerce.ValueSell(itemId, false);
            Dbg.Inf($"{recipeName} ({itemId}): {className} {classLevel}, expected revenue {Commerce.ValueSell(itemId, false)}/{Commerce.ValueSell(itemId, true)}");
            int tcost = 0;
            for (int i = 0; i < 9; ++i)
            {
                int itemamount = recipeData[$"AmountIngredient{i}"].Value<int>();
                int itemid = recipeData[$"ItemIngredient{i}TargetID"].Value<int>();

                if (itemamount > 0)
                {
                    string source;
                    int cost = Commerce.ValueBuy(itemid, false, out source);
                    Dbg.Inf($"  {Db.Item(itemid).name}: buy from {source} for {cost}x{itemamount}");

                    tcost += itemamount * cost;
                }
            }
            Dbg.Inf($"  Total cost: {tcost}, total profit {expectedRevenue - tcost}");
        }
    }
}
