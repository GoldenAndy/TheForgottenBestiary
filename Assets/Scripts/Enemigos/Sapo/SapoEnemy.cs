using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(EnemigoVida))]
[RequireComponent(typeof(EnemigoContacto))]
[RequireComponent(typeof(SapoSensorSuelo))]
[RequireComponent(typeof(SapoCoordinacionGrupo))]
public class SapoEnemy : MonoBehaviour, IFiltroDanoContacto
{
    private enum TipoSalto
    {
        Pequeno,
        Medio,
        Grande
    }

    // =========================================================
    // JUGADOR
    // =========================================================

    [Header("Jugador")]
    [SerializeField] private Transform jugador;
    [SerializeField] private string playerTag = "Player";

    [Header("Atención")]
    [SerializeField] private float radioAtencion = 8f;
    [SerializeField] private float intervaloDecision = 0.12f;

    // =========================================================
    // SALTOS
    // =========================================================

    [Header("Salto pequeño")]
    [SerializeField] private float pequenoHorizontal = 1.6f;
    [SerializeField] private float pequenoVertical = 4.5f;

    [Header("Salto medio")]
    [SerializeField] private float medioHorizontal = 3f;
    [SerializeField] private float medioVertical = 6f;

    [Header("Salto grande")]
    [SerializeField] private float grandeHorizontal = 4.5f;
    [SerializeField] private float grandeVertical = 8f;

    [Header("Frecuencia de salto")]
    [SerializeField] private float tiempoMinimoEntreSaltos = 0.35f;
    [SerializeField] private float variacionTiempoSalto = 0.15f;

    [Header("Pausa al aterrizar")]
    [SerializeField] private float pausaMinimaAterrizaje = 0.35f;
    [SerializeField] private float pausaMaximaAterrizaje = 0.65f;

    // =========================================================
    // DISTANCIAS
    // =========================================================

    [Header("Distancias de decisión")]
    [SerializeField] private float distanciaMuyCerca = 1.4f;
    [SerializeField] private float distanciaMedia = 3.5f;

    // =========================================================
    // EVASIÓN
    // =========================================================

    [Header("Evasión")]
    [SerializeField] private float radioEvasionEspada = 2f;
    [SerializeField] private float radioEvasionPisoton = 1.3f;

    [SerializeField]
    private float velocidadCaidaAmenazante = -0.5f;

    [Range(0f, 1f)]
    [SerializeField]
    private float probabilidadEvasionEspada = 0.70f;

    [Range(0f, 1f)]
    [SerializeField]
    private float probabilidadEvasionPisoton = 0.60f;

    [Range(0f, 1f)]
    [SerializeField]
    private float probabilidadSaltarSobreJugador = 0.55f;

    // =========================================================
    // PISOTÓN
    // =========================================================

    [Header("Pisotón")]
    [SerializeField] private int danoPisoton = 1;

    [Tooltip("Velocidad vertical que recibe el jugador al rebotar.")]
    [SerializeField]
    private float fuerzaReboteJugador = 8f;

    [SerializeField]
    private float cooldownPisoton = 0.18f;

    [SerializeField]
    private float tiempoIgnorarContactoTrasPisoton = 0.25f;

    [Tooltip(
        "Margen vertical para considerar que el jugador " +
        "está cayendo sobre la parte superior del sapo."
    )]
    [SerializeField]
    private float margenCabezaPisoton = 0.25f;

    // =========================================================
    // SQUASH
    // =========================================================

    [Header("Squash por pisotón")]

    [Tooltip(
        "Transform visual que se aplasta. " +
        "Idealmente debe ser un hijo Visual."
    )]
    [SerializeField]
    private Transform visualParaSquash;

    [SerializeField]
    private float squashX = 1.15f;

    [SerializeField]
    private float squashY = 0.70f;

    [SerializeField]
    private float duracionSquash = 0.16f;

    // =========================================================
    // FEEDBACK DE DAÑO
    // =========================================================

    [Header("Feedback de daño")]
    [SerializeField] private int cantidadParpadeos = 3;
    [SerializeField] private float tiempoParpadeo = 0.06f;

