
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public static class Category
{
    public class CategoryInfo
    {
        public Regex regex;
        public TimeSpan cacheExpiry;
        public string columns;
    }

    private static readonly List<CategoryInfo> CategoryData = new List<CategoryInfo>()
    {
        new CategoryInfo
        {
            regex = new Regex("^/gcscripshopitem$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            cacheExpiry = new TimeSpan(1000, 0, 0, 0, 0),
            columns = "Url",
        },
        new CategoryInfo
        {
            regex = new Regex("^/gcscripshopitem/.*$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            cacheExpiry = new TimeSpan(1000, 0, 0, 0, 0),
            columns = "CostGCSeals,Item.IsUntradable,Item.ID,Item.Name",
        },
        new CategoryInfo
        {
            regex = new Regex("^/market/.*$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            cacheExpiry = new TimeSpan(0, 60, 0),
            columns = "History.*.PricePerUnit,History.*.Quantity,History.*.IsHQ",
        },
    };
    private static readonly CategoryInfo CategoryFallback = new CategoryInfo() { cacheExpiry = new TimeSpan(0, 10, 0) };

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
