using Cinemachine;
using UnityEngine;

public enum ModoVerticalCamara { MantenerAlturaNormal, AlturaFija, SeguirJugadorConMargen }
public enum ModoZoomCamara { MantenerZoomNormal, AjustarAlCollider, ZoomPersonalizado }

// Update the virtual camera before the Cinemachine Brain renders its pose.
[DefaultExecutionOrder(-100)]
public class ActivarSeguimientoCamara : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private CinemachineVirtualCamera camaraVirtual;
    [SerializeField] private Transform jugador;
    [Tooltip("Collider corporal del Player; la espada no activa zonas.")]
    [SerializeField] private Collider2D colliderJugador;
    [SerializeField] private Camera camaraPrincipal;
    [Header("Deteccion de zonas")]
    [Tooltip("Everything permite duplicar zonas sin crear capas nuevas. Nothing usa igualmente el registro de zonas.")]
    [SerializeField] private LayerMask capasZonasCamara = ~0;
    [SerializeField] private bool mostrarDebugZonaActiva;
    [Header("Activacion horizontal")]
    [Range(0f, 1f)] [SerializeField] private float puntoActivacionHorizontal = 0.5f;
    [Header("Seguimiento horizontal")]
    [SerializeField, Min(0.01f)] private float suavizadoHorizontal = 0.15f;
    [Header("Limites horizontales")]
    [SerializeField] private LimitesHorizontalesNivel limitesNivel;
    [Tooltip("Alternativa para escenas que usan un collider como limite de encuadre.")]
    [SerializeField] private Collider2D limitesHorizontalesCamara;
    [SerializeField, Min(0f)] private float margenLimiteHorizontal;
    [Header("Valores normales")]
    [SerializeField, Min(0.01f)] private float suavizadoVerticalNormal = 0.55f;
    [SerializeField, Min(0.01f)] private float suavizadoZoomNormal = 0.2f;

    private bool seguimientoActivado;
    private float posicionYNormal, zoomNormal;
    private float velocidadSuavizadoX, velocidadSuavizadoY, velocidadSuavizadoZoom;
    private ZonaCamaraVertical zonaActual;
    public ZonaCamaraVertical ZonaActual => zonaActual;
    public float AlturaNormal => posicionYNormal;
    public bool SeguimientoHorizontalActivo => seguimientoActivado;

    private void Awake()
    {
        if (camaraVirtual == null) camaraVirtual = GetComponent<CinemachineVirtualCamera>();
        if (camaraPrincipal == null) camaraPrincipal = Camera.main;
        if (jugador != null && colliderJugador == null) colliderJugador = jugador.GetComponent<Collider2D>();
        if (camaraVirtual == null || jugador == null || colliderJugador == null)
        { Debug.LogWarning("Faltan referencias de camara o cuerpo del Player en ActivarSeguimientoCamara.", this); enabled = false; return; }
        camaraVirtual.Follow = null; camaraVirtual.LookAt = null;
        posicionYNormal = camaraVirtual.transform.position.y;
        zoomNormal = camaraVirtual.m_Lens.OrthographicSize;
    }
    private void LateUpdate()
    {
        if (Time.deltaTime <= 0f || jugador == null || colliderJugador == null) return;
        ActualizarZona();
        if (!seguimientoActivado && camaraPrincipal != null)
            seguimientoActivado = camaraPrincipal.WorldToViewportPoint(jugador.position).x >= puntoActivacionHorizontal;

        float dt = Time.deltaTime;
        float zoomObjetivo = zoomNormal, suavizadoZoom = suavizadoZoomNormal;
        if (zonaActual != null)
        {
            float zoomZona = zonaActual.ObtenerZoomFinal();
            if (zoomZona > 0f) zoomObjetivo = zoomZona;
            suavizadoZoom = zonaActual.SuavizadoZoom;
        }
        var lente = camaraVirtual.m_Lens;
        lente.OrthographicSize = Mathf.SmoothDamp(lente.OrthographicSize, zoomObjetivo, ref velocidadSuavizadoZoom,
            Mathf.Max(0.01f, suavizadoZoom), Mathf.Infinity, dt);
        camaraVirtual.m_Lens = lente;

        Vector3 actual = camaraVirtual.transform.position;
        float objetivoX = seguimientoActivado ? jugador.position.x : actual.x;
        // Clamp the destination as well as the result, avoiding constant pressure against a boundary.
        objetivoX = LimitarXCamara(objetivoX);
        float x = Mathf.SmoothDamp(actual.x, objetivoX, ref velocidadSuavizadoX,
            Mathf.Max(0.01f, suavizadoHorizontal), Mathf.Infinity, dt);
        float xLimitada = LimitarXCamara(x);
        if (!Mathf.Approximately(x, xLimitada)) velocidadSuavizadoX = 0f;
        float objetivoY = posicionYNormal;
        float suavizadoY = suavizadoVerticalNormal;
        if (zonaActual != null)
        {
            suavizadoY = zonaActual.SuavizadoVertical;
            switch (zonaActual.ModoVertical)
            {
                case ModoVerticalCamara.AlturaFija:
                    objetivoY = zonaActual.ObtenerAlturaCamara() + zonaActual.OffsetY;
                    break;
                case ModoVerticalCamara.SeguirJugadorConMargen:
                    float diferencia = jugador.position.y + zonaActual.OffsetY - actual.y;
                    objetivoY = Mathf.Abs(diferencia) <= zonaActual.MargenVertical ? actual.y :
                        actual.y + diferencia - Mathf.Sign(diferencia) * zonaActual.MargenVertical;
                    break;
            }
            objetivoY = zonaActual.LimitarAltura(objetivoY);
        }
        // Always return to the normal height, including a zone entered before X tracking activates.
        float y = Mathf.SmoothDamp(actual.y, objetivoY, ref velocidadSuavizadoY,
            Mathf.Max(0.01f, suavizadoY), Mathf.Infinity, dt);
        camaraVirtual.transform.position = new Vector3(xLimitada, y, actual.z);
    }
    private void ActualizarZona()
    {
        ZonaCamaraVertical mejor = zonaActual != null && zonaActual.Contiene(colliderJugador, true) ? zonaActual : null;
        foreach (var zona in ZonaCamaraVertical.Activas)
        {
            if (zona == null || (capasZonasCamara.value != 0 && (capasZonasCamara.value & (1 << zona.gameObject.layer)) == 0)) continue;
            if (!zona.Contiene(colliderJugador, zona == zonaActual)) continue;
            // Keeping the current zone for equal priorities avoids oscillation along shared edges.
            if (mejor == null || zona.Prioridad > mejor.Prioridad ||
                (zona.Prioridad == mejor.Prioridad && mejor != zonaActual &&
                 (zona.BoundsZona.center - jugador.position).sqrMagnitude < (mejor.BoundsZona.center - jugador.position).sqrMagnitude)) mejor = zona;
        }
        if (mejor == zonaActual) return;
        zonaActual = mejor;
        if (mostrarDebugZonaActiva) Debug.Log("Zona de camara: " + (zonaActual != null ? zonaActual.name : "altura normal"), this);
    }
    private float LimitarXCamara(float x)
    {
        float ancho = camaraVirtual.m_Lens.OrthographicSize * (camaraPrincipal != null ? camaraPrincipal.aspect : 16f / 9f);
        if (limitesNivel != null) return limitesNivel.LimitarCamaraX(x, ancho, margenLimiteHorizontal);
        if (limitesHorizontalesCamara == null) return x;
        var b = limitesHorizontalesCamara.bounds;
        float min = b.min.x + ancho + margenLimiteHorizontal, max = b.max.x - ancho - margenLimiteHorizontal;
        return min > max ? b.center.x : Mathf.Clamp(x, min, max);
    }
}
