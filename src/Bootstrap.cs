
public class Bootstrap
{
    public static void Main(string[] args)
    {
        Api.Init();

        Dbg.Inf(Api.Retrieve("GCScripShopItem"));
    }
}
