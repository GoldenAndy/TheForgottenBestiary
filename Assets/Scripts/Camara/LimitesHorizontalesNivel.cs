using UnityEngine;

/// <summary>Shared world boundaries for camera framing, physical walls and Player recovery.</summary>
[DefaultExecutionOrder(100)]
public class LimitesHorizontalesNivel : MonoBehaviour
{
    [Header("Marcadores del escenario")]
    [SerializeField] private Transform puntoIzquierdo;
    [SerializeField] private Transform puntoDerecho;
    [Header("Player de esta escena")]
    [SerializeField] private Rigidbody2D jugador;
    [SerializeField] private Collider2D cuerpoJugador;
    [Header("Barreras invisibles")]
    [SerializeField] private BoxCollider2D barreraIzquierda;
    [SerializeField] private BoxCollider2D barreraDerecha;
    [SerializeField, Min(10f)] private float alturaBarreras = 160f;
    public bool TieneLimites => puntoIzquierdo != null && puntoDerecho != null && puntoIzquierdo.position.x < puntoDerecho.position.x;
    public float MinX => puntoIzquierdo.position.x;
    public float MaxX => puntoDerecho.position.x;
    private void FixedUpdate() { ActualizarBarreras(); CorregirJugador(); }
    private void LateUpdate() { CorregirJugador(); }
    public float LimitarCamaraX(float x, float mitadAncho, float margen)
    {
        if (!TieneLimites) return x;
        float min = MinX + mitadAncho + Mathf.Max(0f, margen), max = MaxX - mitadAncho - Mathf.Max(0f, margen);
        return min > max ? (MinX + MaxX) * 0.5f : Mathf.Clamp(x, min, max);
    }
    public void ActualizarBarreras()
    {
        if (!TieneLimites) return;
        ActualizarBarrera(barreraIzquierda, MinX - 0.5f);
        ActualizarBarrera(barreraDerecha, MaxX + 0.5f);
    }
    private void ActualizarBarrera(BoxCollider2D pared, float x)
    {
        if (pared == null) return;
        var posicion = new Vector3(x, 15f, 0f);
        var tamano = new Vector2(1f, alturaBarreras);
        if (pared.transform.position != posicion) pared.transform.position = posicion;
        if (pared.size != tamano) pared.size = tamano;
        if (pared.offset != Vector2.zero) pared.offset = Vector2.zero;
        if (pared.isTrigger) pared.isTrigger = false;
    }
    private void CorregirJugador()
    {
        if (!TieneLimites || jugador == null || cuerpoJugador == null || !cuerpoJugador.enabled || !jugador.gameObject.activeInHierarchy) return;
        var b = cuerpoJugador.bounds;
        float correccion = b.min.x < MinX ? MinX - b.min.x : b.max.x > MaxX ? MaxX - b.max.x : 0f;
        if (Mathf.Abs(correccion) < 0.0001f) return;
        jugador.position += new Vector2(correccion, 0f);
        Vector2 v = jugador.velocity;
        if ((correccion > 0f && v.x < 0f) || (correccion < 0f && v.x > 0f)) jugador.velocity = new Vector2(0f, v.y);
        Physics2D.SyncTransforms();
    }
    private void OnDrawGizmosSelected()
    {
        if (!TieneLimites) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(MinX, -10f), new Vector3(MinX, 25f));
        Gizmos.DrawLine(new Vector3(MaxX, -10f), new Vector3(MaxX, 25f));
    }
}
