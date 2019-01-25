
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public static class Category
{
    public class CategoryInfo
    {
        public Regex regex;
        public string columns;
    }

    private static readonly List<CategoryInfo> CategoryData = new List<CategoryInfo>()
    {
        new CategoryInfo
        {
            regex = new Regex("^/market/[a-z]+/items/[0-9]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            columns = "Prices.*.PricePerUnit,Prices.*.Quantity,Prices.*.IsHQ",
        },
        new CategoryInfo
        {
            regex = new Regex("^/market/[a-z]+/items/[0-9]+/history$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            columns = "History.*.PricePerUnit,History.*.Quantity,History.*.IsHQ,History.*.PurchaseDate",
        },
    };
    private static readonly CategoryInfo CategoryFallback = new CategoryInfo() { };

    private static readonly Regex Extractor = new Regex("^(/[^?]+)", RegexOptions.Compiled);
    private static string GetKeyCategory(string key)
    {
        return Extractor.Match(key).Groups[1].Value.ToLower();
    }

    public static CategoryInfo GetKeyCategoryInfo(string keyRaw)
    {
        string key = GetKeyCategory(keyRaw);

        var category = CategoryData.Where(entry => entry.regex.IsMatch(key)).FirstOrDefault();
        if (category == null)
        {
            Dbg.Err($"Failed to get category data for {key}");
            category = CategoryFallback;
        }

        return category;
    }
}
