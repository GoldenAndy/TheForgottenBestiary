using UnityEngine;

[DefaultExecutionOrder(-100), RequireComponent(typeof(Rigidbody2D)), RequireComponent(typeof(Collider2D))]
public class SapoSensorSuelo : MonoBehaviour
{
    [Header("Deteccion de suelo")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float anchoDetector = 0.65f;
    [SerializeField] private float alturaDetector = 0.08f;
    [SerializeField] private float separacionSuelo = 0.04f;
    [Header("Estabilidad")]
    [SerializeField] private float tiempoConfirmacionSuelo = 0.04f;
    [SerializeField] private float velocidadSalidaSuelo = 0.15f;
    public bool EnSuelo { get; private set; }
    public LayerMask CapasSuelo => groundLayer;
    private Rigidbody2D rb;
    private Collider2D cuerpoCollider;
    private float tiempoDetectandoSuelo;
    private readonly Collider2D[] encontrados = new Collider2D[12];
    private void Awake() { rb = GetComponent<Rigidbody2D>(); cuerpoCollider = GetComponent<Collider2D>(); }
    private void OnDisable() { EnSuelo = false; tiempoDetectandoSuelo = 0f; }
    private void FixedUpdate()
    {
        if (rb.velocity.y > velocidadSalidaSuelo)
        { EnSuelo = false; tiempoDetectandoSuelo = 0f; return; }
        Bounds b = cuerpoCollider.bounds;
        Vector2 centro = new Vector2(b.center.x, b.min.y - separacionSuelo * 0.5f);
        Vector2 tamano = new Vector2(b.size.x * anchoDetector, Mathf.Max(alturaDetector, separacionSuelo * 2f + 0.02f));
        int cantidad = Physics2D.OverlapBoxNonAlloc(centro, tamano, 0f, encontrados, groundLayer);
        bool detectado = false;
        for (int i = 0; i < cantidad; i++)
        {
            var col = encontrados[i];
            if (col == null || col.isTrigger || col.attachedRigidbody == rb || col.GetComponentInParent<SapoEnemy>() != null) continue;
            detectado = true; break;
        }
        tiempoDetectandoSuelo = detectado ? tiempoDetectandoSuelo + Time.fixedDeltaTime : 0f;
        EnSuelo = detectado && tiempoDetectandoSuelo >= tiempoConfirmacionSuelo;
    }
    private void OnDrawGizmosSelected()
    {
        var col = GetComponent<Collider2D>(); if (col == null) return;
        var b = col.bounds; Gizmos.color = Color.green;
        Gizmos.DrawWireCube(new Vector3(b.center.x, b.min.y - separacionSuelo * 0.5f, transform.position.z),
            new Vector3(b.size.x * anchoDetector, Mathf.Max(alturaDetector, separacionSuelo * 2f + 0.02f), 0f));
    }
}
