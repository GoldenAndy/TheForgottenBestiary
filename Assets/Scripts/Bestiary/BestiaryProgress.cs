using System;
using System.Collections.Generic;
using UnityEngine;

public interface IBestiaryProgressStore
{
    int Read(string key);
    void Write(string key, int value);
    void Save();
}

public sealed class PlayerPrefsBestiaryStore : IBestiaryProgressStore
{
    public const string DefaultPrefix = "TFB.Bestiary.v1.";
    private readonly string prefix;
    public PlayerPrefsBestiaryStore(string prefix = DefaultPrefix) { this.prefix = prefix; }
    public int Read(string key) => PlayerPrefs.GetInt(prefix + key, 0);
    public void Write(string key, int value) => PlayerPrefs.SetInt(prefix + key, value);
    public void Save() => PlayerPrefs.Save();
}

/// <summary>Only confirmed defeats reach this service. Progress survives scene reloads and deaths.</summary>
public static class BestiaryProgress
{
    private static IBestiaryProgressStore store = new PlayerPrefsBestiaryStore();
    private static readonly Dictionary<BestiarySpecies, int> counts = new Dictionary<BestiarySpecies, int>();
    public static event Action<BestiarySpecies, int> DefeatRecorded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        store = new PlayerPrefsBestiaryStore();
        counts.Clear();
        DefeatRecorded = null;
    }

    // Explicit stable keys: renaming the display name does not lose saved progress.
    public static string SaveKey(BestiarySpecies species)
    {
        switch (species)
        {
            case BestiarySpecies.Murcielago: return "bat";
            case BestiarySpecies.Sapo: return "frog";
            case BestiarySpecies.Aguila: return "eagle";
            default: throw new ArgumentOutOfRangeException(nameof(species));
        }
    }

    public static int Count(BestiarySpecies species)
    {
        if (!counts.TryGetValue(species, out var count))
        {
            count = Mathf.Max(0, store.Read(SaveKey(species)));
            counts[species] = count;
        }
        return count;
    }

    public static bool IsUnlocked(BestiaryEntry entry) => entry != null && Count(entry.species) >= Mathf.Max(1, entry.defeatsRequired);

    public static int UnlockedCount(BestiaryCatalog catalog)
    {
        int result = 0;
        if (catalog != null)
            foreach (var entry in catalog.entries) if (IsUnlocked(entry)) result++;
        return result;
    }

    public static void RecordDefeat(BestiarySpecies species)
    {
        int previous = Count(species);
        int count = previous == int.MaxValue ? previous : previous + 1;
        counts[species] = count;
        store.Write(SaveKey(species), count);
        store.Save();
        DefeatRecorded?.Invoke(species, count);
    }

    /// <summary>Allows isolated validation and future save slots without changing the game's saved counts.</summary>
    public static void UseStore(IBestiaryProgressStore progressStore)
    {
        store = progressStore ?? throw new ArgumentNullException(nameof(progressStore));
        counts.Clear();
    }
}
