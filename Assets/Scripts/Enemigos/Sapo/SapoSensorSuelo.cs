using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class SapoSensorSuelo : MonoBehaviour
{
    [Header("Detección de suelo")]
    [SerializeField] private LayerMask groundLayer;

    [SerializeField]
    private float anchoDetector = 0.65f;

    [SerializeField]
    private float alturaDetector = 0.08f;

    [SerializeField]
    private float separacionSuelo = 0.04f;

    [Header("Estabilidad")]
    [Tooltip(
        "Pequeño tiempo de confirmación para evitar " +
        "que IsGrounded parpadee entre true y false."
    )]
    [SerializeField]
    private float tiempoConfirmacionSuelo = 0.04f;

    [Tooltip(
        "Si está subiendo más rápido que esto, " +
        "se considera inmediatamente fuera del suelo."
    )]
    [SerializeField]
    private float velocidadSalidaSuelo = 0.15f;

    public bool EnSuelo { get; private set; }

    private Rigidbody2D rb;
    private Collider2D cuerpoCollider;

    private float tiempoDetectandoSuelo;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cuerpoCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        /*
         * Si el sapo acaba de saltar, dejamos de considerarlo
         * en suelo inmediatamente aunque el detector todavía
         * roce el piso durante un frame.
         */
        if (rb.velocity.y > velocidadSalidaSuelo)
        {
            EnSuelo = false;
            tiempoDetectandoSuelo = 0f;

            return;
        }

        bool detectado =
            DetectarSueloFisico();

        if (detectado)
        {
            tiempoDetectandoSuelo +=
                Time.deltaTime;

            if (tiempoDetectandoSuelo >=
                tiempoConfirmacionSuelo)
            {
                EnSuelo = true;
            }
        }
        else
        {
            tiempoDetectandoSuelo = 0f;
            EnSuelo = false;
        }
    }

    private bool DetectarSueloFisico()
    {
        Bounds bounds =
            cuerpoCollider.bounds;

        Vector2 centro =
            new Vector2(
                bounds.center.x,
                bounds.min.y - separacionSuelo
            );

        Vector2 tamano =
            new Vector2(
                bounds.size.x * anchoDetector,
                alturaDetector
            );

        Collider2D[] encontrados =
            Physics2D.OverlapBoxAll(
                centro,
                tamano,
                0f,
                groundLayer
            );

        foreach (Collider2D col in encontrados)
        {
            if (col == null)
                continue;

            // Ignorar nuestro propio collider.
            if (col == cuerpoCollider)
                continue;

            // Ignorar hijos propios como StompZone.
            if (col.transform.IsChildOf(transform))
                continue;

            // Ignorar cualquier collider del mismo Rigidbody.
            if (col.attachedRigidbody == rb)
                continue;

            // ============================================
            // IGNORAR OTROS SAPOS
            // ============================================

            SapoEnemy otroSapo =
                col.GetComponentInParent<SapoEnemy>();

            if (otroSapo != null)
                continue;

            return true;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Collider2D col =
            GetComponent<Collider2D>();

        if (col == null)
            return;

        Bounds bounds =
            col.bounds;

        Vector3 centro =
            new Vector3(
                bounds.center.x,
                bounds.min.y - separacionSuelo,
                transform.position.z
            );

        Vector3 tamano =
            new Vector3(
                bounds.size.x * anchoDetector,
                alturaDetector,
                0f
            );

        Gizmos.color = Color.green;

        Gizmos.DrawWireCube(
            centro,
            tamano
        );
    }
}