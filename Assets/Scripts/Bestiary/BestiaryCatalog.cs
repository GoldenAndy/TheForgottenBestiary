using System;
using UnityEngine;

public enum BestiarySpecies { Murcielago, Sapo, Aguila }

[Serializable]
public sealed class BestiaryEntry
{
    public BestiarySpecies species;
    public string speciesName;
    public string epithet;
    [Min(1)] public int defeatsRequired = 1;
    public Sprite portrait;
    public Sprite[] portraitFrames;
    public string habitat;
    [TextArea(2, 5)] public string description;
    [TextArea(2, 5)] public string fieldNotes;
    [TextArea(1, 3)] public string grandfatherNote;
}

[CreateAssetMenu(menuName = "The Forgotten Bestiary/Catalogo", fileName = "BestiaryCatalog")]
public sealed class BestiaryCatalog : ScriptableObject
{
    [TextArea(8, 15)] public string openingLetter;
    public BestiaryEntry[] entries = Array.Empty<BestiaryEntry>();

    public BestiaryEntry Find(BestiarySpecies species)
    {
        foreach (var entry in entries)
            if (entry != null && entry.species == species) return entry;
        return null;
    }
}
