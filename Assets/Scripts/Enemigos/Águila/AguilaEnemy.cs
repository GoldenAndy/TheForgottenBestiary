using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(EnemigoVida))]
[RequireComponent(typeof(EnemigoContacto))]
public class AguilaEnemy : MonoBehaviour, IFiltroDanoContacto
{
    public enum EstadoAguila
    {
        Esperando,
        Patrulla,

        // NUEVO:
        // Ya decidió atacar, pero todavía está buscando
        // un buen ángulo lateral.
        PreparandoPicada,

        Picada,
        Retirada,
        Evasion,
        Golpeada,
        DuoOrbita,
        DuoLanzamiento,
        DuoHostigamiento
    }


    // =========================================================
    // REFERENCIAS
    // =========================================================

    [Header("Referencias")]
    [SerializeField] private Transform jugador;


    // =========================================================
    // ZONA DEL ÁGUILA
    // =========================================================

    [Header("Zona de activación")]

    [Tooltip(
        "Distancia desde el lugar donde colocaste el águila " +
        "a la que detectará al jugador."
    )]
    [SerializeField]
    private float rangoDeteccion = 7f;

    [Tooltip(
        "Distancia que puede alejarse el jugador antes de que " +
        "el águila abandone la persecución."
    )]
    [SerializeField]
    private float rangoAbandono = 10f;

    [Tooltip(
        "Ancho de patrulla cuando todavía no ha detectado al jugador."
    )]
    [SerializeField]
    private float anchoPatrullaReposo = 3f;


    // =========================================================
    // VUELO NORMAL
    // =========================================================

    [Header("Vuelo normal")]

    [Tooltip(
        "Ancho dentro del cual se mueve alrededor del jugador " +
        "una vez que comienza el combate."
    )]
    [SerializeField]
    private float anchoPatrullaCombate = 6f;

    [SerializeField]
    private float velocidadPatrulla = 2.5f;

    [SerializeField]
    private float velocidadAjusteAltura = 3f;


    // =========================================================
    // ATAQUE
    // =========================================================

    [Header("Ataque en picada")]

    [SerializeField]
    private float tiempoMinimoEntreAtaques = 1.5f;

    [SerializeField]
    private float tiempoMaximoEntreAtaques = 2.8f;

    [SerializeField]
    private float velocidadPicada = 8.5f;

    [Tooltip(
        "Predicción de movimiento del jugador durante la picada."
    )]
    [SerializeField]
    private float anticipacionJugador = 0.20f;

    [SerializeField]
    private float tiempoMaximoPicada = 1.6f;


    // =========================================================
    // PREPARACIÓN LATERAL DE PICADA
    // =========================================================

    [Header("Preparación lateral de picada")]

    [Tooltip(
        "Distancia lateral mínima respecto al jugador que debe " +
        "alcanzar el águila antes de iniciar una picada."
    )]
    [SerializeField]
    private float distanciaLateralMinimaPicada = 2.6f;

    [Tooltip(
        "Distancia lateral máxima que intentará alcanzar mientras " +
        "busca un buen ángulo. Evita que persiga al jugador demasiado lejos."
    )]
    [SerializeField]
    private float distanciaLateralMaximaPicada = 4.5f;

    [Tooltip(
        "Ángulo máximo de la picada medido desde la horizontal. " +
        "Un valor menor hace los ataques más laterales. " +
        "45-55 grados suele funcionar bien."
    )]
    [Range(15f, 80f)]
    [SerializeField]
    private float anguloMaximoPicadaDesdeHorizontal = 50f;

    [Tooltip(
        "Velocidad mientras se coloca para conseguir el ángulo. " +
        "Conviene que sea parecida a Velocidad Patrulla para que " +
        "el movimiento no se vea robótico."
    )]
    [SerializeField]
    private float velocidadPreparacionPicada = 2.8f;

    [Tooltip(
        "Tiempo máximo que intentará buscar el ángulo. " +
        "Si no puede conseguirlo, cancela el intento y sigue patrullando."
    )]
    [SerializeField]
    private float tiempoMaximoPreparacionPicada = 2.5f;

    [Tooltip(
        "Margen para considerar que llegó aproximadamente " +
        "al punto lateral deseado."
    )]
    [SerializeField]
    private float margenPreparacionPicada = 0.25f;


    // =========================================================
    // SUELO
    // =========================================================

    [Header("Detección del suelo")]

    [SerializeField]
    private LayerMask capaSuelo;

    [SerializeField]
    private float distanciaMinimaSuelo = 0.7f;


    // =========================================================
    // RETIRADA
    // =========================================================

    [Header("Retirada")]

    [SerializeField]
    private float velocidadRetirada = 6.5f;

    [SerializeField]
    private float distanciaLateralRetirada = 2.5f;

    [SerializeField]
    private float distanciaLlegada = 0.25f;


    // =========================================================
    // EVASIÓN
    // =========================================================

    [Header("Evasión de espada")]

    [SerializeField]
    private float velocidadEvasion = 10f;

    [SerializeField]
    private float distanciaEvasionHorizontal = 3f;

    [SerializeField]
    private float distanciaEvasionVertical = 2.2f;


    // =========================================================
    // GOLPE RECIBIDO
    // =========================================================

    [Header("Reacción al recibir golpe")]

    [SerializeField]
    private float tiempoGolpeada = 0.35f;

    [Tooltip(
        "Fuerza horizontal con la que sale despedida al recibir daño."
    )]
    [SerializeField]
    private float retrocesoGolpe = 5.5f;

