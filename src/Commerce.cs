
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

public static class Commerce
{
    public class Element
    {
        public int price;
        public int count;
    }
    public static int ValueMarket(int id)
    {
        var results = Api.Retrieve($"/market/midgardsormr/items/{id}/history", new Dictionary<string, string>() { { "columns", "History.*.PricePerUnit,History.*.Quantity" } });

        // find the median
        var elements = new List<Element>();
        foreach (var item in results["History"].OfType<JObject>())
        {
            elements.Add(new Element() { price = item["PricePerUnit"].Value<int>(), count = item["Quantity"].Value<int>() });
        }
        elements.Sort((lhs, rhs) => lhs.price < rhs.price);

        if (elements.Count == 0)
        {
            // okay then
            return 0;
        }

        while (elements.Count > 1)
        {
            int mid = (elements[0].price + elements[elements.Count - 1].price) / 2;
            int removal = Math.Min(elements[0].count, elements[elements.Count - 1].count);

            elements[0].count -= removal;
            elements[elements.Count - 1].count -= removal;

            if (elements[0].count == 0)
            {
                elements.RemoveAt(0);
            }
            if (elements[elements.Count - 1].count == 0)
            {
                elements.RemoveAt(elements.Count - 1);
            }

            if (elements.Count == 0)
            {
                // whoops
                return mid;
            }
        }

        return elements[0].price;
    }
}
