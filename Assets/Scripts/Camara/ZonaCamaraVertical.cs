using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(Collider2D))]
public class ZonaCamaraVertical : MonoBehaviour
{
    [Header("Prioridad")]
    [SerializeField] private int prioridad;
    [Header("Movimiento vertical")]
    [SerializeField] private ModoVerticalCamara modoVertical = ModoVerticalCamara.AlturaFija;
    [SerializeField] private bool usarAlturaDeEsteObjeto = true;
    [SerializeField] private float alturaFijaDeCamara;
    [SerializeField] private float offsetY;
    [SerializeField, Min(0f)] private float margenVertical = 0.75f;
    [SerializeField, Min(0.01f)] private float suavizadoVertical = 0.45f;
    [SerializeField] private bool limitarAltura;
    [SerializeField] private float alturaMinimaCamara;
    [SerializeField] private float alturaMaximaCamara = 14f;
    [Header("Salida estable")]
    [Tooltip("Amplia solo la salida: evita alternar de camara al rozar el borde de una zona.")]
    [SerializeField, Min(0f)] private float margenSalida = 0.6f;
    [Header("Zoom")]
    [SerializeField] private ModoZoomCamara modoZoom = ModoZoomCamara.MantenerZoomNormal;
    [SerializeField, Min(0.5f)] private float zoomPersonalizado = 4.92f;
    [SerializeField, Min(0f)] private float margenInternoZoom = 0.15f;
    [SerializeField, Min(0.01f)] private float suavizadoZoom = 0.2f;
    private Collider2D zonaCollider;
    private static readonly List<ZonaCamaraVertical> activas = new List<ZonaCamaraVertical>();
    public static IReadOnlyList<ZonaCamaraVertical> Activas => activas;
    public int Prioridad => prioridad;
    public ModoVerticalCamara ModoVertical => modoVertical;
    public ModoZoomCamara ModoZoom => modoZoom;
    public float OffsetY => offsetY;
    public float MargenVertical => Mathf.Max(0f, margenVertical);
    public float SuavizadoVertical => suavizadoVertical;
    public float SuavizadoZoom => suavizadoZoom;
    private Collider2D ColliderZona { get { if (zonaCollider == null) zonaCollider = GetComponent<Collider2D>(); return zonaCollider; } }
    public Bounds BoundsZona => ColliderZona != null ? ColliderZona.bounds : new Bounds(transform.position, Vector3.zero);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarRegistro() { activas.Clear(); }
    private void Awake() { zonaCollider = GetComponent<Collider2D>(); zonaCollider.isTrigger = true; }
    private void OnEnable() { if (!activas.Contains(this)) activas.Add(this); }
    private void OnDisable() { activas.Remove(this); }
    public bool Contiene(Collider2D cuerpo, bool mantener)
    {
        if (!isActiveAndEnabled || cuerpo == null || ColliderZona == null || !ColliderZona.enabled || !gameObject.activeInHierarchy) return false;
        Bounds b = ColliderZona.bounds;
        if (mantener) b.Expand(new Vector3(margenSalida * 2f, margenSalida * 2f, 0f));
        var p = cuerpo.bounds;
        bool solapan = b.min.x <= p.max.x && b.max.x >= p.min.x && b.min.y <= p.max.y && b.max.y >= p.min.y;
        return solapan && (mantener || ColliderZona.Distance(cuerpo).isOverlapped);
    }
    public float LimitarAltura(float altura)
    { return limitarAltura ? Mathf.Clamp(altura, Mathf.Min(alturaMinimaCamara, alturaMaximaCamara), Mathf.Max(alturaMinimaCamara, alturaMaximaCamara)) : altura; }
    public float ObtenerAlturaCamara() => usarAlturaDeEsteObjeto ? BoundsZona.center.y : alturaFijaDeCamara;
    public float ObtenerZoomFinal()
    {
        if (modoZoom == ModoZoomCamara.MantenerZoomNormal) return -1f;
        if (modoZoom == ModoZoomCamara.ZoomPersonalizado) return Mathf.Max(0.5f, zoomPersonalizado);
        var cam = Camera.main;
        float aspecto = cam != null ? cam.aspect : 16f / 9f;
        var b = BoundsZona;
        return Mathf.Max(0.5f, Mathf.Min(b.extents.y, b.extents.x / aspecto) - margenInternoZoom);
    }
    private void OnDrawGizmosSelected()
    {
        if (ColliderZona == null) return;
        Gizmos.color = Color.cyan; Gizmos.DrawWireCube(BoundsZona.center, BoundsZona.size);
        var b = BoundsZona; b.Expand(new Vector3(margenSalida * 2f, margenSalida * 2f, 0f));
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f); Gizmos.DrawWireCube(b.center, b.size);
    }
}
