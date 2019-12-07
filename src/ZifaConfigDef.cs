
public class Retainer
{
    public string name;
    public string profession;
    public int level;
    public int skill;
}

public class Profession
{
    public string name;
    public int level;
    public int craftsmanship;
    public int control;
}

public class ZifaConfigDef : Def.Def
{
    public Retainer[] retainers;
    public Profession[] professions;
}

[Def.StaticReferences]
public static class ZifaConfigDefs
{
    static ZifaConfigDefs() { Def.StaticReferencesAttribute.Initialized(); }

    public static ZifaConfigDef Global;
}
