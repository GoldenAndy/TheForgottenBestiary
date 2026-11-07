using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class BestiaryBuilder
{
    public const string PrefabPath = "Assets/Resources/Bestiary/BestiaryJournal.prefab";
    public const string CatalogPath = "Assets/Bestiary/BestiaryCatalog.asset";
    private const string Kit = "Assets/Recursos Externos/Pixel Adventure BookUI_Fantasy RPG Kit_v1.1/";
    private static readonly Color Ink = new Color32(65, 39, 29, 255);
    private static readonly Color Muted = new Color32(119, 79, 48, 255);
    private static readonly Color Gold = new Color32(243, 211, 139, 255);
    private static TMP_FontAsset bodyFont, titleFont, smallFont;

    [MenuItem("Tools/The Forgotten Bestiary/Construir interfaz del bestiario")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Construye el bestiario fuera de Play Mode.");
        Directory.CreateDirectory("Assets/Bestiary/Visuals");
        Directory.CreateDirectory("Assets/Resources/Bestiary");
        AssetDatabase.Refresh();
        bodyFont = CreateFont("CrimsonText-Regular");
        titleFont = CreateFont("CrimsonText-SemiBold");
        smallFont = CreateFont("BestiaryUI", "Assets/Recursos Externos/TextMesh Pro/Fonts/LiberationSans.ttf");
        var book = CopySprite("UI/Backgrounds/UI_Image_MagicBook.png");
        var ribbon = CopySprite("UI/Decorations/UI_TitleBg_Ribbon_Large.png");
        var ornament = CopySprite("UI/Decorations/UI_Image_Deco.png");
        var seal = CopySprite("Features/Bonus/UI_Image_seal.png");
        var lockIcon = CopySprite("Icons/System/Icon_lock.png");
        var bookIcon = CopySprite("Icons/System/Icon_book.png");
        var row = CopySprite("UI/Lists/UI_ListItem_Normal.png", 13);
        var rowSelected = CopySprite("UI/Lists/UI_ListItem_Hover.png", 13);
        var square = CopySprite("UI/Buttons/UI_Btn_square.png");
        var left = CopySprite("Icons/System/Icon_chevron-left.png");
        var right = CopySprite("Icons/System/Icon_chevron-right.png");
        var cross = CopySprite("UI/Buttons/UI_BtnIcon_x.png");
        var catalog = CreateCatalog();

        var root = new GameObject("BestiaryJournal", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(BestiaryJournal));
        root.layer = 5;
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250;
        canvas.pixelPerfect = false;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var v = new BestiaryJournalView { rowNormal = row, rowSelected = rowSelected };

        var hint = ImageRect("B_Abrir_Bestiario", root.transform, new Vector2(-186, 36), new Vector2(326, 44), null, new Color(0.06f, 0.08f, 0.08f, 0.86f));
        Anchor(hint.rectTransform, new Vector2(1, 0));
        v.gameplayHint = hint.gameObject;
        v.hintButton = MakeButton(hint);
        ImageRect("Icono_Libro", hint.transform, new Vector2(-137, 0), new Vector2(29, 29), bookIcon, Gold);
        v.hintLabel = Text("Tecla_B", hint.transform, new Vector2(13, 0), new Vector2(275, 35), "B  ·  BESTIARIO   0/3", 20, Gold, smallFont);

        var toast = ImageRect("Aviso_Registro_Revelado", root.transform, new Vector2(0, -76), new Vector2(460, 78), row, Color.white, true);
        Anchor(toast.rectTransform, new Vector2(0.5f, 1));
        toast.raycastTarget = false;
        v.notification = toast.gameObject.AddComponent<CanvasGroup>();
        v.notification.blocksRaycasts = false;
        v.notification.interactable = false;
        v.notification.alpha = 0;
        ImageRect("Icono_Libro", toast.transform, new Vector2(-186, 0), new Vector2(42, 42), bookIcon, Ink);
        v.notificationLabel = Text("Mensaje", toast.transform, new Vector2(26, 0), new Vector2(370, 70), "", 22, Ink, smallFont);

        var overlay = ImageRect("Libro_Abierto", root.transform, Vector2.zero, Vector2.zero, null, new Color(0.025f, 0.037f, 0.042f, 0.89f));
        Stretch(overlay.rectTransform);
        overlay.raycastTarget = true;
        v.overlay = overlay.gameObject;
        v.overlayFade = overlay.gameObject.AddComponent<CanvasGroup>();
        v.overlayFade.alpha = 0;
        var title = ImageRect("Titulo_Cinta", overlay.transform, new Vector2(0, 432), new Vector2(630, 127), ribbon);
        Text("Titulo", title.transform, new Vector2(0, 2), new Vector2(550, 65), "EL BESTIARIO OLVIDADO", 37, Gold, titleFont);
        v.closeButton = IconButton("Cerrar_B_o_Esc", overlay.transform, new Vector2(720, 379), square, cross);

        var pages = ImageRect("Libro_Pergamino", overlay.transform, new Vector2(0, -15), new Vector2(1405, 765), book);
        pages.raycastTarget = false;
        v.book = pages.rectTransform;
        var index = Rect("Indice", pages.transform, new Vector2(-310, 30), new Vector2(490, 610));
        Text("Cabecera", index, new Vector2(0, 275), new Vector2(470, 48), "Índice de campo", 36, Ink, titleFont);
        ImageRect("Ornamento", index, new Vector2(0, 237), new Vector2(216, 13), ornament);
        Text("Subtitulo", index, new Vector2(0, 202), new Vector2(470, 45), "El bosque aún tiene historias que contar.", 23, Muted);
        v.indexButtons = new Button[4];
        v.indexBackgrounds = new Image[4];
        v.indexStates = new TMP_Text[4];
        string[] names = { "Carta del abuelo", "Murciélago", "Sapo", "Águila" };
        for (int i = 0; i < 4; i++)
        {
            var background = ImageRect("Hoja_" + i, index, new Vector2(0, 126 - i * 99), new Vector2(458, 82), row, Color.white, true);
            v.indexBackgrounds[i] = background;
            v.indexButtons[i] = MakeButton(background);
            Text("Numero", background.transform, new Vector2(-184, 0), new Vector2(49, 44), (i + 1).ToString("00"), 24, Muted, titleFont);
            Text("Especie", background.transform, new Vector2(17, 14), new Vector2(330, 40), names[i], 30, Ink, titleFont);
            v.indexStates[i] = Text("Estado", background.transform, new Vector2(17, -20), new Vector2(340, 26), i == 0 ? "EL COMIENZO DE LA EXPEDICIÓN" : "PÁGINA SELLADA", 14, Muted, smallFont);
        }
        ImageRect("Ornamento_Progreso", index, new Vector2(0, -238), new Vector2(216, 13), ornament);
        v.completion = Text("Progreso", index, new Vector2(0, -279), new Vector2(480, 35), "0 / 3 REGISTROS REVELADOS", 18, Muted, smallFont);

        var content = Rect("Paginas_Del_Diario", pages.transform, new Vector2(315, 30), new Vector2(490, 610));
        var letter = Rect("00_Carta_Del_Abuelo", content, Vector2.zero, new Vector2(490, 610));
        v.letterPage = letter.gameObject;
        Text("Fecha", letter, new Vector2(0, 286), new Vector2(460, 28), "ANOTACIÓN SIN FECHA", 15, Muted, smallFont);
        Text("Titulo_Carta", letter, new Vector2(0, 240), new Vector2(470, 62), "A quien herede este libro", 33, Ink, titleFont);
        ImageRect("Ornamento", letter, new Vector2(0, 195), new Vector2(216, 13), ornament);
        v.letterBody = Text("Carta", letter, new Vector2(0, -14), new Vector2(446, 381), catalog.openingLetter, 25, Ink);
        v.letterBody.alignment = TextAlignmentOptions.TopLeft;
        v.letterBody.enableAutoSizing = true;
        v.letterBody.fontSizeMin = 22;
        v.letterBody.fontSizeMax = 26;
        v.letterBody.lineSpacing = 3;
        Text("Firma", letter, new Vector2(80, -239), new Vector2(280, 46), "— Tu abuelo", 31, Ink, titleFont).alignment = TextAlignmentOptions.Right;
        ImageRect("Sello_De_Cera", letter, new Vector2(-155, -248), new Vector2(70, 70), seal);

        var locked = Rect("Entrada_Sellada", content, Vector2.zero, new Vector2(490, 610));
        v.lockedPage = locked.gameObject;
        Text("Categoria", locked, new Vector2(0, 285), new Vector2(460, 30), "REGISTRO INCOMPLETO", 16, Muted, smallFont);
        v.lockedName = Text("Nombre_De_Especie", locked, new Vector2(0, 226), new Vector2(470, 65), "", 42, Ink, titleFont);
        ImageRect("Ornamento", locked, new Vector2(0, 175), new Vector2(216, 13), ornament);
        ImageRect("Sello_De_Cera", locked, new Vector2(0, 45), new Vector2(119, 119), seal);
        ImageRect("Candado", locked, new Vector2(0, 46), new Vector2(38, 38), lockIcon, Gold);
        Text("Silencio", locked, new Vector2(0, -80), new Vector2(440, 60), "La tinta aún guarda silencio.", 29, Muted);
        Text("Sin_Informacion", locked, new Vector2(0, -145), new Vector2(428, 60), "Los secretos de esta especie\npermanecen sellados.", 24, Muted);

        var entryPage = Rect("Registro_Revelado", content, Vector2.zero, new Vector2(490, 610));
        v.entryPage = entryPage.gameObject;
        v.entryName = Text("Nombre", entryPage, new Vector2(0, 278), new Vector2(470, 60), "", 43, Ink, titleFont);
        v.entryEpithet = Text("Epiteto", entryPage, new Vector2(0, 231), new Vector2(470, 35), "", 24, Muted);
        ImageRect("Ornamento", entryPage, new Vector2(0, 200), new Vector2(216, 13), ornament);
        var portraitFrame = ImageRect("Marco_Retrato", entryPage, new Vector2(-127, 127), new Vector2(174, 145), row, Color.white, true);
        v.portrait = ImageRect("Retrato_Animado", portraitFrame.transform, Vector2.zero, new Vector2(128, 115), null);
        v.portrait.preserveAspect = true;
        Text("Habitat_Titulo", entryPage, new Vector2(110, 177), new Vector2(225, 30), "HÁBITAT", 15, Muted, smallFont);
        v.habitat = Text("Habitat", entryPage, new Vector2(110, 122), new Vector2(220, 88), "", 26, Ink);
        v.description = Text("Descripcion", entryPage, new Vector2(0, -18), new Vector2(449, 105), "", 24, Ink);
        v.description.alignment = TextAlignmentOptions.TopLeft;
        Text("Notas_Titulo", entryPage, new Vector2(0, -82), new Vector2(450, 30), "NOTAS DE CAMPO", 15, Muted, smallFont).alignment = TextAlignmentOptions.Left;
        v.fieldNotes = Text("Notas", entryPage, new Vector2(0, -144), new Vector2(449, 100), "", 24, Ink);
        v.fieldNotes.alignment = TextAlignmentOptions.TopLeft;
        v.grandfatherNote = Text("Voz_Del_Abuelo", entryPage, new Vector2(0, -234), new Vector2(445, 74), "", 24, Muted);
        v.grandfatherNote.fontStyle = FontStyles.Italic;
        v.observedCount = Text("Conteo_Confirmado", entryPage, new Vector2(0, -285), new Vector2(460, 30), "", 16, Muted, smallFont);

        v.previousButton = IconButton("Hoja_Anterior", overlay.transform, new Vector2(-147, -433), square, left);
        v.nextButton = IconButton("Hoja_Siguiente", overlay.transform, new Vector2(147, -433), square, right);
        v.pageNumber = Text("Numero_De_Hoja", overlay.transform, new Vector2(0, -433), new Vector2(220, 40), "HOJA 01 / 04", 20, Gold, smallFont);
        Text("Controles", overlay.transform, new Vector2(0, -493), new Vector2(1000, 35), "← / →  Cambiar hoja      ·      B / Esc  Cerrar", 20, new Color32(205, 195, 173, 255), smallFont);

        var journal = root.GetComponent<BestiaryJournal>();
        journal.Configure(catalog, v);
        journal.EditorPreview(0, false);
        journal.EditorClosePreview();
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        ConfigureEnemies();
        AssetDatabase.SaveAssets();
        Debug.Log("Bestiario construido: carta, tres especies, sprites originales, pausa y progreso persistente.");
    }

    [MenuItem("Tools/The Forgotten Bestiary/Instalar bestiario en la escena activa")]
    public static void InstallInActiveScene()
    {
        if (Object.FindObjectOfType<BestiaryJournal>() != null) return;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) throw new InvalidOperationException("Construye primero el prefab del bestiario.");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(go, "Instalar bestiario");
        EditorSceneManager.MarkSceneDirty(go.scene);
    }

    private static BestiaryCatalog CreateCatalog()
    {
        var c = AssetDatabase.LoadAssetAtPath<BestiaryCatalog>(CatalogPath);
        if (c != null && c.entries != null && c.entries.Length > 0) return c;
        if (c == null) { c = ScriptableObject.CreateInstance<BestiaryCatalog>(); AssetDatabase.CreateAsset(c, CatalogPath); }
        c.openingLetter = "Mi tesoro no espera en ningún mapa.\n\nEl camino duerme en las criaturas del bosque. Aprende sus hábitos y vence su resistencia: cada encuentro devolverá la memoria a estas páginas.\n\nSolo quien revele sus secretos será digno de mi último escondite. Cuando la última hoja despierte, vuelve a leer.";
        c.entries = new[] {
            Entry(BestiarySpecies.Murcielago, "Murciélago", "Vigía de las sombras", 5, "Murcielago", "murcielago-volando", "Copas y refugios oscuros", "Descansa colgado hasta detectar un intruso. Lo persigue por el aire y regresa a su refugio cuando el peligro se aleja.", "Tras un golpe o una embestida necesita recuperarse. Aprovecha esa pausa para responder con la espada.", "El silencio del bosque también tiene alas."),
            Entry(BestiarySpecies.Sapo, "Sapo", "Guardián del sotobosque", 10, "Sapo", "Sapo_Idle", "Suelo y claros del bosque", "Alterna saltos cortos y largos para acercarse, huir y rodear a quien invade su territorio. Rara vez permanece quieto.", "La espada y el pisotón pueden herirlo. Espera su aterrizaje y cae sobre su cabeza: el rebote te permitirá ganar altura.", "Hasta el charco más pequeño guarda un guardián."),
            Entry(BestiarySpecies.Aguila, "Águila", "Centinela del dosel", 3, "Aguila", "Aguila_Idle", "Cielo y alturas del bosque", "Vigila desde el aire y ataca en picada. Al compartir territorio con otra águila, coordina maniobras más peligrosas.", "Observa la picada y sus evasiones. Busca el instante en que expone su cuerpo y responde con la espada.", "No mires sus garras: aprende a leer su vuelo.")
        };
        EditorUtility.SetDirty(c);
        return c;
    }

    private static BestiaryEntry Entry(BestiarySpecies species, string name, string epithet, int required, string prefabName, string clipName, string habitat, string description, string notes, string quote)
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemigos/" + prefabName + ".prefab");
        var clip = go.GetComponent<Animator>().runtimeAnimatorController.animationClips.FirstOrDefault(a => a.name == clipName);
        var frames = new List<Sprite>();
        if (clip != null)
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                if (binding.type == typeof(SpriteRenderer) && binding.propertyName == "m_Sprite")
                    foreach (var key in AnimationUtility.GetObjectReferenceCurve(clip, binding))
                        if (key.value is Sprite sprite && !frames.Contains(sprite)) frames.Add(sprite);
        var portrait = frames.Count > 0 ? frames[0] : go.GetComponentInChildren<SpriteRenderer>().sprite;
        return new BestiaryEntry { species = species, speciesName = name, epithet = epithet, defeatsRequired = required, portrait = portrait, portraitFrames = frames.ToArray(), habitat = habitat, description = description, fieldNotes = notes, grandfatherNote = quote };
    }

    private static void ConfigureEnemies()
    {
        string[] names = { "Murcielago", "Sapo", "Aguila" };
        for (int i = 0; i < names.Length; i++)
        {
            string path = "Assets/Prefabs/Enemigos/" + names[i] + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var tracker = root.GetComponent<BestiaryEnemy>();
                if (tracker == null) tracker = root.AddComponent<BestiaryEnemy>();
                var so = new SerializedObject(tracker);
                so.FindProperty("species").enumValueIndex = i;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }

    private static TMP_FontAsset CreateFont(string name, string sourcePath = null)
    {
        string path = "Assets/Bestiary/Fonts/" + name + " SDF.asset";
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (existing != null) return existing;
        var source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath ?? "Assets/Bestiary/Fonts/" + name + ".ttf");
        if (source == null) throw new InvalidOperationException("Falta la fuente " + name);
        var font = TMP_FontAsset.CreateFontAsset(source, 48, 5, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        font.name = name + " SDF";
        font.isMultiAtlasTexturesEnabled = true;
        font.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789ÁÉÍÓÚÜÑáéíóúüñ¿¡.,;:—“”'!?()/- ·←→", out string missing);
        AssetDatabase.CreateAsset(font, path);
        if (font.material != null) AssetDatabase.AddObjectToAsset(font.material, font);
        foreach (var texture in font.atlasTextures) if (texture != null && !EditorUtility.IsPersistent(texture)) AssetDatabase.AddObjectToAsset(texture, font);
        EditorUtility.SetDirty(font);
        return font;
    }

    private static Sprite CopySprite(string relative, int border = 0)
    {
        string dest = "Assets/Bestiary/Visuals/" + Path.GetFileName(relative);
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(dest) == null)
        {
            if (!AssetDatabase.CopyAsset(Kit + relative, dest)) throw new InvalidOperationException("No se pudo copiar " + relative);
            var importer = (TextureImporter)AssetImporter.GetAtPath(dest);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(dest);
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        var r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = position;
        r.sizeDelta = size;
        return r;
    }
    private static void Anchor(RectTransform rect, Vector2 anchor) { rect.anchorMin = rect.anchorMax = anchor; }
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private static Image ImageRect(string name, Transform parent, Vector2 position, Vector2 size, Sprite sprite, Color? tint = null, bool sliced = false)
    {
        var rect = Rect(name, parent, position, size);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite; image.color = tint ?? Color.white; image.raycastTarget = false;
        if (sliced) image.type = Image.Type.Sliced;
        return image;
    }
    private static TextMeshProUGUI Text(string name, Transform parent, Vector2 position, Vector2 size, string value, float fontSize, Color color, TMP_FontAsset font = null)
    {
        var rect = Rect(name, parent, position, size);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font != null ? font : bodyFont;
        text.text = value; text.fontSize = fontSize; text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }
    private static Button MakeButton(Image image)
    {
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.94f, 0.77f);
        colors.pressedColor = new Color(0.8f, 0.67f, 0.51f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.45f);
        button.colors = colors;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        return button;
    }
    private static Button IconButton(string name, Transform parent, Vector2 position, Sprite background, Sprite icon)
    {
        var image = ImageRect(name, parent, position, new Vector2(58, 58), background);
        var b = MakeButton(image);
        ImageRect("Icono", image.transform, Vector2.zero, new Vector2(32, 32), icon, Gold);
        return b;
    }
}
