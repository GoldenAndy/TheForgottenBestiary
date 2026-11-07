using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Play Mode checks use a separate store and disposable enemy fixtures. They never reset the player's diary.</summary>
[InitializeOnLoad]
public static class BestiaryValidation
{
    private const string PendingKey = "TFB.Bestiary.Validation.Pending";
    private const string ResultKey = "TFB.Bestiary.Validation.Result";
    private static string stagedReport;
    private static double finishAt;

    static BestiaryValidation() { EditorApplication.playModeStateChanged += OnPlayModeStateChanged; }

    public static void StartAutomatedSession()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Inicia la validación fuera de Play Mode.");
        SessionState.SetBool(PendingKey, true);
        SessionState.SetString(ResultKey, "RUNNING");
        EditorApplication.isPlaying = true;
    }

    public static string LastSessionResult => SessionState.GetString(ResultKey, "No validation run");

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
        EditorApplication.delayCall += () =>
        {
            try
            {
                stagedReport = Run();
                BeginPauseProbe();
                finishAt = EditorApplication.timeSinceStartup + 0.6;
                EditorApplication.update += FinishAutomatedSession;
            }
            catch (Exception error) { CompleteAutomatedSession("FAIL: " + error); }
        };
    }

    private static void FinishAutomatedSession()
    {
        if (EditorApplication.timeSinceStartup < finishAt) return;
        EditorApplication.update -= FinishAutomatedSession;
        try { CompleteAutomatedSession("PASS\n" + stagedReport + "\n" + FinishPauseProbe()); }
        catch (Exception error) { CompleteAutomatedSession("FAIL: " + error); }
    }

    private static void CompleteAutomatedSession(string result)
    {
        SessionState.SetString(ResultKey, result);
        SessionState.SetBool(PendingKey, false);
        Debug.Log(result);
        EditorApplication.isPlaying = false;
    }

    private sealed class MemoryStore : IBestiaryProgressStore
    {
        private readonly Dictionary<string, int> data = new Dictionary<string, int>();
        public int Read(string key) => data.TryGetValue(key, out int value) ? value : 0;
        public void Write(string key, int value) { data[key] = value; }
        public void Save() { }
    }

    private static float probeTime, probeScale;
    private static double probeRealtime;
    private static Vector3 probePosition;

    [MenuItem("Tools/The Forgotten Bestiary/Validar bestiario en Play Mode")]
    public static void RunFromMenu() { Debug.Log(Run()); }

    public static string Run()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Estas pruebas requieren Play Mode.");
        var journal = Object.FindObjectOfType<BestiaryJournal>();
        var player = Object.FindObjectOfType<PlayerMovementTest>();
        Require(journal != null && player != null, "Diario y jugador presentes");
        journal.Close();
        var checks = new List<string>();
        float originalScale = Time.timeScale;
        var store = new MemoryStore();
        var fixtures = new GameObject("Bestiary_Validation_Fixtures");
        BestiaryProgress.UseStore(store);
        try
        {
            var view = View(journal);
            journal.ShowPage(1);
            Require(view.lockedPage.activeSelf && !view.entryPage.activeSelf && view.portrait.sprite == null, "Página sellada sin retrato ni información");
            checks.Add("Bloqueo visual correcto");
            int[] required = { 5, 10, 3 };
            string[] prefabs = { "Murcielago", "Sapo", "Aguila" };
            for (int i = 0; i < required.Length; i++)
            {
                var entry = journal.Catalog.entries[i];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemigos/" + prefabs[i] + ".prefab");
                Require(prefab.GetComponent<BestiaryEnemy>().Species == entry.species, "Especie del prefab " + prefabs[i]);
                Require(entry.defeatsRequired == required[i], "Umbral " + prefabs[i]);
                // Instantiate only the real health/tracker components; AI and physical attacks do not belong in a counting fixture.
                var life = Fixture(prefab, fixtures.transform);
                life.RecibirDano(Damage(player.gameObject, 1));
                Require(BestiaryProgress.Count(entry.species) == 0, "El daño no letal no cuenta");
                life.gameObject.SetActive(false);
                Require(BestiaryProgress.Count(entry.species) == 0, "Desactivar no cuenta");
                var uncredited = Fixture(prefab, fixtures.transform);
                uncredited.RecibirDano(Damage(fixtures, 100));
                Require(BestiaryProgress.Count(entry.species) == 0, "Una muerte ajena al jugador no cuenta");
                for (int kill = 1; kill <= required[i]; kill++)
                {
                    var enemy = Fixture(prefab, fixtures.transform);
                    enemy.RecibirDano(Damage(player.gameObject, 100));
                    enemy.RecibirDano(Damage(player.gameObject, 100));
                    Require(BestiaryProgress.Count(entry.species) == kill, "Cada enemigo cuenta una vez");
                    Require(BestiaryProgress.IsUnlocked(entry) == (kill >= required[i]), "Desbloqueo exacto en " + kill);
                }
                journal.ShowPage(i + 1);
                Require(view.entryPage.activeSelf && !view.lockedPage.activeSelf && view.portrait.sprite != null, "Información revelada");
                checks.Add(prefabs[i] + ": daño/despawn/autoría/duplicados/umbral " + required[i] + " correctos");
            }
            Require(BestiaryProgress.UnlockedCount(journal.Catalog) == 3, "Tres registros revelados");
            BestiaryProgress.UseStore(store); // Simulate cache recreation when loading a save.
            Require(BestiaryProgress.Count(BestiarySpecies.Sapo) == 10, "Recarga de progreso");
            checks.Add("Recarga de progreso correcta");

            string prefix = "TFB.Bestiary.Validation." + Guid.NewGuid().ToString("N") + ".";
            try
            {
                BestiaryProgress.UseStore(new PlayerPrefsBestiaryStore(prefix));
                BestiaryProgress.RecordDefeat(BestiarySpecies.Murcielago);
                BestiaryProgress.UseStore(new PlayerPrefsBestiaryStore(prefix));
                Require(BestiaryProgress.Count(BestiarySpecies.Murcielago) == 1, "Persistencia PlayerPrefs");
                checks.Add("Persistencia real en PlayerPrefs comprobada con claves temporales");
            }
            finally
            {
                foreach (BestiarySpecies species in Enum.GetValues(typeof(BestiarySpecies))) PlayerPrefs.DeleteKey(prefix + BestiaryProgress.SaveKey(species));
                PlayerPrefs.Save();
            }
            BestiaryProgress.UseStore(store);
            Time.timeScale = 0.5f;
            Require(journal.Open() && Time.timeScale == 0f && BestiaryJournal.BlocksGameplayInput, "Abrir pausa y bloquea input");
            // It must also reject pause-menu requests while the journal owns the pause.
            var menuObject = new GameObject("Pause_Validation");
            menuObject.SetActive(false);
            var menu = menuObject.AddComponent<MenuPausa>();
            menu.Pausar(); // No UI references: would throw if the journal guard did not intercept it.
            Object.DestroyImmediate(menuObject);
            journal.Close();
            Require(Mathf.Approximately(Time.timeScale, 0.5f), "Restaurar la velocidad previa");
            Time.timeScale = 0f;
            Require(!journal.Open() && Time.timeScale == 0f, "Respeta otra pausa");
            checks.Add("Pausa, restauración a 0.5 y convivencia con MenuPausa correctas");
            return string.Join("\n", checks);
        }
        finally
        {
            journal.Close();
            Time.timeScale = originalScale;
            BestiaryProgress.UseStore(new PlayerPrefsBestiaryStore());
            journal.ShowPage(0);
            Object.DestroyImmediate(fixtures);
        }
    }

    public static string BeginPauseProbe()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Requiere Play Mode.");
        var journal = Object.FindObjectOfType<BestiaryJournal>();
        journal.Close();
        var store = new MemoryStore();
        foreach (var entry in journal.Catalog.entries) store.Write(BestiaryProgress.SaveKey(entry.species), entry.defeatsRequired);
        BestiaryProgress.UseStore(store);
        probeScale = Time.timeScale;
        Require(journal.Open(), "Abrir libro para comprobar el tiempo");
        journal.ShowPage(1);
        probeTime = Time.time;
        probeRealtime = EditorApplication.timeSinceStartup;
        probePosition = Object.FindObjectOfType<PlayerMovementTest>().transform.position;
        return "Probe started: timeScale=" + Time.timeScale;
    }

    public static string FinishPauseProbe()
    {
        var journal = Object.FindObjectOfType<BestiaryJournal>();
        try
        {
            Require(EditorApplication.timeSinceStartup - probeRealtime >= 0.2, "Ha transcurrido tiempo real");
            Require(Mathf.Abs(Time.time - probeTime) < 0.001f, "El tiempo de juego permanece congelado");
            Require((Object.FindObjectOfType<PlayerMovementTest>().transform.position - probePosition).sqrMagnitude < 0.000001f, "Player inmóvil durante la lectura");
            Require(View(journal).overlayFade.alpha > 0.99f, "La animación del libro avanza con tiempo no escalado");
            return "PASS: tiempo de juego congelado, Player inmóvil y animación de apertura activa durante la pausa.";
        }
        finally
        {
            journal.Close();
            Time.timeScale = probeScale;
            BestiaryProgress.UseStore(new PlayerPrefsBestiaryStore());
            journal.ShowPage(0);
        }
    }

    private static BestiaryJournalView View(BestiaryJournal journal) => (BestiaryJournalView)typeof(BestiaryJournal).GetField("view", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(journal);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception("Bestiary validation failed: " + message); }
    private static DamageInfo Damage(GameObject attacker, int amount) => new DamageInfo(amount, attacker, Vector2.zero, Vector2.zero, 0f);

    private static EnemigoVida Fixture(GameObject prefab, Transform parent)
    {
        var go = new GameObject("Validation_" + prefab.name);
        go.SetActive(false);
        go.transform.SetParent(parent);
        var life = go.AddComponent<EnemigoVida>();
        EditorUtility.CopySerialized(prefab.GetComponent<EnemigoVida>(), life);
        var tracker = go.AddComponent<BestiaryEnemy>();
        EditorUtility.CopySerialized(prefab.GetComponent<BestiaryEnemy>(), tracker);
        go.SetActive(true);
        return life;
    }
}
