using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlataformaMovil : MonoBehaviour
{
    // =========================================================
    // PUNTOS
    // =========================================================

    [Header("Puntos de recorrido")]
    [SerializeField] private Transform puntoA;
    [SerializeField] private Transform puntoB;


    // =========================================================
    // MOVIMIENTO
    // =========================================================

    [Header("Movimiento")]

    [SerializeField]
    private float velocidad = 2f;

    [Tooltip(
        "Distancia mínima para considerar que la plataforma " +
        "llegó a uno de los puntos."
    )]
    [SerializeField]
    private float distanciaLlegada = 0.05f;


    // =========================================================
    // PAUSA
    // =========================================================

    [Header("Pausa en los extremos")]

    [Tooltip(
        "Tiempo que espera la plataforma antes de regresar. " +
        "Ponlo en 0 si no quieres ninguna pausa."
    )]
    [SerializeField]
    private float tiempoEspera = 0.25f;


    // =========================================================
    // COMPONENTES
    // =========================================================

    private Rigidbody2D rb;


    // =========================================================
    // ESTADO
    // =========================================================

    private Vector2 posicionA;
    private Vector2 posicionB;

    private Vector2 destino;

    private bool viajandoHaciaB = true;

    private float tiempoRestanteEspera;


    // =========================================================
    // VELOCIDAD DE LA PLATAFORMA
    // =========================================================

    /*
     * Esta propiedad permite que otros objetos,
     * especialmente el jugador, sepan a qué velocidad
     * se está desplazando actualmente la plataforma.
     *
     * Esto es MUY importante para plataformas móviles,
     * porque el jugador debe calcular su movimiento
     * relativo al suelo que tiene debajo.
     */
    public Vector2 VelocidadActual { get; private set; }


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        /*
         * La plataforma la mueve el código,
         * no la gravedad ni las fuerzas.
         */
        rb.bodyType =
            RigidbodyType2D.Kinematic;

        rb.freezeRotation =
            true;

        /*
         * Interpolate suaviza visualmente el movimiento
         * entre pasos de física.
         */
        rb.interpolation =
            RigidbodyInterpolation2D.Interpolate;

        /*
         * Ayuda a evitar problemas de colisión
         * cuando la plataforma se mueve.
         */
        rb.collisionDetectionMode =
            CollisionDetectionMode2D.Continuous;

        /*
         * Al iniciar todavía no existe movimiento.
         */
        VelocidadActual =
            Vector2.zero;
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (puntoA == null ||
            puntoB == null)
        {
            Debug.LogError(
                $"{name}: debes asignar Punto A y Punto B.",
                this
            );

            enabled = false;

            return;
        }

        /*
         * MUY IMPORTANTE:
         *
         * Guardamos las posiciones originales.
         *
         * Así, aunque PuntoA o PuntoB sean hijos de algún
         * objeto que después se mueva, nuestra ruta no
         * empieza a desplazarse accidentalmente.
         */
        posicionA =
            puntoA.position;

        posicionB =
            puntoB.position;

        destino =
            posicionB;

        viajandoHaciaB =
            true;

        tiempoRestanteEspera =
            0f;

        VelocidadActual =
            Vector2.zero;
    }


    // =========================================================
    // FIXED UPDATE
    // =========================================================

    private void FixedUpdate()
    {
        // =====================================================
        // ESPERA EN UN EXTREMO
        // =====================================================

        if (tiempoRestanteEspera > 0f)
        {
            /*
             * Mientras la plataforma está detenida,
             * su velocidad real es cero.
             *
             * Esto evita que el jugador siga creyendo
             * que la plataforma continúa desplazándose.
             */
            VelocidadActual =
                Vector2.zero;

            tiempoRestanteEspera -=
                Time.fixedDeltaTime;

            if (tiempoRestanteEspera < 0f)
            {
                tiempoRestanteEspera =
                    0f;
            }

            return;
        }


        // =====================================================
        // CALCULAR NUEVA POSICIÓN
        // =====================================================

        /*
         * Guardamos dónde estaba la plataforma antes
         * del movimiento de este paso de física.
         */
        Vector2 posicionAnterior =
            rb.position;


        Vector2 nuevaPosicion =
            Vector2.MoveTowards(
                rb.position,
                destino,
                velocidad *
                Time.fixedDeltaTime
            );


        // =====================================================
        // CALCULAR VELOCIDAD REAL
        // =====================================================

        /*
         * velocidad = desplazamiento / tiempo
         *
         * Ejemplo:
         *
         * Si en un FixedUpdate la plataforma avanzó
         * 0.04 unidades y FixedDeltaTime es 0.02,
         *
         * 0.04 / 0.02 = 2 unidades por segundo.
         */
        VelocidadActual =
            (nuevaPosicion - posicionAnterior) /
            Time.fixedDeltaTime;


        // =====================================================
        // MOVER PLATAFORMA
        // =====================================================

        /*
         * Al tener Rigidbody2D no movemos el Transform
         * directamente.
         */
        rb.MovePosition(
            nuevaPosicion
        );


        // =====================================================
        // COMPROBAR LLEGADA
        // =====================================================

        float distancia =
            Vector2.Distance(
                nuevaPosicion,
                destino
            );

        if (distancia >
            distanciaLlegada)
        {
            return;
        }


        // =====================================================
        // CAMBIAR DIRECCIÓN
        // =====================================================

        viajandoHaciaB =
            !viajandoHaciaB;

        destino =
            viajandoHaciaB
                ? posicionB
                : posicionA;

        tiempoRestanteEspera =
            tiempoEspera;
    }


    // =========================================================
    // DESACTIVACIÓN
    // =========================================================

    private void OnDisable()
    {
        /*
         * Si por cualquier razón la plataforma se desactiva,
         * dejamos explícitamente su velocidad en cero.
         */
        VelocidadActual =
            Vector2.zero;
    }


    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (puntoA == null ||
            puntoB == null)
        {
            return;
        }

        /*
         * Nos permite ver en Scene la ruta
         * que seguirá la plataforma.
         */
        Gizmos.DrawLine(
            puntoA.position,
            puntoB.position
        );

        Gizmos.DrawWireSphere(
            puntoA.position,
            0.15f
        );

        Gizmos.DrawWireSphere(
            puntoB.position,
            0.15f
        );
    }
}