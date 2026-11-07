using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[Serializable]
public sealed class BestiaryJournalView
{
    public GameObject overlay;
    public CanvasGroup overlayFade;
    public RectTransform book;
    public GameObject letterPage, lockedPage, entryPage;
    public TMP_Text letterBody, lockedName, entryName, entryEpithet;
    public TMP_Text habitat, description, fieldNotes, grandfatherNote, observedCount;
    public TMP_Text completion, pageNumber, hintLabel, notificationLabel;
    public Image portrait;
    public Button hintButton, closeButton, previousButton, nextButton;
    public Button[] indexButtons;
    public Image[] indexBackgrounds;
    public TMP_Text[] indexStates;
    public Sprite rowNormal, rowSelected;
    public CanvasGroup notification;
    public GameObject gameplayHint;
}

[DisallowMultipleComponent]
public sealed class BestiaryJournal : MonoBehaviour
{
    [SerializeField] private BestiaryCatalog catalog;
    [SerializeField] private BestiaryJournalView view = new BestiaryJournalView();
    private static BestiaryJournal instance;
    private static int closedFrame = -1;
    private bool isOpen, ownsPause;
    private float previousTimeScale, fade, notificationRemaining, portraitTimer;
    private int page, portraitFrame;
    private PlayerMovementTest player;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;
    private BestiaryEntry displayedEntry;
    private bool editorPreview;

    public static bool IsOpen => instance != null && instance.isOpen;
    public static bool BlocksGameplayInput => IsOpen || closedFrame == Time.frameCount;
    public int CurrentPage => page;
    public BestiaryCatalog Catalog => catalog;