    [Tooltip(
        "Pequeño impulso vertical al recibir daño."
    )]
    [SerializeField]
    private float impulsoVerticalGolpe = 1.2f;

    [Tooltip(
        "Qué tan rápido pierde velocidad después del golpe."
    )]
    [SerializeField]
    private float frenadoTrasGolpe = 10f;


    // =========================================================
    // CONTROL DE DÚO / JEFE
    // =========================================================

    private bool modoJefeDuoActivo;

    /*
     * Se activa cuando esta águila formaba parte del combate
     * de jefe y su compañera fue derrotada.
     *
     * Desde ese momento conserva la IA individual normal,
     * pero NUNCA abandona el combate por distancia.
     */
    private bool jefeSolitarioPersistente;

    private Vector2 duoCentroOrbita;
    private float duoRadioOrbita;
    private float duoVelocidadAngular;
    private float duoAnguloActual;
    private float duoVueltasActuales;
    private int duoVueltasObjetivo;

    private Vector2 duoDestino;
    private float duoVelocidadMovimiento;
    private bool duoIgnorarEvasion;


    // =========================================================
    // ANIMATOR
    // =========================================================

    [Header("Animator")]

    [SerializeField]
    private string estadoIdle = "Aguila_Idle";

    [SerializeField]
    private string estadoAtaque = "Aguila_Ataque";

    [SerializeField]
    private string estadoGolpeada = "Aguila_Golpeada";


    // =========================================================
    // COMPONENTES
    // =========================================================

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private EnemigoContacto contacto;

    private Rigidbody2D jugadorRb;
    private Camera camara;


    // =========================================================
    // POSICIÓN ORIGINAL
    // =========================================================

    private Vector2 posicionInicial;

    /*
     * Altura original donde fue colocada el águila.
     */
    private float alturaBaseY;


    // =========================================================
    // ESTADO
    // =========================================================

    private EstadoAguila estadoActual;

    private bool combateActivo;

    private float direccionPatrulla = -1f;

    private float tiempoHastaAtaque;

    private float tiempoActualPicada;

    private float tiempoRestanteGolpeada;

    private Vector2 objetivoPicada;

    private Vector2 objetivoRetirada;

    private Vector2 objetivoEvasion;


    // =========================================================
    // PREPARACIÓN DE PICADA
    // =========================================================

    /*
     * -1 = atacar desde izquierda.
     *  1 = atacar desde derecha.
     */
    private float ladoPreparacionPicada;

    private float tiempoActualPreparacionPicada;


    // =========================================================
    // EMPUJE ESPECIAL
    // =========================================================

    private Vector2 impulsoGolpePendiente;

    private bool debeAplicarImpulsoGolpe;


    // =========================================================
    // PROPIEDADES PÚBLICAS
    // =========================================================

    public EstadoAguila EstadoActual =>
        estadoActual;

    public bool EstaEnPicada =>
        estadoActual == EstadoAguila.Picada;

    public bool EstaPatrullando =>
        estadoActual == EstadoAguila.Patrulla;

    public bool CombateActivo =>
        combateActivo;

    public bool EsJefeSolitario =>
        jefeSolitarioPersistente;

    public float AlturaBaseY =>
        alturaBaseY;

    public Vector2 PosicionInicial =>
        posicionInicial;

    public bool EstaBajoControlDuo =>
        estadoActual == EstadoAguila.DuoOrbita ||
        estadoActual == EstadoAguila.DuoLanzamiento ||
        estadoActual == EstadoAguila.DuoHostigamiento;

    public bool EstaEnLanzamientoDuo =>
        estadoActual == EstadoAguila.DuoLanzamiento;

    public bool DuoIgnoraEvasion =>
        duoIgnorarEvasion;

    public bool EstaEnOrbitaDuo =>
        estadoActual == EstadoAguila.DuoOrbita;

    public bool TerminoVueltasDuo =>
        estadoActual == EstadoAguila.DuoOrbita &&
        duoVueltasActuales >= duoVueltasObjetivo;

    public bool PuedeSerDirigidaPorPareja =>
        estadoActual != EstadoAguila.Golpeada &&
        estadoActual != EstadoAguila.Evasion;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        rb =
            GetComponent<Rigidbody2D>();

        spriteRenderer =
            GetComponent<SpriteRenderer>();

        animator =
            GetComponent<Animator>();

        contacto =
            GetComponent<EnemigoContacto>();

        posicionInicial =
            transform.position;

        alturaBaseY =
            transform.position.y;

        rb.gravityScale = 0f;

        rb.freezeRotation = true;

        rb.collisionDetectionMode =
            CollisionDetectionMode2D.Continuous;

        rb.interpolation =
            RigidbodyInterpolation2D.Interpolate;

        camara =
            Camera.main;
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (jugador == null)
        {
            GameObject objetoJugador =
                GameObject.FindGameObjectWithTag(
                    "Player"
                );

            if (objetoJugador != null)
            {
                jugador =
                    objetoJugador.transform;
            }
        }

        if (jugador == null)
        {
            Debug.LogError(
                $"{name}: No se encontró al jugador."
            );

            enabled = false;

            return;
        }

        jugadorRb =
            jugador.GetComponent<Rigidbody2D>();

        combateActivo =
            false;

        estadoActual =
            EstadoAguila.Esperando;

        direccionPatrulla =
            Random.value < 0.5f
                ? -1f
                : 1f;

        ReproducirIdle();

