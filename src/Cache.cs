
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text.RegularExpressions;

public static class Cache
{
    private static SQLiteConnection DbConnection;

    class CategoryInfo
    {
        public TimeSpan cacheExpiry;
    }

    public static void Init()
    {
        DbConnection = new SQLiteConnection("Data Source=../../../cache.sqlite");
        DbConnection.Open();

        DbConnection.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS cache (key TEXT PRIMARY KEY, time INTEGER NOT NULL, value TEXT NOT NULL)");
    }

    public static string GetCacheEntry(string key)
    {
        // We technically don't need this until later, but we keep it up here so we get appropriate angry messages if it fails
        var categoryInfo = GetKeyCategoryInfo(key);

        var cmd = new SQLiteCommand("SELECT time, value FROM cache WHERE key=@key", DbConnection);
        cmd.Parameters.AddWithValue("@key", key);
        var reader = cmd.ExecuteReader();

        if (!reader.Read())
        {
            return null;
        }

        // Check cache age
        var cachetimestamp = DateTimeOffset.FromUnixTimeSeconds(reader.GetField<long>("time"));
        var cacheage = DateTimeOffset.Now - cachetimestamp;
        if (cacheage > categoryInfo.cacheExpiry)
        {
            return null;
        }

        // We're good, return the cache!
        return reader.GetField<string>("value");
    }

    public static void StoreCacheEntry(string key, string value)
    {
        var cmd = new SQLiteCommand("INSERT OR REPLACE INTO cache (key, time, value) VALUES (@key, @time, @value)", DbConnection);
        cmd.Parameters.AddWithValue("@key", key);
        cmd.Parameters.AddWithValue("@time", DateTimeOffset.Now.ToUnixTimeSeconds());
        cmd.Parameters.AddWithValue("@value", value);
        cmd.ExecuteNonQuery();
    }

    private static readonly Dictionary<string, CategoryInfo> CategoryData = new Dictionary<string, CategoryInfo>()
    {
        { "market", new CategoryInfo() { cacheExpiry = new TimeSpan(0, 10, 0) } },
        { "gcscripshopitem", new CategoryInfo() { cacheExpiry = new TimeSpan(1000, 0, 0, 0, 0) } },
    };
    private static readonly CategoryInfo CategoryFallback = new CategoryInfo() { cacheExpiry = new TimeSpan(0, 10, 0) };
    private static CategoryInfo GetKeyCategoryInfo(string key)
    {
        string cat = GetKeyCategory(key);
        if (CategoryData.ContainsKey(cat))
        {
            return CategoryData[cat];
        }
        else
        {
            Dbg.Err($"Failed to get category data for {cat}");
            return CategoryFallback;
        }
    }

    private static readonly Regex Extractor = new Regex("https://xivapi.com/([^/?]+).*", RegexOptions.Compiled);
    private static string GetKeyCategory(string key)
    {
        return Extractor.Match(key).Groups[1].Value.ToLower();
    }
}
