using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ZonaCamaraVertical : MonoBehaviour
{
    [Header("Prioridad")]
    [Tooltip("Si varias zonas se tocan al mismo tiempo, gana la prioridad más alta.")]
    [SerializeField] private int prioridad = 0;

    [Header("Movimiento vertical")]
    [SerializeField] private ModoVerticalCamara modoVertical = ModoVerticalCamara.AlturaFija;

    [Tooltip("Si está activo, la altura fija será el centro real del BoxCollider2D.")]
    [SerializeField] private bool usarAlturaDeEsteObjeto = true;

    [SerializeField] private float alturaFijaDeCamara;

    [Tooltip("Sirve para subir o bajar un poco el encuadre.")]
    [SerializeField] private float offsetY;

    [Tooltip("Solo se usa en modo SeguirJugadorConMargen. Mientras más alto, menos sigue saltitos.")]
    [SerializeField] private float margenVertical = 0.75f;

    [SerializeField] private float suavizadoVertical = 0.25f;

    [Header("Zoom")]
    [SerializeField] private ModoZoomCamara modoZoom = ModoZoomCamara.MantenerZoomNormal;

    [Tooltip("Solo se usa si Modo Zoom es ZoomPersonalizado.")]
    [SerializeField] private float zoomPersonalizado = 4.92f;

    [Tooltip("Solo se usa si Modo Zoom es AjustarAlCollider.")]
    [SerializeField] private float margenInternoZoom = 0.15f;

    [SerializeField] private float suavizadoZoom = 0.2f;

    private Collider2D zonaCollider;

    public int Prioridad => prioridad;
    public ModoVerticalCamara ModoVertical => modoVertical;
    public ModoZoomCamara ModoZoom => modoZoom;
    public float OffsetY => offsetY;
    public float MargenVertical => margenVertical;
    public float SuavizadoVertical => suavizadoVertical;
    public float SuavizadoZoom => suavizadoZoom;

    public Bounds BoundsZona => zonaCollider.bounds;

    private void Awake()
    {
        zonaCollider = GetComponent<Collider2D>();
        zonaCollider.isTrigger = true;
    }

    public float ObtenerAlturaCamara()
    {
        if (usarAlturaDeEsteObjeto && zonaCollider != null)
        {
            return zonaCollider.bounds.center.y;
        }

        return alturaFijaDeCamara;
    }

    public float ObtenerZoomFinal()
    {
        switch (modoZoom)
        {
            case ModoZoomCamara.MantenerZoomNormal:
                return -1f;

            case ModoZoomCamara.ZoomPersonalizado:
                return zoomPersonalizado;

            case ModoZoomCamara.AjustarAlCollider:
                return CalcularZoomSegunCollider();

            default:
                return -1f;
        }
    }

    private float CalcularZoomSegunCollider()
    {
        if (zonaCollider == null)
        {
            return -1f;
        }

        Camera camaraPrincipal = Camera.main;

        float aspecto = camaraPrincipal != null
            ? camaraPrincipal.aspect
            : 16f / 9f;

        Bounds limites = zonaCollider.bounds;

        float zoomPorAlto = (limites.size.y * 0.5f) - margenInternoZoom;
        float zoomPorAncho = (limites.size.x * 0.5f / aspecto) - margenInternoZoom;

        float zoomFinal = Mathf.Min(zoomPorAlto, zoomPorAncho);

        return Mathf.Max(0.5f, zoomFinal);
    }

    private void OnDrawGizmosSelected()
    {
        Collider2D col = GetComponent<Collider2D>();

        Gizmos.color = Color.cyan;

        if (col != null)
        {
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
        else
        {
            Gizmos.DrawWireCube(transform.position, transform.localScale);
        }
    }
}