    public void Configure(BestiaryCatalog data, BestiaryJournalView references) { catalog = data; view = references; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; closedFrame = -1; }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        player = FindObjectOfType<PlayerMovementTest>();
        view.overlay.SetActive(false);
        view.notification.alpha = 0f;
        view.hintButton.onClick.AddListener(Toggle);
        view.closeButton.onClick.AddListener(Close);
        view.previousButton.onClick.AddListener(() => ShowPage(page - 1));
        view.nextButton.onClick.AddListener(() => ShowPage(page + 1));
        for (int i = 0; i < view.indexButtons.Length; i++)
        {
            int selectedPage = i;
            view.indexButtons[i].onClick.AddListener(() => ShowPage(selectedPage));
        }
        RefreshProgress();
    }

    private void OnEnable() { BestiaryProgress.DefeatRecorded += OnDefeatRecorded; }
    private void OnDisable()
    {
        BestiaryProgress.DefeatRecorded -= OnDefeatRecorded;
        ReleasePause();
        isOpen = false;
        if (instance == this) instance = null;
    }

    private bool CanRead()
    {
        if (player == null) player = FindObjectOfType<PlayerMovementTest>();
        if (player == null || !player.isActiveAndEnabled) return false;
        var stats = player.GetComponent<PlayerStats>();
        return stats == null || stats.Health > 0f;
    }

    private void Update()
    {
        if (editorPreview) return;
        bool available = CanRead();
        view.gameplayHint.SetActive(available && !isOpen && Time.timeScale > 0f);
        if (isOpen && !available) Close();
        if (Input.GetKeyDown(KeyCode.B)) Toggle();
        else if (isOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
        else if (isOpen)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) ShowPage(page + 1);
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) ShowPage(page - 1);
            if (Input.GetKeyDown(KeyCode.Home)) ShowPage(0);
        }

        fade = Mathf.MoveTowards(fade, isOpen ? 1f : 0f, Time.unscaledDeltaTime * 7f);
        view.overlayFade.alpha = Mathf.SmoothStep(0f, 1f, fade);
        view.book.localScale = Vector3.one * Mathf.Lerp(0.97f, 1f, view.overlayFade.alpha);
        if (!isOpen && fade <= 0f && view.overlay.activeSelf) view.overlay.SetActive(false);

        if (isOpen && displayedEntry != null && displayedEntry.portraitFrames != null && displayedEntry.portraitFrames.Length > 1)
        {
            portraitTimer += Time.unscaledDeltaTime;
            if (portraitTimer >= 0.14f)
            {
                portraitTimer %= 0.14f;
                portraitFrame = (portraitFrame + 1) % displayedEntry.portraitFrames.Length;
                view.portrait.sprite = displayedEntry.portraitFrames[portraitFrame];
            }
        }

        notificationRemaining = Mathf.Max(0f, notificationRemaining - Time.unscaledDeltaTime);
        view.notification.alpha = Mathf.MoveTowards(view.notification.alpha, notificationRemaining > 0f && !isOpen ? 1f : 0f, Time.unscaledDeltaTime * 5f);
    }

    public void Toggle() { if (isOpen) Close(); else Open(); }

    public bool Open()
    {
        if (isOpen || !CanRead() || Time.timeScale <= 0f) return false;
        previousTimeScale = Time.timeScale;
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        ownsPause = true;
        isOpen = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        view.overlay.SetActive(true);
        view.overlayFade.interactable = true;
        view.overlayFade.blocksRaycasts = true;
        view.gameplayHint.SetActive(false);
        ShowPage(page);
        return true;
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        closedFrame = Time.frameCount;
        view.overlayFade.interactable = false;
        view.overlayFade.blocksRaycasts = false;
        ReleasePause();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void ReleasePause()
    {
        if (!ownsPause) return;
        Time.timeScale = previousTimeScale;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
        ownsPause = false;
    }

    public void ShowPage(int selectedPage) { RenderPage(selectedPage, null); }

    private void RenderPage(int selectedPage, bool? previewUnlocked)
    {
        if (catalog == null || catalog.entries.Length == 0) return;
        page = Mathf.Clamp(selectedPage, 0, catalog.entries.Length);
        displayedEntry = null;
        portraitTimer = 0f;
        portraitFrame = 0;
        for (int i = 0; i < view.indexButtons.Length; i++)
            view.indexBackgrounds[i].sprite = i == page ? view.rowSelected : view.rowNormal;

        bool letter = page == 0;
        var entry = letter ? null : catalog.entries[page - 1];
        bool unlocked = entry != null && (previewUnlocked ?? BestiaryProgress.IsUnlocked(entry));
        view.letterPage.SetActive(letter);
        view.lockedPage.SetActive(!letter && !unlocked);
        view.entryPage.SetActive(!letter && unlocked);
        view.letterBody.text = catalog.openingLetter;
        view.portrait.sprite = null;
        if (entry != null)
        {
            // A sealed page contains no enemy image, statistics, lore or kill counter.
            view.lockedName.text = entry.speciesName;
            if (unlocked)
            {
                displayedEntry = entry;
                view.entryName.text = entry.speciesName;
                view.entryEpithet.text = entry.epithet;
                view.portrait.sprite = entry.portrait;
                view.habitat.text = entry.habitat;
                view.description.text = entry.description;
                view.fieldNotes.text = entry.fieldNotes;
                view.grandfatherNote.text = "“" + entry.grandfatherNote + "”";
                view.observedCount.text = "Ejemplares vencidos: " + BestiaryProgress.Count(entry.species);
            }
        }
        view.previousButton.interactable = page > 0;
        view.nextButton.interactable = page < catalog.entries.Length;
        view.pageNumber.text = "HOJA " + (page + 1).ToString("00") + " / " + (catalog.entries.Length + 1).ToString("00");
        RefreshProgress();
    }

    private void RefreshProgress()
    {
        if (catalog == null) return;
        int unlocked = BestiaryProgress.UnlockedCount(catalog);
        view.completion.text = unlocked + " / " + catalog.entries.Length + " REGISTROS REVELADOS";
        view.hintLabel.text = "B  ·  BESTIARIO   " + unlocked + "/" + catalog.entries.Length;
        if (view.indexStates != null)
            for (int i = 1; i < view.indexStates.Length; i++)
                view.indexStates[i].text = BestiaryProgress.IsUnlocked(catalog.entries[i - 1]) ? "REGISTRO REVELADO" : "PÁGINA SELLADA";
    }

    private void OnDefeatRecorded(BestiarySpecies species, int count)
    {
        RefreshProgress();
        var entry = catalog.Find(species);
        if (entry == null) return;
        if (isOpen) ShowPage(page);
        if (count != entry.defeatsRequired) return;
        view.notificationLabel.text = "REGISTRO REVELADO\n" + entry.speciesName + "   ·   B para leer";
        notificationRemaining = 5f;
    }

#if UNITY_EDITOR
    // Visual preview only: does not pause, save progress, or mark any entry as unlocked.
    public void EditorPreview(int selectedPage, bool unlocked)
    {
        editorPreview = true;
        view.overlay.SetActive(true);
        view.overlayFade.alpha = 1f;
        view.book.localScale = Vector3.one;
        view.gameplayHint.SetActive(false);
        view.notification.alpha = 0f;
        RenderPage(selectedPage, unlocked);
        if (unlocked && selectedPage > 0)
            view.indexStates[selectedPage].text = "REGISTRO REVELADO";
    }
    public void EditorClosePreview()
    {
        editorPreview = false;
        view.overlay.SetActive(false);
        view.gameplayHint.SetActive(true);
    }
#endif
}
