
using Newtonsoft.Json;
using System.Collections.Generic;

public static class JsonCache
{
    public static Dictionary<string, object> s_Cache = new Dictionary<string, object>();

    public static T Retrieve<T>(string input) where T : class
    {
        var result = s_Cache.TryGetValue(input) as T;

        if (result == null)
        {
            result = JsonConvert.DeserializeObject<T>(input);

            s_Cache[input] = result;

            if (s_Cache.Count >= 100000)
            {
                // Cache is getting full. Guess we should come up with a nice elegant way to remove the least-recently-seen things!
                s_Cache.Clear(); // WHOOPS MY FINGER SLIPPED
                Dbg.Wrn("Cleaing cache . . .");
            }
        }

        return result;
    }
}
