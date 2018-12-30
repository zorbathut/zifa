
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

public static class Commerce
{
    public static int ValueMarket(int id, bool hq)
    {
        var results = Api.Retrieve($"/market/midgardsormr/items/{id}/history");

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

    public static int ValueSell(int id, bool hq)
    {
        var results = Api.Retrieve($"/item/{id}");

        int vendorprice = results["PriceLow"].Value<int>();
        if (hq)
        {
            // This seems to be the right equation
            vendorprice = (int)Math.Ceiling(vendorprice * 1.1);
        }

        return Math.Max(vendorprice, (int)(ValueMarket(id, hq) * 0.95));
    }

    public static int ValueBuy(int id, bool hq)
    {
        // can't buy HQ stuff from vendors
        if (hq)
        {
            return (int)(ValueMarket(id, hq) * 1.05);
        }

        var results = Api.Retrieve($"/item/{id}");

        int bestprice = (int)(ValueMarket(id, hq) * 1.05);

        if (results["GameContentLinks"]["GilShop"].Type != JTokenType.Null)
        {
            // "Can it be bought in a gil shop" seems to be the best way to handle this, I think.
            // Look for errors.
            bestprice = Math.Min(bestprice, results["PriceMid"].Value<int>());
        }

        return bestprice;
    }
}
