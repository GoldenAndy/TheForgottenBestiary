using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ZonaCamaraVertical))]
public class ZonaCamaraVerticalEditor : Editor
{
    private SerializedProperty prioridad;

    private SerializedProperty modoVertical;
    private SerializedProperty usarAlturaDeEsteObjeto;
    private SerializedProperty alturaFijaDeCamara;
    private SerializedProperty offsetY;
    private SerializedProperty margenVertical;
    private SerializedProperty suavizadoVertical;

    private SerializedProperty modoZoom;
    private SerializedProperty zoomPersonalizado;
    private SerializedProperty margenInternoZoom;
    private SerializedProperty suavizadoZoom;

    private void OnEnable()
    {
        prioridad = serializedObject.FindProperty("prioridad");

        modoVertical = serializedObject.FindProperty("modoVertical");
        usarAlturaDeEsteObjeto = serializedObject.FindProperty("usarAlturaDeEsteObjeto");
        alturaFijaDeCamara = serializedObject.FindProperty("alturaFijaDeCamara");
        offsetY = serializedObject.FindProperty("offsetY");
        margenVertical = serializedObject.FindProperty("margenVertical");
        suavizadoVertical = serializedObject.FindProperty("suavizadoVertical");

        modoZoom = serializedObject.FindProperty("modoZoom");
        zoomPersonalizado = serializedObject.FindProperty("zoomPersonalizado");
        margenInternoZoom = serializedObject.FindProperty("margenInternoZoom");
        suavizadoZoom = serializedObject.FindProperty("suavizadoZoom");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DibujarScriptDeshabilitado();

        EditorGUILayout.Space(6);

        EditorGUILayout.LabelField("Prioridad", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(prioridad);

        EditorGUILayout.Space(10);

        DibujarMovimientoVertical();

        EditorGUILayout.Space(10);

        DibujarZoom();

        serializedObject.ApplyModifiedProperties();
    }

    private void DibujarScriptDeshabilitado()
    {
        using (new EditorGUI.DisabledScope(true))
        {
            MonoScript script = MonoScript.FromMonoBehaviour((ZonaCamaraVertical)target);
            EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
        }
    }

    private void DibujarMovimientoVertical()
    {
        EditorGUILayout.LabelField("Movimiento vertical", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(modoVertical);

        ModoVerticalCamara modo =
            (ModoVerticalCamara)modoVertical.enumValueIndex;

        switch (modo)
        {
            case ModoVerticalCamara.MantenerAlturaNormal:
                EditorGUILayout.HelpBox(
                    "La cámara mantendrá la altura normal. No se usan opciones verticales adicionales.",
                    MessageType.Info
                );
                break;

            case ModoVerticalCamara.AlturaFija:
                EditorGUILayout.PropertyField(usarAlturaDeEsteObjeto);

                if (!usarAlturaDeEsteObjeto.boolValue)
                {
                    EditorGUILayout.PropertyField(alturaFijaDeCamara);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "La altura fija se tomará del centro real del BoxCollider2D.",
                        MessageType.None
                    );
                }

                EditorGUILayout.PropertyField(offsetY);
                EditorGUILayout.PropertyField(suavizadoVertical);
                break;

            case ModoVerticalCamara.SeguirJugadorConMargen:
                EditorGUILayout.PropertyField(offsetY);
                EditorGUILayout.PropertyField(margenVertical);
                EditorGUILayout.PropertyField(suavizadoVertical);

                EditorGUILayout.HelpBox(
                    "La cámara seguirá al jugador en Y solo cuando se aleje más que el margen vertical.",
                    MessageType.None
                );
                break;
        }
    }

    private void DibujarZoom()
    {
        EditorGUILayout.LabelField("Zoom", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(modoZoom);

        ModoZoomCamara modo =
            (ModoZoomCamara)modoZoom.enumValueIndex;

        switch (modo)
        {
            case ModoZoomCamara.MantenerZoomNormal:
                EditorGUILayout.HelpBox(
                    "La cámara mantendrá el zoom normal. No se usan opciones adicionales de zoom.",
                    MessageType.Info
                );
                break;

            case ModoZoomCamara.ZoomPersonalizado:
                EditorGUILayout.PropertyField(zoomPersonalizado);
                EditorGUILayout.PropertyField(suavizadoZoom);
                break;

            case ModoZoomCamara.AjustarAlCollider:
                EditorGUILayout.PropertyField(margenInternoZoom);
                EditorGUILayout.PropertyField(suavizadoZoom);

                EditorGUILayout.HelpBox(
                    "El zoom se calculará usando el tamaño del BoxCollider2D de esta zona.",
                    MessageType.None
                );
                break;
        }
    }
}