        ProgramarSiguienteAtaque();
    }


    // =========================================================
    // EVENTOS
    // =========================================================

    private void OnEnable()
    {
        if (contacto != null)
        {
            contacto.OnDanoAplicado +=
                ManejarDanoAplicadoAlJugador;
        }
    }

    private void OnDisable()
    {
        if (contacto != null)
        {
            contacto.OnDanoAplicado -=
                ManejarDanoAplicadoAlJugador;
        }
    }


    // =========================================================
    // FIXED UPDATE
    // =========================================================

    private void FixedUpdate()
    {
        if (jugador == null)
            return;

        switch (estadoActual)
        {
            case EstadoAguila.Esperando:

                ActualizarEspera();

                break;


            case EstadoAguila.Patrulla:

                ActualizarPatrulla();

                break;


            case EstadoAguila.PreparandoPicada:

                ActualizarPreparacionPicada();

                break;


            case EstadoAguila.Picada:

                ActualizarPicada();

                break;


            case EstadoAguila.Retirada:

                ActualizarRetirada();

                break;


            case EstadoAguila.Evasion:

                ActualizarEvasion();

                break;


            case EstadoAguila.Golpeada:

                ActualizarGolpeada();

                break;


            case EstadoAguila.DuoOrbita:

                ActualizarDuoOrbita();

                break;


            case EstadoAguila.DuoLanzamiento:

                ActualizarDuoLanzamiento();

                break;


            case EstadoAguila.DuoHostigamiento:

                ActualizarDuoHostigamiento();

                break;
        }
    }


    // =========================================================
    // ESPERA
    // =========================================================

    private void ActualizarEspera()
    {
        float mitad =
            anchoPatrullaReposo * 0.5f;

        float izquierda =
            posicionInicial.x - mitad;

        float derecha =
            posicionInicial.x + mitad;

        if (transform.position.x <= izquierda)
        {
            direccionPatrulla = 1f;
        }
        else if (transform.position.x >= derecha)
        {
            direccionPatrulla = -1f;
        }


        float diferenciaY =
            alturaBaseY -
            transform.position.y;

        float velocidadY =
            Mathf.Clamp(
                diferenciaY * 2f,
                -velocidadAjusteAltura,
                velocidadAjusteAltura
            );

        rb.velocity =
            new Vector2(
                direccionPatrulla *
                velocidadPatrulla,

                velocidadY
            );

        ActualizarDireccionVisual(
            rb.velocity.x
        );


        // ¿Entró el jugador al rango?
        if (JugadorDentroDeRango(
            rangoDeteccion))
        {
            ActivarCombate();
        }
    }


    // =========================================================
    // ACTIVAR COMBATE
    // =========================================================

    private void ActivarCombate()
    {
        combateActivo =
            true;

        estadoActual =
            EstadoAguila.Patrulla;

        ProgramarSiguienteAtaque();

        ReproducirIdle();

        Debug.Log(
            $"{name}: ¡Jugador detectado!"
        );
    }


    // =========================================================
    // DESACTIVAR COMBATE
    // =========================================================

    private void DesactivarCombate()
    {
        /*
         * Un águila que sobrevivió al combate de dúo ya es
         * un jefe solitario persistente.
         *
         * Aunque algún flujo intente desactivar el combate,
         * NO permitimos que vuelva a Esperando.
         */
        if (jefeSolitarioPersistente)
        {
            combateActivo =
                true;

            estadoActual =
                EstadoAguila.Patrulla;

            tiempoActualPreparacionPicada =
                0f;

            ProgramarSiguienteAtaque();

            ReproducirIdle();

            return;
        }

        combateActivo =
            false;

        estadoActual =
            EstadoAguila.Esperando;

        tiempoActualPreparacionPicada =
            0f;

        ReproducirIdle();

        Debug.Log(
            $"{name}: El jugador abandonó la zona."
        );
    }


    // =========================================================
    // PATRULLA DE COMBATE
    // =========================================================

    private void ActualizarPatrulla()
    {
        /*
         * Continúa patrullando alrededor del jugador
         * manteniendo siempre su altura original.
         */

        float centroX =
            jugador.position.x;

        float mitad =
            anchoPatrullaCombate * 0.5f;

        float limiteIzquierdo =
            centroX - mitad;

        float limiteDerecho =
            centroX + mitad;

        if (transform.position.x <=
            limiteIzquierdo)
        {
            direccionPatrulla = 1f;
        }
        else if (transform.position.x >=
                 limiteDerecho)
        {
            direccionPatrulla = -1f;
        }


        // =====================================================
        // ALTURA FIJA
        // =====================================================

        float diferenciaY =
            alturaBaseY -
            transform.position.y;

        float velocidadY =
            Mathf.Clamp(
                diferenciaY * 2f,
                -velocidadAjusteAltura,
                velocidadAjusteAltura
            );

        rb.velocity =
            new Vector2(
                direccionPatrulla *
                velocidadPatrulla,

                velocidadY
            );

        ActualizarDireccionVisual(
            rb.velocity.x
        );


        // =====================================================
        // JUGADOR SE FUE
        // =====================================================

        if (!modoJefeDuoActivo &&
            !jefeSolitarioPersistente &&
            !JugadorDentroDeRango(
                rangoAbandono
            ))
        {
            DesactivarCombate();

            return;
        }


        // =====================================================
        // TEMPORIZADOR DE ATAQUE
        // =====================================================

        tiempoHastaAtaque -=
            Time.fixedDeltaTime;

        if (tiempoHastaAtaque <= 0f &&
            EstaVisibleEnCamara())
        {
            /*
             * ANTES:
             *
             * IniciarPicada();
             *
             * AHORA:
             *
             * Primero buscamos un ángulo lateral.
             */
            IniciarPreparacionPicada();
        }
    }


    // =========================================================
    // INICIAR PREPARACIÓN DE PICADA
    // =========================================================

    private void IniciarPreparacionPicada()
    {
        estadoActual =
            EstadoAguila.PreparandoPicada;

        tiempoActualPreparacionPicada =
            0f;


        // =====================================================
        // ELEGIR EL LADO MÁS NATURAL
        // =====================================================

        float diferenciaX =
            transform.position.x -
            jugador.position.x;

        /*
         * Si ya está claramente a izquierda/derecha
         * del jugador, conserva ese lado.
         *
         * Esto evita que cruce todo el escenario
         * artificialmente solo para atacar.
         */
        if (Mathf.Abs(diferenciaX) >
            0.40f)
        {
            ladoPreparacionPicada =
                Mathf.Sign(
                    diferenciaX
                );
        }
        else
        {
            /*
             * Si está prácticamente encima del jugador,
             * continúa en la dirección en la que ya
             * venía patrullando.
             */
            ladoPreparacionPicada =
                direccionPatrulla >= 0f
                    ? 1f
                    : -1f;
        }

        ReproducirIdle();
    }


    // =========================================================
    // ACTUALIZAR PREPARACIÓN DE PICADA
    // =========================================================

    private void ActualizarPreparacionPicada()
    {
        // =====================================================
        // JUGADOR ABANDONÓ EL RANGO
        // =====================================================

        if (!modoJefeDuoActivo &&
            !jefeSolitarioPersistente &&
            !JugadorDentroDeRango(
                rangoAbandono
            ))
        {
            DesactivarCombate();

            return;
        }


        // =====================================================
        // TIEMPO MÁXIMO
        // =====================================================

        tiempoActualPreparacionPicada +=
            Time.fixedDeltaTime;

        if (tiempoActualPreparacionPicada >=
            tiempoMaximoPreparacionPicada)
        {
            CancelarPreparacionPicada();

            return;
        }


        // =====================================================
        // POSICIÓN PREVISTA DEL PLAYER
        // =====================================================

        Vector2 jugadorPredicho =
            ObtenerPosicionPredichaJugador();


        // =====================================================
        // DISTANCIA NECESARIA SEGÚN ALTURA
        // =====================================================

        float distanciaNecesaria =
            CalcularDistanciaLateralNecesaria(
                jugadorPredicho
            );

        /*
         * Impedimos que el águila se vaya demasiado lejos
         * intentando obtener un ángulo imposible.
         */
        distanciaNecesaria =
            Mathf.Min(
                distanciaNecesaria,
                distanciaLateralMaximaPicada
            );


        // =====================================================
        // X DE PREPARACIÓN
        // =====================================================

        float objetivoX =
            jugador.position.x +
            ladoPreparacionPicada *
            distanciaNecesaria;

        float diferenciaX =
            objetivoX -
            rb.position.x;


        float velocidadX = 0f;

        if (Mathf.Abs(diferenciaX) >
            margenPreparacionPicada)
        {
            velocidadX =
                Mathf.Sign(
                    diferenciaX
                ) *
                velocidadPreparacionPicada;
        }


        // =====================================================
        // MANTENER ALTURA ORIGINAL
        // =====================================================

        float diferenciaY =
            alturaBaseY -
            rb.position.y;

        float velocidadY =
            Mathf.Clamp(
                diferenciaY * 2f,
                -velocidadAjusteAltura,
                velocidadAjusteAltura
            );


        rb.velocity =
            new Vector2(
                velocidadX,
                velocidadY
            );

        ActualizarDireccionVisual(
            rb.velocity.x
        );


        // =====================================================
        // ¿YA TENEMOS UN BUEN ÁNGULO?
        // =====================================================

        if (PuedeIniciarPicadaDesdeAqui(
            jugadorPredicho))
        {
            IniciarPicada(
                jugadorPredicho
            );
        }
    }


    // =========================================================
    // DISTANCIA NECESARIA PARA EL ÁNGULO
    // =========================================================

    private float CalcularDistanciaLateralNecesaria(
        Vector2 posicionObjetivoJugador)
    {
        float diferenciaVertical =
            Mathf.Abs(
                rb.position.y -
                posicionObjetivoJugador.y
            );

        float anguloSeguro =
            Mathf.Clamp(
                anguloMaximoPicadaDesdeHorizontal,
                15f,
                80f
            );

        float tangente =
            Mathf.Tan(
                anguloSeguro *
                Mathf.Deg2Rad
            );


        /*
         * Si:
         *
         * tan(ángulo) = vertical / horizontal
         *
         * entonces:
         *
         * horizontal = vertical / tan(ángulo)
         */
        float distanciaPorAngulo =
            tangente > 0.001f
                ? diferenciaVertical / tangente
                : distanciaLateralMaximaPicada;


        return Mathf.Max(
            distanciaLateralMinimaPicada,
            distanciaPorAngulo
        );
    }


    // =========================================================
    // COMPROBAR SI EL ÁNGULO ES VÁLIDO
    // =========================================================

    private bool PuedeIniciarPicadaDesdeAqui(
        Vector2 posicionObjetivoJugador)
    {
        Vector2 diferencia =
            posicionObjetivoJugador -
            rb.position;

        float distanciaHorizontal =
            Mathf.Abs(
                diferencia.x
            );

        float distanciaVertical =
            Mathf.Abs(
                diferencia.y
            );


        // =====================================================
        // DEBE ESTAR SUFICIENTEMENTE AL COSTADO
        // =====================================================

        if (distanciaHorizontal <
            distanciaLateralMinimaPicada)
        {
            return false;
        }


        // =====================================================
        // DEBE ESTAR EN EL LADO ELEGIDO
        // =====================================================

        float ladoActual =
            Mathf.Sign(
                rb.position.x -
                posicionObjetivoJugador.x
            );

        if (ladoActual !=
            ladoPreparacionPicada)
        {
            return false;
        }


        // =====================================================
        // CALCULAR ÁNGULO DESDE LA HORIZONTAL
        // =====================================================

        float angulo =
            Mathf.Atan2(
                distanciaVertical,
                distanciaHorizontal
            ) *
            Mathf.Rad2Deg;

        if (angulo >
            anguloMaximoPicadaDesdeHorizontal)
        {
            return false;
        }


        // =====================================================
        // DEBE ESTAR VISIBLE
        // =====================================================

        if (!EstaVisibleEnCamara())
        {
            return false;
        }


        return true;
    }


    // =========================================================
    // CANCELAR PREPARACIÓN
    // =========================================================

    private void CancelarPreparacionPicada()
    {
        estadoActual =
            EstadoAguila.Patrulla;

        tiempoActualPreparacionPicada =
            0f;

        ProgramarSiguienteAtaque();

        ReproducirIdle();
    }


    // =========================================================
    // POSICIÓN PREDICHA DEL JUGADOR
    // =========================================================

    private Vector2 ObtenerPosicionPredichaJugador()
    {
        Vector2 posicionPredicha =
            jugador.position;

        if (jugadorRb != null)
        {
            posicionPredicha +=
                jugadorRb.velocity *
                anticipacionJugador;
        }

        return posicionPredicha;
    }


    // =========================================================
    // PICADA
    // =========================================================

    private void IniciarPicada(
        Vector2 posicionObjetivo)
    {
        estadoActual =
            EstadoAguila.Picada;

        tiempoActualPicada =
            0f;

        /*
         * El objetivo se congela al iniciar el ataque,
         * igual que en el comportamiento original.
         */
        objetivoPicada =
            posicionObjetivo;

        animator.CrossFade(
            estadoAtaque,
            0.05f
        );
    }


    // =========================================================
    // ACTUALIZAR PICADA
    // =========================================================

    private void ActualizarPicada()
    {
        tiempoActualPicada +=
            Time.fixedDeltaTime;

        Vector2 diferencia =
            objetivoPicada -
            rb.position;

        Vector2 direccion =
            diferencia.normalized;

        Vector2 velocidad =
            direccion *
            velocidadPicada;

        rb.velocity =
            velocidad;

        ActualizarDireccionVisual(
            velocidad.x
        );


        // =====================================================
        // SUELO
        // =====================================================

        if (SueloDemasiadoCerca())
        {
            IniciarRetirada();

            return;
        }


        // =====================================================
        // TIEMPO MÁXIMO
        // =====================================================

        if (tiempoActualPicada >=
            tiempoMaximoPicada)
        {
            IniciarRetirada();

            return;
        }


        // =====================================================
        // LLEGÓ AL OBJETIVO
        // =====================================================

        if (diferencia.magnitude <=
            0.35f)
        {
            IniciarRetirada();
        }
    }


    // =========================================================
    // SUELO
    // =========================================================

    private bool SueloDemasiadoCerca()
    {
        RaycastHit2D golpe =
            Physics2D.Raycast(
                rb.position,
                Vector2.down,
                distanciaMinimaSuelo,
                capaSuelo
            );

        return golpe.collider != null;
    }


    // =========================================================
    // RETIRADA
    // =========================================================

    private void IniciarRetirada()
    {
        duoIgnorarEvasion =
            false;

        // El jugador puede haber sido destruido mientras
        // se desactiva una de las águilas o cambia la escena.
        if (jugador == null)
        {
            if (rb != null)
            {
                rb.velocity =
                    Vector2.zero;
            }

            estadoActual =
                EstadoAguila.Esperando;

            return;
        }

        estadoActual =
            EstadoAguila.Retirada;

        float lado =
            Mathf.Sign(
                transform.position.x -
                jugador.position.x
            );

        if (lado == 0f)
        {
            lado =
                Random.value < 0.5f
                    ? -1f
                    : 1f;
        }

        objetivoRetirada =
            new Vector2(
                jugador.position.x +
                lado *
                distanciaLateralRetirada,

                alturaBaseY
            );

        ReproducirIdle();
    }


    // =========================================================
    // ACTUALIZAR RETIRADA
    // =========================================================

    private void ActualizarRetirada()
    {
        if (MoverHacia(
            objetivoRetirada,
            velocidadRetirada))
        {
            if (modoJefeDuoActivo ||
                jefeSolitarioPersistente ||
                JugadorDentroDeRango(
                    rangoAbandono
                ))
            {
                estadoActual =
                    EstadoAguila.Patrulla;

                ProgramarSiguienteAtaque();

                ReproducirIdle();
            }
            else
            {
                DesactivarCombate();
            }
        }
    }


    // =========================================================
    // EVASIÓN
    // =========================================================

    public void ForzarEvasion()
    {
        if (jugador == null)
            return;

        /*
         * Si estaba atacando, la evasión ARRUINA inmediatamente
         * ese ataque. Desde este momento deja de estar en Picada
         * y, gracias a IFiltroDanoContacto, tampoco puede hacer
         * daño por tocar al jugador mientras esquiva.
         */
        duoIgnorarEvasion = false;

        estadoActual =
            EstadoAguila.Evasion;

        tiempoActualPicada = 0f;

        float lado =
            Mathf.Sign(
                transform.position.x -
                jugador.position.x
            );

        if (Mathf.Abs(lado) <
            0.01f)
        {
            /*
             * Si está prácticamente encima del jugador,
             * elegimos el lado contrario a su movimiento
             * horizontal actual cuando sea posible.
             */
            if (Mathf.Abs(rb.velocity.x) >
                0.05f)
            {
                lado =
                    -Mathf.Sign(
                        rb.velocity.x
                    );
            }
            else
            {
                lado =
                    Random.value < 0.5f
                        ? -1f
                        : 1f;
            }
        }

        objetivoEvasion =
            rb.position +
            new Vector2(
                lado *
                distanciaEvasionHorizontal,

                distanciaEvasionVertical
            );

        /*
         * Cambiamos la velocidad YA, sin esperar al próximo
         * FixedUpdate. Así no conserva durante un instante
         * la velocidad de la picada hacia el jugador.
         */
        Vector2 direccionEvasion =
            (objetivoEvasion -
             rb.position).normalized;

        rb.velocity =
            direccionEvasion *
            velocidadEvasion;

        ActualizarDireccionVisual(
            rb.velocity.x
        );

        ReproducirIdle();
    }


    private void ActualizarEvasion()
    {
        if (MoverHacia(
            objetivoEvasion,
            velocidadEvasion))
        {
            IniciarRetirada();
        }
    }


    // =========================================================
    // GOLPE RECIBIDO
    // =========================================================

    public void RegistrarGolpeRecibido(
        DamageInfo damageInfo)
    {
        estadoActual =
            EstadoAguila.Golpeada;

        tiempoRestanteGolpeada =
            tiempoGolpeada;

        animator.CrossFade(
            estadoGolpeada,
            0.03f
        );


        // =====================================================
        // CALCULAR EMPUJE
        // =====================================================

        float direccionX = 0f;

        /*
         * Queremos que el retroceso sea SIEMPRE alejándose
         * del jugador, no alejándose de la Hitbox de la espada.
         *
         * Esto evita el caso donde una hitbox situada al otro
         * lado del águila hacía que el empuje la lanzara
         * accidentalmente HACIA el Player.
         */
        if (jugador != null)
        {
            direccionX =
                Mathf.Sign(
                    rb.position.x -
                    jugador.position.x
                );
        }

        /*
         * Si están prácticamente en la misma X, usamos
         * la dirección del DamageInfo como respaldo.
         */
        if (Mathf.Abs(direccionX) <
            0.01f)
        {
            direccionX =
                Mathf.Sign(
                    damageInfo.direccion.x
                );
        }

        /*
         * Último respaldo para un caso totalmente centrado.
         */
        if (Mathf.Abs(direccionX) <
            0.01f)
        {
            direccionX =
                Random.value < 0.5f
                    ? -1f
                    : 1f;
        }

        impulsoGolpePendiente =
            new Vector2(
                direccionX *
                retrocesoGolpe,

                impulsoVerticalGolpe
            );

        debeAplicarImpulsoGolpe =
            true;
    }


    // =========================================================
    // ACTUALIZAR GOLPEADA
    // =========================================================

    private void ActualizarGolpeada()
    {
        if (debeAplicarImpulsoGolpe)
        {
            rb.velocity =
                impulsoGolpePendiente;

            debeAplicarImpulsoGolpe =
                false;
        }
        else
        {
            rb.velocity =
                Vector2.MoveTowards(
                    rb.velocity,
                    Vector2.zero,
                    frenadoTrasGolpe *
                    Time.fixedDeltaTime
                );
        }

        tiempoRestanteGolpeada -=
            Time.fixedDeltaTime;

        if (tiempoRestanteGolpeada <=
            0f)
        {
            IniciarRetirada();
        }
    }


    // =========================================================
    // FILTRO DE DAÑO POR CONTACTO
    // =========================================================

    /*
     * EnemigoContacto consulta automáticamente cualquier
     * IFiltroDanoContacto que exista en este mismo GameObject.
     *
     * La idea es sencilla:
     *
     * - Una águila SOLO hace daño cuando está ejecutando
     *   una acción realmente ofensiva.
     *
     * - Si está esquivando, golpeada, retirándose, patrullando
     *   o buscando el ángulo de la picada, tocar al jugador
     *   NO le quita vida.
     *
     * Esto evita que el jugador sea castigado por haber
     * logrado esquivar/golpear correctamente al enemigo.
     */
    public bool PuedeDañarPorContacto(
        Collider2D objetivo)
    {
        switch (estadoActual)
        {
            // Ataque individual real.
            case EstadoAguila.Picada:

            // Ataque disparado de los patrones de dúo.
            case EstadoAguila.DuoLanzamiento:

            // Compañero que cierra al jugador durante la pinza.
            case EstadoAguila.DuoHostigamiento:

            // Conservamos el comportamiento que ya tenías:
            // durante el Supergiro puede rozar y hacer daño.
            case EstadoAguila.DuoOrbita:

                return true;


            // Todo lo demás es movimiento NO ofensivo.
            case EstadoAguila.Esperando:
            case EstadoAguila.Patrulla:
            case EstadoAguila.PreparandoPicada:
            case EstadoAguila.Retirada:
            case EstadoAguila.Evasion:
            case EstadoAguila.Golpeada:

            default:

                return false;
        }
    }


    // =========================================================
    // ÁGUILA GOLPEÓ AL JUGADOR
    // =========================================================

    private void ManejarDanoAplicadoAlJugador(
        Vector2 direccion)
    {
        switch (estadoActual)
        {
            // Ataque individual.
            case EstadoAguila.Picada:

            // Salida disparada del dúo.
            case EstadoAguila.DuoLanzamiento:

            // El compañero logró cerrar al jugador.
            case EstadoAguila.DuoHostigamiento:

                IniciarRetirada();

                break;

            /*
             * DuoOrbita NO aparece aquí a propósito.
             *
             * Durante el Supergiro puede rozar al jugador,
             * hacerle daño y continuar girando.
             */
        }
    }


    // =========================================================
    // RANGO
    // =========================================================

    private bool JugadorDentroDeRango(
        float rango)
    {
        /*
         * El rango permanece centrado
         * en la posición original del águila.
         */
        float distancia =
            Vector2.Distance(
                jugador.position,
                posicionInicial
            );

        return distancia <= rango;
    }


    // =========================================================
    // MOVIMIENTO HACIA OBJETIVO
    // =========================================================

    private bool MoverHacia(
        Vector2 destino,
        float velocidadMaxima)
    {
        Vector2 diferencia =
            destino -
            rb.position;

        if (diferencia.magnitude <=
            distanciaLlegada)
        {
            rb.velocity =
                Vector2.zero;

            return true;
        }

        Vector2 velocidadNecesaria =
            diferencia /
            Time.fixedDeltaTime;

        Vector2 velocidad =
            Vector2.ClampMagnitude(
                velocidadNecesaria,
                velocidadMaxima
            );

        rb.velocity =
            velocidad;

        ActualizarDireccionVisual(
            velocidad.x
        );

        return false;
    }


    // =========================================================
    // SPRITE
    // =========================================================

    private void ActualizarDireccionVisual(
        float movimientoX)
    {
        if (Mathf.Abs(movimientoX) <
            0.05f)
        {
            return;
        }

        /*
         * Sprite original mira a la izquierda.
         */
        spriteRenderer.flipX =
            movimientoX > 0f;
    }


    // =========================================================
    // ANIMACIONES
    // =========================================================

    private void ReproducirIdle()
    {
        animator.CrossFade(
            estadoIdle,
            0.05f
        );
    }


    // =========================================================
    // PROGRAMAR ATAQUE
    // =========================================================

    private void ProgramarSiguienteAtaque()
    {
        tiempoHastaAtaque =
            Random.Range(
                tiempoMinimoEntreAtaques,
                tiempoMaximoEntreAtaques
            );
    }


    // =========================================================
    // CÁMARA
    // =========================================================

    private bool EstaVisibleEnCamara()
    {
        if (camara == null)
        {
            camara =
                Camera.main;
        }

        if (camara == null)
            return true;

        Vector3 viewport =
            camara.WorldToViewportPoint(
                transform.position
            );

        return
            viewport.z > 0f &&
            viewport.x > 0.03f &&
            viewport.x < 0.97f &&
            viewport.y > 0.03f &&
            viewport.y < 0.97f;
    }


    // =========================================================
    // =========================================================
    // COMBATE COMPARTIDO
    // =========================================================
    // =========================================================


    // =========================================================
    // SUPERGIRO
    // =========================================================

    private void ActualizarDuoOrbita()
    {
        float deltaAngulo =
            duoVelocidadAngular *
            Time.fixedDeltaTime;

        duoAnguloActual +=
            deltaAngulo;

        duoVueltasActuales +=
            Mathf.Abs(deltaAngulo) /
            360f;

        float radianes =
            duoAnguloActual *
            Mathf.Deg2Rad;

        Vector2 offsetOrbita =
            new Vector2(
                Mathf.Cos(radianes),
                Mathf.Sin(radianes)
            ) *
            duoRadioOrbita;

        Vector2 destinoOrbita =
            duoCentroOrbita +
            offsetOrbita;

        float velocidadOrbita =
            Mathf.Abs(
                duoVelocidadAngular
            ) *
            Mathf.Deg2Rad *
            duoRadioOrbita;

        velocidadOrbita *=
            1.20f;

        velocidadOrbita =
            Mathf.Max(
                velocidadOrbita,
                2f
            );

        MoverHacia(
            destinoOrbita,
            velocidadOrbita
        );
    }


    // =========================================================
    // LANZAMIENTO DÚO
    // =========================================================

    private void ActualizarDuoLanzamiento()
    {
        Vector2 diferencia =
            duoDestino -
            rb.position;

        if (diferencia.magnitude <=
            0.35f)
        {
            IniciarRetirada();

            return;
        }

        Vector2 direccion =
            diferencia.normalized;

        rb.velocity =
            direccion *
            duoVelocidadMovimiento;

        ActualizarDireccionVisual(
            rb.velocity.x
        );

        if (SueloDemasiadoCerca())
        {
            IniciarRetirada();

            return;
        }
    }


    // =========================================================
    // HOSTIGAMIENTO DÚO
    // =========================================================

    private void ActualizarDuoHostigamiento()
    {
        Vector2 diferencia =
            duoDestino -
            rb.position;

        if (diferencia.magnitude <=
            distanciaLlegada)
        {
            rb.velocity =
                Vector2.zero;

            return;
        }

        MoverHacia(
            duoDestino,
            duoVelocidadMovimiento
        );
    }


    // =========================================================
    // JEFE SOLITARIO
    // =========================================================

    /*
     * Lo llama AguilaDuoCoordinator cuando esta águila
     * pierde a su compañera DESPUÉS de haber iniciado
     * el combate de jefe.
     *
     * A partir de aquí:
     *
     * - deja de depender del Coordinator;
     * - utiliza la IA individual normal;
     * - conserva picadas, evasiones, golpes y retiradas;
     * - ignora para siempre rangoAbandono;
     * - continúa persiguiendo al jugador hasta ser derrotada.
     */
    public void ConvertirseEnJefeSolitario()
    {
        if (!gameObject.activeInHierarchy)
            return;

        jefeSolitarioPersistente =
            true;

        modoJefeDuoActivo =
            false;

        combateActivo =
            true;

        duoIgnorarEvasion =
            false;

        tiempoActualPreparacionPicada =
            0f;

        /*
         * Si justo estaba reaccionando a un golpe o realizando
         * una evasión, dejamos que termine esa reacción.
         *
         * Ambos estados ya terminan llamando a IniciarRetirada(),
         * y gracias a jefeSolitarioPersistente la retirada
         * regresará después a Patrulla sin revisar el rango.
         */
        if (estadoActual == EstadoAguila.Golpeada ||
            estadoActual == EstadoAguila.Evasion ||
            estadoActual == EstadoAguila.Retirada)
        {
            Debug.Log(
                $"{name}: compañera derrotada. " +
                "Continuará como jefe solitario al terminar su reacción actual."
            );

            return;
        }

        /*
         * Para cualquier otro estado —incluyendo órbita,
         * lanzamiento, hostigamiento, patrulla o picada—
         * cancelamos la acción actual y hacemos una retirada
         * limpia antes de volver a la IA individual.
         */
        IniciarRetirada();

        Debug.Log(
            $"{name}: compañera derrotada. " +
            "¡Ahora continúa como jefe solitario!"
        );
    }


    // =========================================================
    // MODO JEFE DÚO
    // =========================================================

    public void ForzarModoJefeDuo(
        bool activo)
    {
        /*
         * Una vez convertida en jefe solitario, no debe volver
         * a entrar accidentalmente en un patrón de dúo.
         */
        if (jefeSolitarioPersistente &&
            activo)
        {
            return;
        }

        modoJefeDuoActivo =
            activo;

        if (activo &&
            !combateActivo)
        {
            ActivarCombate();
        }
    }


    // =========================================================
    // SUPERGIRO / ÓRBITA
    // =========================================================

    public void IniciarOrbitaDuo(
        Vector2 centro,
        float radio,
        float velocidadAngular,
        float anguloInicial,
        int vueltasObjetivo)
    {
        if (jefeSolitarioPersistente)
            return;

        combateActivo =
            true;

        estadoActual =
            EstadoAguila.DuoOrbita;

        duoCentroOrbita =
            centro;

        duoRadioOrbita =
            Mathf.Max(
                0.1f,
                radio
            );

        duoVelocidadAngular =
            velocidadAngular;

        duoAnguloActual =
            anguloInicial;

        duoVueltasActuales =
            0f;

        duoVueltasObjetivo =
            Mathf.Max(
                1,
                vueltasObjetivo
            );

        animator.CrossFade(
            estadoAtaque,
            0.03f
        );
    }


    // =========================================================
    // ACTUALIZAR CENTRO DE ÓRBITA
    // =========================================================

    public void ActualizarCentroOrbitaDuo(
        Vector2 centro)
    {
        duoCentroOrbita =
            centro;
    }


    // =========================================================
    // LANZAMIENTO DÚO
    // =========================================================

    public void IniciarLanzamientoDuo(
        Vector2 destino,
        float velocidad,
        bool ignorarEvasion)
    {
        if (jefeSolitarioPersistente)
            return;

        combateActivo =
            true;

        estadoActual =
            EstadoAguila.DuoLanzamiento;

        duoDestino =
            destino;

        duoVelocidadMovimiento =
            Mathf.Max(
                0.1f,
                velocidad
            );

        duoIgnorarEvasion =
            ignorarEvasion;

        animator.CrossFade(
            estadoAtaque,
            0.03f
        );
    }


    // =========================================================
    // HOSTIGAMIENTO
    // =========================================================

    public void IniciarHostigamientoDuo(
        Vector2 destino,
        float velocidad)
    {
        if (jefeSolitarioPersistente)
            return;

        combateActivo =
            true;

        estadoActual =
            EstadoAguila.DuoHostigamiento;

        duoDestino =
            destino;

        duoVelocidadMovimiento =
            Mathf.Max(
                0.1f,
                velocidad
            );

        ReproducirIdle();
    }


    // =========================================================
    // ABANDONAR CONTROL DEL COORDINADOR
    // =========================================================

    public void SalirDeControlDuo()
    {
        if (!gameObject.activeInHierarchy)
            return;

        IniciarRetirada();
    }


    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            rangoDeteccion
        );

        Vector3 izquierda =
            transform.position +
            Vector3.left * 4f;

        Vector3 derecha =
            transform.position +
            Vector3.right * 4f;

        Gizmos.DrawLine(
            izquierda,
            derecha
        );
    }
}