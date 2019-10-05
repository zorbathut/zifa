
using System;
using System.Data.SQLite;

public static class Cache
{
    private static SQLiteConnection DbConnection;

    public static void Init()
    {
        DbConnection = new SQLiteConnection("Data Source=cache.sqlite");
        DbConnection.Open();

        DbConnection.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS cache (key TEXT PRIMARY KEY, time INTEGER NOT NULL, value TEXT NOT NULL)");
    }

    public static string GetCacheEntry(string key, TimeSpan invalidation, out DateTimeOffset retrievalTime)
    {
        retrievalTime = DateTimeOffset.MinValue;
        if (invalidation.TotalSeconds <= 0)
        {
            // no cache allowed
            return null;
        }

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
        if (cacheage > invalidation)
        {
            return null;
        }

        // We're good, return the cache!
        retrievalTime = cachetimestamp;
        return reader.GetField<string>("value");
    }

    public static string GetCacheEntry(string key, TimeSpan invalidation)
    {
        DateTimeOffset _;
        return GetCacheEntry(key, invalidation, out _);
    }

    public static void StoreCacheEntry(string key, string value)
    {
        var cmd = new SQLiteCommand("INSERT OR REPLACE INTO cache (key, time, value) VALUES (@key, @time, @value)", DbConnection);
        cmd.Parameters.AddWithValue("@key", key);
        cmd.Parameters.AddWithValue("@time", DateTimeOffset.Now.ToUnixTimeSeconds());
        cmd.Parameters.AddWithValue("@value", value);
        cmd.ExecuteNonQuery();
    }
}
