
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

public static class Commerce
{
    public static int ValueMarket(int id, bool hq)
    {
        var results = Api.Retrieve($"/market/midgardsormr/items/{id}/history", new Dictionary<string, string>() { { "columns", "History.*.PricePerUnit,History.*.Quantity,History.*.IsHQ" } });

        var history = results["History"].OfType<JObject>();

        Util.Element builder(JObject item) => new Util.Element { price = item["PricePerUnit"].Value<int>(), count = item["Quantity"].Value<int>() };

        int lqp = history.Where(item => item["IsHQ"].Value<bool>() == false).Select(builder).Median();
        int hqp = history.Where(item => item["IsHQ"].Value<bool>() == true).Select(builder).Median();

        // If we have no data for either set, give it the other's data
        if (lqp == 0)
        {
            lqp = hqp;
        }
        if (hqp == 0)
        {
            hqp = lqp;
        }

        // If LQP is more expensive than HQP, return the unfiltered results
        if (lqp > hqp)
        {
            return history.Select(builder).Median();
        }

        return hq ? hqp : lqp;
    }
}