    // =========================================================
    // RETROCESO POR ESPADA
    // =========================================================

    [Header("Retroceso al recibir espada")]
    [SerializeField] private float velocidadRetroceso = 4f;
    [SerializeField] private float duracionRetroceso = 0.18f;

    // =========================================================
    // SEGURIDAD DE SALTOS
    // =========================================================

    [Header("Seguridad de saltos")]

    [Tooltip(
        "Debe contener solamente las Layers consideradas suelo."
    )]
    [SerializeField]
    private LayerMask groundLayer;

    [SerializeField]
    private float alturaChequeoSuelo = 3f;

    [SerializeField]
    private float distanciaChequeoAterrizaje = 2.2f;

    // =========================================================
    // SPRITE
    // =========================================================

    [Header("Sprite")]

    [Tooltip(
        "Actívalo únicamente si los sprites originales " +
        "miran hacia la derecha."
    )]
    [SerializeField]
    private bool spriteMiraDerecha = false;

    // =========================================================
    // COMPONENTES
    // =========================================================

    private Rigidbody2D rb;
    private Rigidbody2D rbJugador;

    private Collider2D cuerpoCollider;
    private SpriteRenderer spriteRenderer;

    private EnemigoVida enemigoVida;
    private SapoSensorSuelo sensorSuelo;
    private SapoCoordinacionGrupo coordinacion;

    private EspadaHitbox espadaJugador;
    private Collider2D espadaColliderJugador;

    // =========================================================
    // ESTADO
    // =========================================================

    private bool estaEnSuelo;
    private bool estabaEnSuelo;

    private float siguienteDecision;
    private float siguienteSaltoPermitido;
    private float siguientePisotonPermitido;

    private float ignorarContactoHasta;

    private bool procesandoPisoton;

    private float tiempoRetrocesoRestante;
    private float direccionRetroceso;

    private Coroutine coroutineParpadeo;
    private Coroutine coroutineSquash;

    private Vector3 escalaVisualOriginal;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        cuerpoCollider =
            GetComponent<Collider2D>();

        spriteRenderer =
            GetComponent<SpriteRenderer>();

        enemigoVida =
            GetComponent<EnemigoVida>();

        sensorSuelo =
            GetComponent<SapoSensorSuelo>();

        coordinacion =
            GetComponent<SapoCoordinacionGrupo>();

        rb.freezeRotation = true;

        if (visualParaSquash == null)
        {
            visualParaSquash =
                spriteRenderer.transform;
        }

