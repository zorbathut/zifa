
public class ZifaConfigDef : Def.Def
{
    public string[] retainers;
}

[Def.StaticReferences]
public static class ZifaConfigDefs
{
    static ZifaConfigDefs() { Def.StaticReferencesAttribute.Initialized(); }

    public static ZifaConfigDef Global;
}
