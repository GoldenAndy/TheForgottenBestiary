using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class MurcielagoEnemy : MonoBehaviour
{
    private enum EstadoMurcielago
    {
        Colgado,
        Persiguiendo,
        Regresando
    }

    [Header("Jugador")]
    [SerializeField] private Transform jugador;
    [SerializeField] private string playerTag = "Player";

    [Header("Detección")]
    [SerializeField] private float radioDeteccion = 4f;
    [SerializeField] private float radioTrabajo = 8f;

    [Header("Movimiento")]
    [SerializeField] private float velocidadVuelo = 3f;
    [SerializeField] private float velocidadRegreso = 3.5f;

    [SerializeField]
    private float distanciaDetenerseDelJugador = 0.4f;

    [SerializeField]
    private float distanciaLlegadaOrigen = 0.08f;

    // =========================================================
    // RETROCESO AL DAÑAR AL JUGADOR
    // =========================================================

    [Header("Retroceso al atacar")]

    [SerializeField]
    private float velocidadRetrocesoAtaque = 4f;

    [SerializeField]
    private float duracionRetrocesoAtaque = 0.25f;

    [SerializeField]
    private float pausaDespuesDelAtaque = 0.15f;

    // =========================================================
    // RETROCESO AL RECIBIR ESPADAZO
    // =========================================================

    [Header("Retroceso al recibir daño")]

    [Tooltip("Velocidad con la que sale despedido al recibir un golpe.")]
    [SerializeField]
    private float velocidadRetrocesoDano = 5.5f;

    [Tooltip("Tiempo durante el cual retrocede tras recibir un espadazo.")]
    [SerializeField]
    private float duracionRetrocesoDano = 0.22f;

    [Tooltip("Pequeña pausa antes de volver a perseguir.")]
    [SerializeField]
    private float pausaDespuesDeRecibirDano = 0.10f;

    // =========================================================
    // RUTA AUTOMÁTICA
    // =========================================================

    [Header("Ruta de regreso automática")]

    [Tooltip(
        "Distancia que debe recorrer antes de guardar " +
        "otro punto de su recorrido."
    )]
    [SerializeField]
    private float distanciaEntrePuntosRuta = 0.25f;

    [Tooltip(
        "Qué tan cerca debe estar de un punto de la ruta " +
        "para considerarlo alcanzado."
    )]
    [SerializeField]
    private float distanciaLlegadaPuntoRuta = 0.12f;

    [Tooltip(
        "Si no consigue acercarse a un punto durante este tiempo, " +
        "lo salta para evitar quedarse atascado."
    )]
    [SerializeField]
    private float tiempoMaximoSinProgreso = 0.45f;

    [Header("Debug")]
    [SerializeField] private bool dibujarGizmos = true;
    [SerializeField] private bool dibujarRuta = true;

    // =========================================================
    // COMPONENTES
    // =========================================================

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private EnemigoContacto enemigoContacto;
    private EnemigoVida enemigoVida;

    // =========================================================
    // ESTADO
    // =========================================================

    private EstadoMurcielago estadoActual;

    private Vector2 posicionInicial;

    private bool flipInicial;

    // =========================================================
    // RETROCESO
    // =========================================================

    private Vector2 direccionRetroceso;

    private float tiempoRetrocesoRestante;
    private float tiempoPausaRestante;

    // =========================================================
    // RUTA
    // =========================================================

    private readonly List<Vector2> rutaRecorrida =
        new List<Vector2>();

    private int indiceRutaRegreso = -1;

    private float distanciaAnteriorAlPunto =
        Mathf.Infinity;

    private float tiempoSinProgreso;
    private bool esperandoSalidaJugadorParaDespertar;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        enemigoContacto =
            GetComponent<EnemigoContacto>();

        enemigoVida =
            GetComponent<EnemigoVida>();
    }

    private void OnEnable()
    {
        if (enemigoContacto == null)
        {
            enemigoContacto =
                GetComponent<EnemigoContacto>();
        }

        if (enemigoVida == null)
        {
            enemigoVida =
                GetComponent<EnemigoVida>();
        }

        if (enemigoContacto != null)
        {
            enemigoContacto.OnDanoAplicado +=
                AlDañarJugador;
        }

        if (enemigoVida != null)
        {
            enemigoVida.OnDanoRecibido +=
                AlRecibirDano;
        }
    }

    private void OnDisable()
    {
        if (enemigoContacto != null)
        {
            enemigoContacto.OnDanoAplicado -=
                AlDañarJugador;
        }

        if (enemigoVida != null)
        {
            enemigoVida.OnDanoRecibido -=
                AlRecibirDano;
        }
    }

    private void Start()
    {
        posicionInicial = rb.position;

        flipInicial =
            spriteRenderer.flipX;

        ReiniciarRuta();

        if (jugador == null)
        {
            BuscarJugador();
        }

        EntrarEstadoColgado();
    }

    private void Update()
    {
        if (jugador == null)
        {
            BuscarJugador();
        }

        // =====================================================
        // ESTÁ RETROCEDIENDO
        // =====================================================

        if (tiempoRetrocesoRestante > 0f)
        {
            tiempoRetrocesoRestante -=
                Time.deltaTime;

            if (tiempoRetrocesoRestante <= 0f)
            {
                tiempoPausaRestante =
                    Mathf.Max(
                        tiempoPausaRestante,
                        0.01f
                    );
            }

            return;
        }

        // =====================================================
        // PAUSA POSTERIOR AL GOLPE
        // =====================================================

        if (tiempoPausaRestante > 0f)
        {
            tiempoPausaRestante -=
                Time.deltaTime;

            return;
        }

        // =====================================================
        // COMPORTAMIENTO NORMAL
        // =====================================================

        switch (estadoActual)
        {
            case EstadoMurcielago.Colgado:

                ActualizarColgado();

                break;

            case EstadoMurcielago.Persiguiendo:

                ActualizarPersiguiendo();

                break;

            case EstadoMurcielago.Regresando:

                ActualizarRegresando();

                break;
        }
    }

    private void FixedUpdate()
    {
        // =====================================================
        // RETROCESO
        // =====================================================

        if (tiempoRetrocesoRestante > 0f)
        {
            MoverRetroceso();

            return;
        }

        // =====================================================
        // PAUSA
        // =====================================================

        if (tiempoPausaRestante > 0f)
        {
            rb.velocity =
                Vector2.zero;

            return;
        }

        // =====================================================
        // MOVIMIENTO NORMAL
        // =====================================================

        switch (estadoActual)
        {
            case EstadoMurcielago.Colgado:

                MantenerColgado();

                break;

            case EstadoMurcielago.Persiguiendo:

                /*
                 * Guardamos el camino REAL por el
                 * que está pasando el murciélago.
                 */
                RegistrarPuntoRuta();

                MoverHaciaJugador();

                break;

            case EstadoMurcielago.Regresando:

                MoverPorRutaDeRegreso();

                break;
        }
    }

    // =========================================================
    // BUSCAR JUGADOR
    // =========================================================

    private void BuscarJugador()
    {
        GameObject objetoJugador =
            GameObject.FindGameObjectWithTag(
                playerTag
            );

        if (objetoJugador != null)
        {
            jugador =
                objetoJugador.transform;
        }
    }

    // =========================================================
    // COLGADO
    // =========================================================

    private void ActualizarColgado()
    {
        if (jugador == null)
            return;

        if (JugadorDentroDelRadioDeteccion())
        {
            EntrarEstadoPersiguiendo();
        }
    }

    private void MantenerColgado()
    {
        rb.velocity =
            Vector2.zero;

        rb.MovePosition(
            posicionInicial
        );
    }

    // =========================================================
    // PERSIGUIENDO
    // =========================================================

    private void ActualizarPersiguiendo()
    {
        if (jugador == null)
        {
            EntrarEstadoRegresando();
            return;
        }

        if (!JugadorDentroDelRadioTrabajo())
        {
            EntrarEstadoRegresando();
        }
    }

    private void MoverHaciaJugador()
    {
        if (jugador == null)
            return;

        Vector2 posicionJugador =
            jugador.position;

        Vector2 direccion =
            posicionJugador -
            rb.position;

        ActualizarFlip(
            direccion.x
        );

        float distancia =
            direccion.magnitude;

        if (distancia <=
            distanciaDetenerseDelJugador)
        {
            rb.velocity =
                Vector2.zero;

            return;
        }

        Vector2 nuevaPosicion =
            Vector2.MoveTowards(
                rb.position,
                posicionJugador,
                velocidadVuelo *
                Time.fixedDeltaTime
            );

        rb.MovePosition(
            nuevaPosicion
        );
    }

    // =========================================================
    // RUTA AUTOMÁTICA
    // =========================================================

    private void ReiniciarRuta()
    {
        rutaRecorrida.Clear();

        rutaRecorrida.Add(
            posicionInicial
        );

        indiceRutaRegreso = -1;

        ReiniciarDetectorAtasco();
    }

    private void RegistrarPuntoRuta()
    {
        if (rutaRecorrida.Count == 0)
        {
            rutaRecorrida.Add(
                rb.position
            );

            return;
        }

        Vector2 ultimoPunto =
            rutaRecorrida[
                rutaRecorrida.Count - 1
            ];

        float distancia =
            Vector2.Distance(
                ultimoPunto,
                rb.position
            );

        if (distancia >=
            distanciaEntrePuntosRuta)
        {
            rutaRecorrida.Add(
                rb.position
            );
        }
    }

    private void RegistrarPuntoRutaForzado()
    {
        if (rutaRecorrida.Count == 0)
        {
            rutaRecorrida.Add(
                rb.position
            );

            return;
        }

        Vector2 ultimo =
            rutaRecorrida[
                rutaRecorrida.Count - 1
            ];

        if (Vector2.Distance(
                ultimo,
                rb.position
            ) > 0.02f)
        {
            rutaRecorrida.Add(
                rb.position
            );
        }
    }

    // =========================================================
    // REGRESANDO
    // =========================================================

    private void EntrarEstadoRegresando()
    {
        estadoActual =
            EstadoMurcielago.Regresando;

        animator.SetBool(
            "IsFlying",
            true
        );

        /*
         * Guardamos exactamente el lugar desde
         * donde empieza a regresar.
         */
        RegistrarPuntoRutaForzado();

        indiceRutaRegreso =
            rutaRecorrida.Count - 1;

        ReiniciarDetectorAtasco();
    }

    private void ActualizarRegresando()
    {
        /*
         * Si vuelve a entrar el jugador en el radio,
         * reanudamos la persecución.
         */
        if (jugador != null &&
            JugadorDentroDelRadioDeteccion())
        {
            PrepararRutaTrasInterrumpirRegreso();

            EntrarEstadoPersiguiendo();

            return;
        }

        /*
         * Cuando terminamos todas las migas,
         * terminamos de posicionarlo exactamente
         * en su lugar de descanso.
         */
        if (indiceRutaRegreso < 0)
        {
            float distanciaOrigen =
                Vector2.Distance(
                    rb.position,
                    posicionInicial
                );

            if (distanciaOrigen <=
                distanciaLlegadaOrigen)
            {
                rb.position =
                    posicionInicial;

                EntrarEstadoColgado();
            }
        }
    }

    private void MoverPorRutaDeRegreso()
    {
        Vector2 destino;

        if (indiceRutaRegreso >= 0 &&
            indiceRutaRegreso <
            rutaRecorrida.Count)
        {
            destino =
                rutaRecorrida[
                    indiceRutaRegreso
                ];
        }
        else
        {
            destino =
                posicionInicial;
        }

        float distancia =
            Vector2.Distance(
                rb.position,
                destino
            );

        // =====================================================
        // PUNTO ALCANZADO
        // =====================================================

        if (distancia <=
            distanciaLlegadaPuntoRuta)
        {
            indiceRutaRegreso--;

            ReiniciarDetectorAtasco();

            return;
        }

        // =====================================================
        // DETECTOR ANTI-ATASCO
        // =====================================================

        DetectarAtasco(
            distancia
        );

        Vector2 direccion =
            destino -
            rb.position;

        ActualizarFlip(
            direccion.x
        );

        Vector2 nuevaPosicion =
            Vector2.MoveTowards(
                rb.position,
                destino,
                velocidadRegreso *
                Time.fixedDeltaTime
            );

        rb.MovePosition(
            nuevaPosicion
        );
    }

    private void DetectarAtasco(
        float distanciaActual)
    {
        /*
         * Si la distancia sí está disminuyendo,
         * está avanzando correctamente.
         */
        if (distanciaActual <
            distanciaAnteriorAlPunto - 0.005f)
        {
            distanciaAnteriorAlPunto =
                distanciaActual;

            tiempoSinProgreso = 0f;

            return;
        }

        tiempoSinProgreso +=
            Time.fixedDeltaTime;

        /*
         * Si lleva demasiado tiempo sin acercarse,
         * saltamos UNA miga.
         *
         * Como las migas están muy juntas,
         * esto permite salir de pequeñas esquinas
         * sin mandar al murciélago a atravesar
         * media cueva.
         */
        if (tiempoSinProgreso >=
            tiempoMaximoSinProgreso)
        {
            indiceRutaRegreso--;

            ReiniciarDetectorAtasco();
        }
    }

    private void ReiniciarDetectorAtasco()
    {
        distanciaAnteriorAlPunto =
            Mathf.Infinity;

        tiempoSinProgreso = 0f;
    }

    private void PrepararRutaTrasInterrumpirRegreso()
    {
        if (rutaRecorrida.Count == 0)
            return;

        /*
         * Ya que el murciélago regresó parte
         * del camino, eliminamos la parte
         * de la ruta que quedó por detrás.
         */
        int cantidadConservar =
            Mathf.Clamp(
                indiceRutaRegreso + 1,
                1,
                rutaRecorrida.Count
            );

        if (cantidadConservar <
            rutaRecorrida.Count)
        {
            rutaRecorrida.RemoveRange(
                cantidadConservar,
                rutaRecorrida.Count -
                cantidadConservar
            );
        }

        RegistrarPuntoRutaForzado();
    }

    // =========================================================
    // GOLPEAR AL JUGADOR
    // =========================================================

    private void AlDañarJugador(
        Vector2 direccionEmpujeJugador)
    {
        Vector2 direccion =
            -direccionEmpujeJugador.normalized;

        if (direccion == Vector2.zero)
        {
            direccion =
                Vector2.left;
        }

        IniciarRetroceso(
            direccion,
            velocidadRetrocesoAtaque,
            duracionRetrocesoAtaque,
            pausaDespuesDelAtaque
        );
    }

    // =========================================================
    // RECIBIR ESPADAZO
    // =========================================================

    private void AlRecibirDano(
        DamageInfo damageInfo)
    {
        /*
         * DamageInfo.direccion ya apunta desde
         * el atacante hacia el murciélago.
         *
         * Es exactamente la dirección en la que
         * queremos expulsarlo.
         */
        Vector2 direccion =
            damageInfo.direccion.normalized;

        if (direccion == Vector2.zero)
        {
            if (damageInfo.atacante != null)
            {
                direccion =
                    (
                        rb.position -
                        (Vector2)
                        damageInfo.atacante
                            .transform.position
                    ).normalized;
            }
        }

        if (direccion == Vector2.zero)
        {
            direccion =
                Vector2.right;
        }

        /*
         * Si estaba dormido y recibe un golpe,
         * obviamente se despierta.
         */
        if (estadoActual ==
            EstadoMurcielago.Colgado)
        {
            EntrarEstadoPersiguiendo();
        }

        IniciarRetroceso(
            direccion,
            velocidadRetrocesoDano,
            duracionRetrocesoDano,
            pausaDespuesDeRecibirDano
        );
    }

    // =========================================================
    // RETROCESO
    // =========================================================

    private void IniciarRetroceso(
        Vector2 direccion,
        float velocidad,
        float duracion,
        float pausa)
    {
        direccionRetroceso =
            direccion.normalized;

        velocidadRetrocesoActual =
            velocidad;

        tiempoRetrocesoRestante =
            duracion;

        tiempoPausaRestante =
            pausa;
    }

    private float velocidadRetrocesoActual;

    private void MoverRetroceso()
    {
        if (direccionRetroceso ==
            Vector2.zero)
        {
            return;
        }

        Vector2 nuevaPosicion =
            rb.position +
            direccionRetroceso *
            velocidadRetrocesoActual *
            Time.fixedDeltaTime;

        rb.MovePosition(
            nuevaPosicion
        );

        /*
         * Durante el retroceso mira hacia
         * el lado del atacante.
         */
        ActualizarFlip(
            -direccionRetroceso.x
        );
    }

    // =========================================================
    // ESTADOS
    // =========================================================

    private void EntrarEstadoColgado()
    {
        estadoActual =
            EstadoMurcielago.Colgado;

        rb.velocity =
            Vector2.zero;

        rb.position =
            posicionInicial;

        spriteRenderer.flipX =
            flipInicial;

        animator.SetBool(
            "IsFlying",
            false
        );

        /*
         * Una vez volvió a casa ya no necesitamos
         * conservar la ruta anterior.
         */
        ReiniciarRuta();
    }

    private void EntrarEstadoPersiguiendo()
    {
        estadoActual =
            EstadoMurcielago.Persiguiendo;

        animator.SetBool(
            "IsFlying",
            true
        );
    }

    // =========================================================
    // DISTANCIAS
    // =========================================================

    private bool JugadorDentroDelRadioDeteccion()
    {
        if (jugador == null)
            return false;

        float distancia =
            Vector2.Distance(
                posicionInicial,
                jugador.position
            );

        return distancia <=
            radioDeteccion;
    }

    private bool JugadorDentroDelRadioTrabajo()
    {
        if (jugador == null)
            return false;

        float distancia =
            Vector2.Distance(
                posicionInicial,
                jugador.position
            );

        return distancia <=
            radioTrabajo;
    }

    // =========================================================
    // SPRITE
    // =========================================================

    private void ActualizarFlip(
        float direccionX)
    {
        if (Mathf.Abs(direccionX) <
            0.01f)
        {
            return;
        }

        spriteRenderer.flipX =
            direccionX < 0f;
    }

    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (!dibujarGizmos)
            return;

        Vector3 centro =
            Application.isPlaying
                ? (Vector3)posicionInicial
                : transform.position;

        // Radio de detección.
        Gizmos.color =
            Color.green;

        Gizmos.DrawWireSphere(
            centro,
            radioDeteccion
        );

        // Radio máximo.
        Gizmos.color =
            Color.yellow;

        Gizmos.DrawWireSphere(
            centro,
            radioTrabajo
        );

        // Lugar de descanso.
        Gizmos.color =
            Color.white;

        Gizmos.DrawWireSphere(
            centro,
            0.1f
        );

        // Ruta real recorrida.
        if (!dibujarRuta ||
            !Application.isPlaying ||
            rutaRecorrida == null ||
            rutaRecorrida.Count < 2)
        {
            return;
        }

        Gizmos.color =
            Color.cyan;

        for (int i = 1;
             i < rutaRecorrida.Count;
             i++)
        {
            Gizmos.DrawLine(
                rutaRecorrida[i - 1],
                rutaRecorrida[i]
            );
        }
    }
}