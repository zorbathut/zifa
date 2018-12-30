
using Newtonsoft.Json.Linq;

public class Item
{
    public string name;
    public bool untradable;
}

public static class Db
{
    public static Item Item(int id)
    {
        var results = Api.Retrieve($"/item/{id}");

        return new Item
        {
            name = results["Name"].Value<string>(),
            untradable = results["IsUntradable"].Value<bool>(),
        };
    }
}