        escalaVisualOriginal =
            visualParaSquash.localScale;
    }

    private void OnEnable()
    {
        if (enemigoVida == null)
        {
            enemigoVida =
                GetComponent<EnemigoVida>();
        }

        if (enemigoVida != null)
        {
            enemigoVida.OnDanoRecibido +=
                AlRecibirDano;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
    }

    private void OnDisable()
    {
        if (enemigoVida != null)
        {
            enemigoVida.OnDanoRecibido -=
                AlRecibirDano;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        if (visualParaSquash != null)
        {
            visualParaSquash.localScale =
                escalaVisualOriginal;
        }
    }

    private void Start()
    {
        BuscarJugador();

        float pausaInicial =
            Random.Range(
                pausaMinimaAterrizaje,
                pausaMaximaAterrizaje
            );

        pausaInicial *=
            ObtenerMultiplicadorPausa();

        siguienteSaltoPermitido =
            Time.time +
            pausaInicial;

        siguienteDecision =
            Time.time +
            Random.Range(
                0f,
                0.15f
            );

        if (sensorSuelo != null)
        {
            estaEnSuelo =
                sensorSuelo.EnSuelo;

            estabaEnSuelo =
                estaEnSuelo;
        }
    }

    private void Update()
    {
        // =====================================================
        // SUELO
        // =====================================================

        ActualizarEstadoSuelo();

        // =====================================================
        // DIRECCIÓN VISUAL
        // =====================================================

        ActualizarDireccionVisual();

        // =====================================================
        // RETROCESO
        // =====================================================

        if (tiempoRetrocesoRestante > 0f)
        {
            tiempoRetrocesoRestante -=
                Time.deltaTime;

            return;
        }

        // =====================================================
        // JUGADOR
        // =====================================================

        if (jugador == null)
        {
            BuscarJugador();

            if (jugador == null)
                return;
        }

        // =====================================================
        // RADIO DE ATENCIÓN
        // =====================================================

        float distanciaJugador =
            Vector2.Distance(
                transform.position,
                jugador.position
            );

        if (distanciaJugador >
            radioAtencion)
        {
            return;
        }

        // =====================================================
        // SOLO PIENSA EN EL SUELO
        // =====================================================

        if (!estaEnSuelo)
            return;

        // =====================================================
        // INTERVALO ENTRE DECISIONES
        // =====================================================

        if (Time.time <
            siguienteDecision)
        {
            return;
        }

        float agresividad =
            ObtenerMultiplicadorAgresividad();

        float intervaloReal =
            intervaloDecision /
            Mathf.Max(
                0.1f,
                agresividad
            );

        siguienteDecision =
            Time.time +
            intervaloReal;

        // =====================================================
        // COOLDOWN DE SALTO
        // =====================================================

        if (Time.time <
            siguienteSaltoPermitido)
        {
            return;
        }

        TomarDecision();
    }

    private void FixedUpdate()
    {
        // =====================================================
        // RETROCESO POR ESPADA
        // =====================================================

        if (tiempoRetrocesoRestante > 0f)
        {
            rb.velocity =
                new Vector2(
                    direccionRetroceso *
                    velocidadRetroceso,

                    rb.velocity.y
                );

            return;
        }

        // =====================================================
        // NO DESLIZARSE EN IDLE
        // =====================================================

        if (estaEnSuelo &&
            Mathf.Abs(rb.velocity.y) <
            0.1f)
        {
            rb.velocity =
                new Vector2(
                    0f,
                    rb.velocity.y
                );
        }
    }

    // =========================================================
    // SUELO
    // =========================================================

    private void ActualizarEstadoSuelo()
    {
        if (sensorSuelo == null)
        {
            estaEnSuelo = false;
            return;
        }

        bool sueloAhora =
            sensorSuelo.EnSuelo;

        // =====================================================
        // ACABA DE ATERRIZAR
        // =====================================================

        if (sueloAhora &&
            !estabaEnSuelo)
        {
            float pausa =
                Random.Range(
                    pausaMinimaAterrizaje,
                    pausaMaximaAterrizaje
                );

            /*
             * Cuantos más sapos haya,
             * menor será su pausa grupal.
             */
            pausa *=
                ObtenerMultiplicadorPausa();

            siguienteSaltoPermitido =
                Mathf.Max(
                    siguienteSaltoPermitido,
                    Time.time + pausa
                );

            siguienteDecision =
                Mathf.Max(
                    siguienteDecision,
                    Time.time + pausa
                );
        }

        estaEnSuelo =
            sueloAhora;

        estabaEnSuelo =
            sueloAhora;
    }

    // =========================================================
    // MULTIPLICADORES DE COORDINACIÓN
    // =========================================================

    private float ObtenerMultiplicadorPausa()
    {
        if (coordinacion == null)
            return 1f;

        return
            Mathf.Max(
                0.1f,
                coordinacion.MultiplicadorPausa
            );
    }

    private float ObtenerMultiplicadorAgresividad()
    {
        if (coordinacion == null)
            return 1f;

        return
            Mathf.Max(
                0.1f,
                coordinacion.MultiplicadorAgresividad
            );
    }

    // =========================================================
    // JUGADOR
    // =========================================================

    private void BuscarJugador()
    {
        GameObject obj =
            GameObject.FindGameObjectWithTag(
                playerTag
            );

        if (obj == null)
            return;

        jugador =
            obj.transform;

        rbJugador =
            obj.GetComponent<Rigidbody2D>();

        if (rbJugador == null)
        {
            rbJugador =
                obj.GetComponentInChildren<Rigidbody2D>();
        }

        espadaJugador =
            obj.GetComponentInChildren<EspadaHitbox>(
                true
            );

        if (espadaJugador != null)
        {
            espadaColliderJugador =
                espadaJugador
                    .GetComponent<Collider2D>();
        }
    }

    // =========================================================
    // DECISIÓN PRINCIPAL
    // =========================================================

    private void TomarDecision()
    {
        if (jugador == null)
            return;

        // =====================================================
        // EVASIÓN SIEMPRE TIENE PRIORIDAD
        // =====================================================

        /*
         * Incluso un sapo que actualmente está flanqueando
         * puede defenderse de una espada.
         */
        if (EspadaActivaCerca())
        {
            if (Random.value <=
                probabilidadEvasionEspada)
            {
                EjecutarEvasion();
                return;
            }
        }

        /*
         * También puede intentar evitar un pisotón
         * aunque actualmente no sea su turno ofensivo.
         */
        if (JugadorIntentandoPisoton())
        {
            if (Random.value <=
                probabilidadEvasionPisoton)
            {
                EjecutarEvasion();
                return;
            }
        }

        float distancia =
            Mathf.Abs(
                jugador.position.x -
                transform.position.x
            );

        // =====================================================
        // OBJETIVO DEL CEREBRO GRUPAL
        // =====================================================

        float objetivoX =
            coordinacion != null
                ? coordinacion.ObtenerObjetivoX()
                : jugador.position.x;

        float direccionObjetivo =
            Mathf.Sign(
                objetivoX -
                transform.position.x
            );

        if (Mathf.Abs(
            direccionObjetivo) < 0.01f)
        {
            direccionObjetivo =
                DireccionHaciaJugador();
        }

        // =====================================================
        // ¿ES SU TURNO DE ATACAR?
        // =====================================================

        bool puedeAtacar =
            coordinacion == null ||
            coordinacion.PuedeAtacarAhora;

        SapoCoordinacionGrupo.SaltoTactico
            saltoSugerido =
                coordinacion != null
                    ? coordinacion.ObtenerSaltoSugerido(
                        distancia
                    )
                    : SapoCoordinacionGrupo
                        .SaltoTactico.Libre;

        // =====================================================
        // NO ES SU TURNO: REPOSICIONARSE
        // =====================================================

        if (!puedeAtacar)
        {
            TipoSalto saltoReposicion =
                ConvertirSaltoTactico(
                    saltoSugerido,
                    distancia
                );

            EjecutarSalto(
                saltoReposicion,
                direccionObjetivo,
                false
            );

            return;
        }

        // =====================================================
        // ATAQUE COORDINADO
        // =====================================================

        if (saltoSugerido !=
            SapoCoordinacionGrupo
                .SaltoTactico.Libre)
        {
            TipoSalto saltoAtaque =
                ConvertirSaltoTactico(
                    saltoSugerido,
                    distancia
                );

            EjecutarSalto(
                saltoAtaque,
                direccionObjetivo,
                true
            );

            return;
        }

        // =====================================================
        // DUELISTA / COMPORTAMIENTO LIBRE
        // =====================================================

        TomarDecisionIndividual(
            distancia,
            direccionObjetivo
        );
    }

    // =========================================================
    // DECISIÓN INDIVIDUAL
    // =========================================================

    private void TomarDecisionIndividual(
        float distancia,
        float direccionObjetivo)
    {
        // =====================================================
        // JUGADOR MUY CERCA
        // =====================================================

        if (distancia <=
            distanciaMuyCerca)
        {
            /*
             * Si el jugador viene corriendo hacia él,
             * intenta saltarlo por encima.
             */
            if (JugadorSeAcercaRapido())
            {
                EjecutarSalto(
                    TipoSalto.Grande,
                    DireccionHaciaJugador(),
                    true
                );

                return;
            }

            if (Random.value < 0.65f)
            {
                EjecutarSalto(
                    TipoSalto.Pequeno,
                    direccionObjetivo,
                    true
                );
            }
            else
            {
                EjecutarSalto(
                    TipoSalto.Medio,
                    direccionObjetivo,
                    true
                );
            }

            return;
        }

        // =====================================================
        // DISTANCIA MEDIA
        // =====================================================

        if (distancia <=
            distanciaMedia)
        {
            EjecutarSalto(
                TipoSalto.Medio,
                direccionObjetivo,
                true
            );

            return;
        }

        // =====================================================
        // LEJOS
        // =====================================================

        EjecutarSalto(
            TipoSalto.Pequeno,
            direccionObjetivo,
            true
        );
    }

    // =========================================================
    // SALTO TÁCTICO → SALTO REAL
    // =========================================================

    private TipoSalto ConvertirSaltoTactico(
        SapoCoordinacionGrupo.SaltoTactico salto,
        float distanciaJugador)
    {
        switch (salto)
        {
            case SapoCoordinacionGrupo
                .SaltoTactico.Pequeno:

                return
                    TipoSalto.Pequeno;

            case SapoCoordinacionGrupo
                .SaltoTactico.Medio:

                return
                    TipoSalto.Medio;

            case SapoCoordinacionGrupo
                .SaltoTactico.Grande:

                return
                    TipoSalto.Grande;

            default:

                /*
                 * Si el coordinador dice "Libre",
                 * elegimos algo razonable por distancia.
                 */
                if (distanciaJugador <=
                    distanciaMuyCerca)
                {
                    return TipoSalto.Pequeno;
                }

                if (distanciaJugador <=
                    distanciaMedia)
                {
                    return TipoSalto.Medio;
                }

                return TipoSalto.Pequeno;
        }
    }

    // =========================================================
    // EVASIÓN
    // =========================================================

    private bool EspadaActivaCerca()
    {
        if (espadaColliderJugador == null)
            return false;

        if (!espadaColliderJugador.enabled)
            return false;

        float distancia =
            Vector2.Distance(
                transform.position,
                espadaColliderJugador
                    .bounds.center
            );

        return
            distancia <=
            radioEvasionEspada;
    }

    private bool JugadorIntentandoPisoton()
    {
        if (jugador == null ||
            rbJugador == null)
        {
            return false;
        }

        bool vieneCayendo =
            rbJugador.velocity.y <=
            velocidadCaidaAmenazante;

        bool estaArriba =
            jugador.position.y >
            transform.position.y;

        float diferenciaX =
            Mathf.Abs(
                jugador.position.x -
                transform.position.x
            );

        return
            vieneCayendo &&
            estaArriba &&
            diferenciaX <=
            radioEvasionPisoton;
    }

    private void EjecutarEvasion()
    {
        if (jugador == null)
            return;

        float haciaJugador =
            DireccionHaciaJugador();

        float lejosJugador =
            -haciaJugador;

        float distancia =
            Mathf.Abs(
                jugador.position.x -
                transform.position.x
            );

        bool puedePasarPorEncima =
            distancia <=
            distanciaMuyCerca *
            1.4f;

        /*
         * Algunas veces evade saltando por encima.
         * Otras simplemente escapa en dirección opuesta.
         */
        if (puedePasarPorEncima &&
            Random.value <
            probabilidadSaltarSobreJugador)
        {
            EjecutarSalto(
                TipoSalto.Grande,
                haciaJugador,
                false
            );
        }
        else
        {
            EjecutarSalto(
                TipoSalto.Grande,
                lejosJugador,
                false
            );
        }
    }

    private bool JugadorSeAcercaRapido()
    {
        if (jugador == null ||
            rbJugador == null)
        {
            return false;
        }

        float diferencia =
            jugador.position.x -
            transform.position.x;

        float velocidad =
            rbJugador.velocity.x;

        return
            diferencia *
            velocidad < -0.5f;
    }

    // =========================================================
    // SALTAR
    // =========================================================

    private void EjecutarSalto(
        TipoSalto tipo,
        float direccion,
        bool cuentaComoAtaque)
    {
        if (!estaEnSuelo)
            return;

        if (Mathf.Abs(direccion) <
            0.01f)
        {
            direccion =
                DireccionHaciaJugador();
        }

        direccion =
            Mathf.Sign(
                direccion
            );

        float fuerzaHorizontal;
        float fuerzaVertical;

        switch (tipo)
        {
            case TipoSalto.Pequeno:

                fuerzaHorizontal =
                    pequenoHorizontal;

                fuerzaVertical =
                    pequenoVertical;

                break;

            case TipoSalto.Medio:

                fuerzaHorizontal =
                    medioHorizontal;

                fuerzaVertical =
                    medioVertical;

                break;

            default:

                fuerzaHorizontal =
                    grandeHorizontal;

                fuerzaVertical =
                    grandeVertical;

                break;
        }

        // =====================================================
        // BUSCAR DIRECCIÓN SEGURA
        // =====================================================

        direccion =
            BuscarDireccionSegura(
                direccion,
                tipo
            );

        rb.velocity =
            new Vector2(
                direccion *
                fuerzaHorizontal,

                fuerzaVertical
            );

        /*
         * Evita que en el mismo frame siga pensando
         * que está apoyado en el suelo.
         */
        estaEnSuelo = false;
        estabaEnSuelo = false;

        // =====================================================
        // COOLDOWN
        // =====================================================

        float variacion =
            Random.Range(
                -variacionTiempoSalto,
                variacionTiempoSalto
            );

        float cooldown =
            Mathf.Max(
                0.1f,
                tiempoMinimoEntreSaltos +
                variacion
            );

        cooldown *=
            ObtenerMultiplicadorPausa();

        siguienteSaltoPermitido =
            Time.time +
            cooldown;

        // =====================================================
        // NOTIFICAR AL COORDINADOR
        // =====================================================

        if (cuentaComoAtaque &&
            coordinacion != null)
        {
            coordinacion
                .NotificarAtaqueRealizado();
        }
    }

    // =========================================================
    // SEGURIDAD DEL SALTO
    // =========================================================

    private float BuscarDireccionSegura(
        float direccionDeseada,
        TipoSalto tipo)
    {
        float distanciaEsperada;

        switch (tipo)
        {
            case TipoSalto.Pequeno:

                distanciaEsperada =
                    distanciaChequeoAterrizaje *
                    0.45f;

                break;

            case TipoSalto.Medio:

                distanciaEsperada =
                    distanciaChequeoAterrizaje *
                    0.75f;

                break;

            default:

                distanciaEsperada =
                    distanciaChequeoAterrizaje;

                break;
        }

        if (HaySueloEnDireccion(
            direccionDeseada,
            distanciaEsperada))
        {
            return direccionDeseada;
        }

        float direccionOpuesta =
            -direccionDeseada;

        if (HaySueloEnDireccion(
            direccionOpuesta,
            distanciaEsperada))
        {
            return direccionOpuesta;
        }

        /*
         * Si no encuentra suelo seguro a ninguno de
         * los lados, hace un salto vertical.
         */
        return 0f;
    }

    private bool HaySueloEnDireccion(
        float direccion,
        float distancia)
    {
        Vector2 origen =
            new Vector2(
                rb.position.x +
                direccion *
                distancia,

                cuerpoCollider
                    .bounds.max.y +
                0.3f
            );

        RaycastHit2D[] hits =
            Physics2D.RaycastAll(
                origen,
                Vector2.down,
                alturaChequeoSuelo,
                groundLayer
            );

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null)
                continue;

            // Propio Rigidbody.
            if (hit.collider.attachedRigidbody == rb)
                continue;

            // Propio objeto.
            if (hit.collider.transform == transform)
                continue;

            // Hijos propios.
            if (hit.collider.transform.IsChildOf(transform))
                continue;

            // ============================================
            // IGNORAR OTROS SAPOS
            // ============================================

            SapoEnemy otroSapo =
                hit.collider.GetComponentInParent<SapoEnemy>();

            if (otroSapo != null)
                continue;

            return true;
        }

        return false;
    }

    private float DireccionHaciaJugador()
    {
        if (jugador == null)
            return 1f;

        float diferencia =
            jugador.position.x -
            transform.position.x;

        if (Mathf.Abs(diferencia) <
            0.01f)
        {
            return 1f;
        }

        return
            Mathf.Sign(
                diferencia
            );
    }

    // =========================================================
    // PISOTÓN
    // =========================================================

    public bool IntentarProcesarPisoton(
        Collider2D jugadorCollider)
    {
        if (jugadorCollider == null)
            return false;

        if (Time.time <
            siguientePisotonPermitido)
        {
            return false;
        }

        Transform raizJugador =
            jugadorCollider
                .transform.root;

        if (!raizJugador.CompareTag(
            playerTag))
        {
            return false;
        }

        Rigidbody2D jugadorRb =
            jugadorCollider
                .attachedRigidbody;

        if (jugadorRb == null)
        {
            jugadorRb =
                raizJugador
                    .GetComponent<Rigidbody2D>();
        }

        if (jugadorRb == null)
            return false;

        // =====================================================
        // DEBE VENIR CAYENDO
        // =====================================================

        if (jugadorRb.velocity.y >
            0.2f)
        {
            return false;
        }

        // =====================================================
        // DEBE ESTAR POR ENCIMA
        // =====================================================

        if (jugadorCollider
                .bounds.center.y <=
            cuerpoCollider
                .bounds.center.y)
        {
            return false;
        }

        // =====================================================
        // ACTIVAR COOLDOWN
        // =====================================================

        siguientePisotonPermitido =
            Time.time +
            cooldownPisoton;

        ignorarContactoHasta =
            Time.time +
            tiempoIgnorarContactoTrasPisoton;

        // =====================================================
        // DAÑO AL SAPO
        // =====================================================

        procesandoPisoton = true;

        DamageInfo damageInfo =
            new DamageInfo(
                danoPisoton,
                raizJugador.gameObject,
                jugadorCollider
                    .ClosestPoint(
                        transform.position
                    ),
                Vector2.down,
                0f
            );

        enemigoVida.RecibirDano(
            damageInfo
        );

        procesandoPisoton = false;

        // =====================================================
        // REBOTE
        // =====================================================

        jugadorRb.velocity =
            new Vector2(
                jugadorRb.velocity.x,
                fuerzaReboteJugador
            );

        // =====================================================
        // SQUASH
        // =====================================================

        IniciarSquash();

        return true;
    }

    // =========================================================
    // FILTRO DE ENEMIGOCONTACTO
    // =========================================================

    public bool PuedeDañarPorContacto(
        Collider2D objetivo)
    {
        if (objetivo == null)
            return true;

        // =====================================================
        // PROTECCIÓN TRAS PISOTÓN
        // =====================================================

        if (Time.time <
            ignorarContactoHasta)
        {
            return false;
        }

        Transform raiz =
            objetivo.transform.root;

        if (!raiz.CompareTag(playerTag))
            return true;

        Rigidbody2D objetivoRb =
            objetivo
                .attachedRigidbody;

        if (objetivoRb == null)
        {
            objetivoRb =
                raiz
                    .GetComponent<Rigidbody2D>();
        }

        if (objetivoRb == null)
            return true;

        // =====================================================
        // ¿VIENE CAYENDO?
        // =====================================================

        bool cayendo =
            objetivoRb.velocity.y <=
            0.2f;

        if (!cayendo)
            return true;

        // =====================================================
        // POSICIÓN VERTICAL
        // =====================================================

        float piesJugador =
            objetivo.bounds.min.y;

        float parteSuperiorSapo =
            cuerpoCollider.bounds.max.y;

        bool estaPorEncima =
            objetivo.bounds.center.y >
            cuerpoCollider.bounds.center.y;

        bool cercaDeLaCabeza =
            piesJugador >=
            parteSuperiorSapo -
            margenCabezaPisoton;

        // =====================================================
        // SOLAPAMIENTO HORIZONTAL
        // =====================================================

        bool haySolapamientoHorizontal =
            objetivo.bounds.max.x >
            cuerpoCollider.bounds.min.x &&

            objetivo.bounds.min.x <
            cuerpoCollider.bounds.max.x;

        // =====================================================
        // PISOTÓN
        // =====================================================

        if (estaPorEncima &&
            cercaDeLaCabeza &&
            haySolapamientoHorizontal)
        {
            /*
             * Procesamos aquí mismo el pisotón.
             *
             * Así no importa si Unity ejecuta primero
             * EnemigoContacto o StompZone.
             */
            IntentarProcesarPisoton(
                objetivo
            );

            /*
             * Bajo ninguna circunstancia el jugador
             * recibe daño de contacto en este caso.
             */
            return false;
        }

        return true;
    }

    // =========================================================
    // RECIBIR DAÑO
    // =========================================================

    private void AlRecibirDano(
        DamageInfo damageInfo)
    {
        IniciarParpadeo();

        /*
         * Pisotón:
         * feedback + squash,
         * pero sin retroceso.
         */
        if (procesandoPisoton)
            return;

        // =====================================================
        // RETROCESO POR ESPADA
        // =====================================================

        float direccion =
            damageInfo.direccion.x;

        if (Mathf.Abs(direccion) <
            0.01f &&
            damageInfo.atacante != null)
        {
            direccion =
                transform.position.x -
                damageInfo
                    .atacante
                    .transform.position.x;
        }

        if (Mathf.Abs(direccion) <
            0.01f)
        {
            direccion = 1f;
        }

        direccionRetroceso =
            Mathf.Sign(
                direccion
            );

        tiempoRetrocesoRestante =
            duracionRetroceso;
    }

    // =========================================================
    // PARPADEO
    // =========================================================

    private void IniciarParpadeo()
    {
        if (coroutineParpadeo != null)
        {
            StopCoroutine(
                coroutineParpadeo
            );
        }

        coroutineParpadeo =
            StartCoroutine(
                Parpadear()
            );
    }

    private IEnumerator Parpadear()
    {
        for (int i = 0;
             i < cantidadParpadeos;
             i++)
        {
            spriteRenderer.enabled =
                false;

            yield return
                new WaitForSeconds(
                    tiempoParpadeo
                );

            spriteRenderer.enabled =
                true;

            yield return
                new WaitForSeconds(
                    tiempoParpadeo
                );
        }

        spriteRenderer.enabled =
            true;

        coroutineParpadeo = null;
    }

    // =========================================================
    // SQUASH
    // =========================================================

    private void IniciarSquash()
    {
        if (visualParaSquash == null)
            return;

        if (coroutineSquash != null)
        {
            StopCoroutine(
                coroutineSquash
            );

            visualParaSquash.localScale =
                escalaVisualOriginal;
        }

        coroutineSquash =
            StartCoroutine(
                HacerSquash()
            );
    }

    private IEnumerator HacerSquash()
    {
        Vector3 escalaAplastada =
            new Vector3(
                escalaVisualOriginal.x *
                squashX,

                escalaVisualOriginal.y *
                squashY,

                escalaVisualOriginal.z
            );

        visualParaSquash.localScale =
            escalaAplastada;

        float mitad =
            duracionSquash *
            0.5f;

        yield return
            new WaitForSeconds(
                mitad
            );

        float tiempo = 0f;

        while (tiempo < mitad)
        {
            tiempo +=
                Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    tiempo / mitad
                );

            visualParaSquash.localScale =
                Vector3.Lerp(
                    escalaAplastada,
                    escalaVisualOriginal,
                    t
                );

            yield return null;
        }

        visualParaSquash.localScale =
            escalaVisualOriginal;

        coroutineSquash = null;
    }

    // =========================================================
    // DIRECCIÓN DEL SPRITE
    // =========================================================

    private void ActualizarDireccionVisual()
    {
        float direccion;

        /*
         * Mientras se mueve,
         * mira hacia su desplazamiento.
         */
        if (Mathf.Abs(rb.velocity.x) >
            0.1f)
        {
            direccion =
                rb.velocity.x;
        }

        /*
         * Cuando está quieto,
         * mira hacia el jugador.
         */
        else if (jugador != null)
        {
            direccion =
                jugador.position.x -
                transform.position.x;
        }
        else
        {
            return;
        }

        bool mirarDerecha =
            direccion > 0f;

        if (spriteMiraDerecha)
        {
            spriteRenderer.flipX =
                !mirarDerecha;
        }
        else
        {
            spriteRenderer.flipX =
                mirarDerecha;
        }
    }

    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color =
            Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            radioAtencion
        );

        /*
         * El círculo cyan de coordinación ya lo dibuja
         * SapoCoordinacionGrupo.
         */
    }
}