using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class BestiaryBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInitialScene() { EnsureJournal(); }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) { EnsureJournal(); }

    private static void EnsureJournal()
    {
        if (Object.FindObjectOfType<PlayerMovementTest>() == null) return;
        if (Object.FindObjectOfType<BestiaryJournal>() == null)
        {
            var prefab = Resources.Load<GameObject>("Bestiary/BestiaryJournal");
            if (prefab == null) { Debug.LogError("No se encuentra Resources/Bestiary/BestiaryJournal."); return; }
            Object.Instantiate(prefab).name = "BestiaryJournal";
        }
        if (Object.FindObjectOfType<EventSystem>() == null)
            new GameObject("Bestiary_EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}
