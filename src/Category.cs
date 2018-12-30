
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
            regex = new Regex("^/item/.*$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            cacheExpiry = new TimeSpan(1000, 0, 0, 0, 0),
            columns = "PriceLow,PriceMid,GameContentLinks.GilShopItem,Name,IsUntradable",
        },
        new CategoryInfo
        {
            regex = new Regex("^/market/.*/history$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            cacheExpiry = new TimeSpan(0, 60, 0),
            columns = "History.*.PricePerUnit,History.*.Quantity,History.*.IsHQ",
        },
        new CategoryInfo
        {
            regex = new Regex("^/recipe$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            cacheExpiry = new TimeSpan(1000, 0, 0, 0, 0),
            columns = "Url",
        },
        new CategoryInfo
        {
            regex = new Regex("^/recipe/.*$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            cacheExpiry = new TimeSpan(1000, 0, 0, 0, 0),
            columns = "AmountIngredient0,AmountIngredient1,AmountIngredient2,AmountIngredient3,AmountIngredient4,AmountIngredient5,AmountIngredient6,AmountIngredient7,AmountIngredient8,AmountIngredient9,ItemIngredient0TargetID,ItemIngredient1TargetID,ItemIngredient2TargetID,ItemIngredient3TargetID,ItemIngredient4TargetID,ItemIngredient5TargetID,ItemIngredient6TargetID,ItemIngredient7TargetID,ItemIngredient8TargetID,ItemIngredient9TargetID,ClassJob.Name,RecipeLevelTable.ClassJobLevel,ItemResultTargetID,Name",
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
