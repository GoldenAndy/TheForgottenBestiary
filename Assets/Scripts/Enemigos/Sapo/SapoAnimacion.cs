using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SapoSensorSuelo))]
public class SapoAnimacion : MonoBehaviour
{
    // =========================================================
    // UMBRALES
    // =========================================================

    [Header("Umbrales")]

    [Tooltip(
        "Velocidad vertical mínima para considerar " +
        "que el sapo está subiendo."
    )]
    [SerializeField]
    private float velocidadMinimaSubida = 0.15f;

    [Tooltip(
        "Velocidad vertical máxima negativa para considerar " +
        "que el sapo está cayendo."
    )]
    [SerializeField]
    private float velocidadMinimaCaida = -0.15f;

    // =========================================================
    // COMPONENTES
    // =========================================================

    private Rigidbody2D rb;
    private Animator animator;
    private SapoSensorSuelo sensorSuelo;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        rb =
            GetComponent<Rigidbody2D>();

        animator =
            GetComponent<Animator>();

        sensorSuelo =
            GetComponent<SapoSensorSuelo>();
    }

    private void Update()
    {
        bool enSuelo =
            sensorSuelo.EnSuelo;

        float velocidadVertical =
            rb.velocity.y;

        // =====================================================
        // ESTABILIZAR VELOCIDAD VERTICAL
        // =====================================================

        /*
         * Cuando está apoyado en el suelo pueden existir
         * pequeñas velocidades producidas por la física.
         *
         * Para el Animator esas velocidades deben ser cero.
         */
        if (enSuelo)
        {
            velocidadVertical = 0f;
        }
        else
        {
            /*
             * Si está en el aire pero la velocidad se encuentra
             * entre los dos umbrales, está aproximadamente
             * en el punto más alto del salto.
             *
             * Lo convertimos en cero para impedir cambios
             * rápidos entre Jump y Fall.
             */
            bool zonaNeutra =
                velocidadVertical <=
                    velocidadMinimaSubida &&
                velocidadVertical >=
                    velocidadMinimaCaida;

            if (zonaNeutra)
            {
                velocidadVertical = 0f;
            }
        }

        // =====================================================
        // ACTUALIZAR ANIMATOR
        // =====================================================

        animator.SetBool(
            "IsGrounded",
            enSuelo
        );

        animator.SetFloat(
            "VerticalSpeed",
            velocidadVertical
        );
    }
}