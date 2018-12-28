
public class Bootstrap
{
    public static void Main(string[] args)
    {
        Api.Init();

        foreach (var item in Api.List("GCScripShopItem"))
        {
            Dbg.Inf(item.ToString());
        }
    }
}
