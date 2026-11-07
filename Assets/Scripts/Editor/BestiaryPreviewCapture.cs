using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BestiaryPreviewCapture
{
    public static string LastLayoutReport { get; private set; }

    public static byte[] Render(int page, bool unlocked, int width = 1920, int height = 1080)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BestiaryBuilder.PrefabPath);
        var root = Object.Instantiate(prefab);
        root.name = "Bestiary_Temporary_Preview";
        root.hideFlags = HideFlags.HideAndDontSave;
        var cameraObject = new GameObject("Bestiary_Temporary_Camera");
        cameraObject.hideFlags = HideFlags.HideAndDontSave;
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.4f;
        camera.aspect = (float)width / height;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.12f, 0.11f);
        camera.cullingMask = 1 << 5;
        camera.transform.position = new Vector3(5000, 5000, -10);
        var rt = new RenderTexture(width, height, 24);
        var previous = RenderTexture.active;
        Texture2D texture = null;
        try
        {
            camera.targetTexture = rt;
            var canvas = root.GetComponent<Canvas>();
            root.GetComponent<UnityEngine.UI.CanvasScaler>().enabled = false;
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1920,1080);
            rect.position = new Vector3(5000,5000,0);
            rect.localScale = Vector3.one * 0.01f;
            root.GetComponent<BestiaryJournal>().EditorPreview(page, unlocked);
            Canvas.ForceUpdateCanvases();
            var report = new List<string>();
            foreach (var text in root.GetComponentsInChildren<TMP_Text>())
            {
                text.ForceMeshUpdate();
                if (text.isTextOverflowing) report.Add("OVERFLOW: " + text.name + " preferred=" + text.preferredHeight + " box=" + text.rectTransform.rect.height);
            }
            LastLayoutReport = report.Count == 0 ? "No text overflow" : string.Join("\n", report);
            camera.Render();
            RenderTexture.active = rt;
            texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            return texture.EncodeToPNG();
        }
        finally
        {
            RenderTexture.active = previous;
            camera.targetTexture = null;
            if (texture != null) Object.DestroyImmediate(texture);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(cameraObject);
        }
    }
}
