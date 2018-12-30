
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

public static class Commerce
{
    public static int ValueMarket(int id, bool hq)
    {
        // just get this out of the way first; it's a much much cheaper query
        var itemdb = Db.Item(id);
        if (itemdb.untradable)
        {
            return 0;
        }

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

    public static int ValueSell(int id, bool hq, out string destination)
    {
        var results = Api.Retrieve($"/item/{id}");

        int bestprice = results["PriceLow"].Value<int>();
        if (hq)
        {
            // This seems to be the right equation
            bestprice = (int)Math.Ceiling(bestprice * 1.1);
        }
        destination = "vendor";

        int market = (int)(ValueMarket(id, hq) * 0.95);
        if (market > 0 && market > bestprice)
        {
            bestprice = market;
            destination = "market";
        }

        return bestprice;
    }

    public static int ValueSell(int id, bool hq)
    {
        string _;
        return ValueSell(id, hq, out _);
    }

    public static int ValueBuy(int id, bool hq, out string source)
    {
        // can't buy HQ stuff from vendors
        if (hq)
        {
            source = "market";
            return (int)(ValueMarket(id, hq) * 1.05);
        }

        var results = Api.Retrieve($"/item/{id}");

        int bestprice = (int)(ValueMarket(id, hq) * 1.05);
        source = "market";

        if (results["GameContentLinks"]["GilShopItem"].Type != JTokenType.Null)
        {
            // "Can it be bought in a gil shop" seems to be the best way to handle this, I think.
            // Look for errors.
            int vendorprice = Math.Min(bestprice, results["PriceMid"].Value<int>());
            if (vendorprice <= bestprice)
            {
                bestprice = vendorprice;
                source = "vendor";
            }
        }

        return bestprice;
    }

    public static int ValueBuy(int id, bool hq)
    {
        string _;
        return ValueBuy(id, hq, out _);
    }

}
