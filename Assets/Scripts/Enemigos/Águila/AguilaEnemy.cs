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

    [Header("Vuelo organico")]
    [SerializeField, Min(0.1f)] private float aceleracionVuelo = 9f;
    [SerializeField, Min(0.1f)] private float aceleracionAtaque = 32f;
    [SerializeField, Min(0.1f)] private float aceleracionOrbita = 48f;
    [SerializeField, Min(0f)] private float amplitudPlaneo = 0.22f;
    [SerializeField, Min(0.05f)] private float frecuenciaPlaneo = 1.25f;
    [SerializeField, Min(0f)] private float anticipacionVisualPicada = 0.18f;
    [SerializeField, Range(0.4f, 1f)] private float proporcionVerticalOrbita = 0.72f;

    private EnemigoVida vida;
    private AguilaDuoCoordinator coordinador;
    private float fasePlaneo, variacionVuelo, centroPatrullaX;
    private float tiempoAnguloValido, tiempoEnEstado;
    private EstadoAguila ultimoEstado;
    private Vector2 direccionAtaque;
    private float duoRadioActual, duoAngularActual;

    public bool EstaViva => isActiveAndEnabled && gameObject.activeInHierarchy && (vida == null || !vida.EstaMuerto);



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
        EstaViva && !jefeSolitarioPersistente &&
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

        vida = GetComponent<EnemigoVida>();
        coordinador = GetComponent<AguilaDuoCoordinator>();
        fasePlaneo = Random.Range(0f, Mathf.PI * 2f);
        variacionVuelo = Random.Range(0.92f, 1.08f);
        centroPatrullaX = posicionInicial.x;
        camara = Camera.main;
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
        if (vida != null) vida.OnDerrotado += AlSerDerrotada;
        if (contacto != null)
        {
            contacto.OnDanoAplicado +=
                ManejarDanoAplicadoAlJugador;
        }
    }

    private void OnDisable()
    {
        if (vida != null) vida.OnDerrotado -= AlSerDerrotada;
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
        if (!EstaViva) return;
        if (ultimoEstado != estadoActual) { ultimoEstado = estadoActual; tiempoEnEstado = 0f; }
        tiempoEnEstado += Time.fixedDeltaTime;
        fasePlaneo += Time.fixedDeltaTime * frecuenciaPlaneo * variacionVuelo;

        // A second line of defence: no shared state can outlive its coordinator or partner.
        if (EstaBajoControlDuo && (coordinador == null || !coordinador.TieneParejaValida))
        {
            if (modoJefeDuoActivo) ConvertirseEnJefeSolitario();
            else IniciarRetirada();
        }
        if ((estadoActual == EstadoAguila.Evasion && tiempoEnEstado > 1.5f) ||
            (estadoActual == EstadoAguila.DuoLanzamiento && tiempoEnEstado > tiempoMaximoPicada) ||
            (estadoActual == EstadoAguila.DuoHostigamiento && tiempoEnEstado > 4f)) IniciarRetirada();
        if (estadoActual == EstadoAguila.Retirada && tiempoEnEstado > 4.5f)
        {
            estadoActual = combateActivo ? EstadoAguila.Patrulla : EstadoAguila.Esperando;
            ProgramarSiguienteAtaque();
        }
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
        ActualizarPlaneo(posicionInicial.x, anchoPatrullaReposo);
        if (JugadorDentroDeRango(rangoDeteccion)) ActivarCombate();
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
        tiempoAnguloValido = 0f;

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
        centroPatrullaX = Mathf.MoveTowards(centroPatrullaX, jugador.position.x, velocidadPatrulla * Time.fixedDeltaTime);
        ActualizarPlaneo(centroPatrullaX, anchoPatrullaCombate);
        if (!modoJefeDuoActivo && !jefeSolitarioPersistente && !JugadorDentroDeRango(rangoAbandono))
        {
            DesactivarCombate(); return;
        }
        tiempoHastaAtaque -= Time.fixedDeltaTime;
        if (tiempoHastaAtaque <= 0f && EstaVisibleEnCamara()) IniciarPreparacionPicada();
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
        tiempoAnguloValido = 0f;


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
        if (!modoJefeDuoActivo && !jefeSolitarioPersistente && !JugadorDentroDeRango(rangoAbandono))
        { DesactivarCombate(); return; }
        tiempoActualPreparacionPicada += Time.fixedDeltaTime;
        if (tiempoActualPreparacionPicada >= tiempoMaximoPreparacionPicada)
        { CancelarPreparacionPicada(); return; }

        Vector2 prediccion = ObtenerPosicionPredichaJugador();
        float lateral = Mathf.Min(CalcularDistanciaLateralNecesaria(prediccion) + margenPreparacionPicada, distanciaLateralMaximaPicada);
        float x = jugador.position.x + ladoPreparacionPicada * lateral;
        Vector2 diferencia = new Vector2(x - rb.position.x, AlturaDePlaneo() - rb.position.y);
        var deseada = new Vector2(Mathf.Clamp(diferencia.x * 2.2f, -velocidadPreparacionPicada, velocidadPreparacionPicada),
            Mathf.Clamp(diferencia.y * 1.8f, -velocidadAjusteAltura, velocidadAjusteAltura));
        if (Mathf.Abs(diferencia.x) < margenPreparacionPicada * 0.1f) deseada.x = 0f;
        AplicarVelocidadSuave(deseada, aceleracionVuelo);
        tiempoAnguloValido = PuedeIniciarPicadaDesdeAqui(prediccion) ? tiempoAnguloValido + Time.fixedDeltaTime : 0f;
        if (tiempoAnguloValido >= anticipacionVisualPicada) IniciarPicada(prediccion);
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
        tiempoAnguloValido = 0f;

        ProgramarSiguienteAtaque();

        ReproducirIdle();
    }


    // =========================================================
    // POSICIÓN PREDICHA DEL JUGADOR
    // =========================================================

    private Vector2 ObtenerPosicionPredichaJugador()
    {
        var avance = jugadorRb != null ? Vector2.ClampMagnitude(jugadorRb.velocity * anticipacionJugador, 1.25f) : Vector2.zero;
        return (Vector2)jugador.position + avance;
    }


    // =========================================================
    // PICADA
    // =========================================================

    private void IniciarPicada(Vector2 posicionObjetivo)
    {
        estadoActual = EstadoAguila.Picada;
        tiempoEnEstado = 0f;
        tiempoActualPicada = 0f;
        direccionAtaque = (posicionObjetivo - rb.position).normalized;
        if (direccionAtaque.sqrMagnitude < 0.01f) direccionAtaque = Vector2.down;
        objetivoPicada = posicionObjetivo + direccionAtaque * 0.9f;
        animator.CrossFade(estadoAtaque, 0.05f);
    }


    // =========================================================
    // ACTUALIZAR PICADA
    // =========================================================

    private void ActualizarPicada()
    {
        tiempoActualPicada += Time.fixedDeltaTime;
        AplicarVelocidadSuave(direccionAtaque * velocidadPicada, aceleracionAtaque);
        // Commit to a pass. Never reverse or home in on a point after overshooting it.
        if (Vector2.Dot(objetivoPicada - rb.position, direccionAtaque) <= 0f ||
            SueloDemasiadoCerca() || tiempoActualPicada >= tiempoMaximoPicada) IniciarRetirada();
    }


    // =========================================================
    // SUELO
    // =========================================================

    private bool SueloDemasiadoCerca()
    {
        var col = GetComponent<Collider2D>();
        float margen = Mathf.Max(distanciaMinimaSuelo, col != null ? col.bounds.extents.y + 0.12f : 0.7f);
        if (rb.velocity.y < -0.1f && Physics2D.Raycast(rb.position, Vector2.down, margen, capaSuelo).collider != null) return true;
        return rb.velocity.sqrMagnitude > 1f && Physics2D.Raycast(rb.position, rb.velocity.normalized,
            Mathf.Min(1.4f, rb.velocity.magnitude * 0.12f), capaSuelo).collider != null;
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
        tiempoEnEstado = 0f;

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

                AlturaDePlaneo()
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
        if (!EstaViva || jugador == null || estadoActual == EstadoAguila.Golpeada) return;
        duoIgnorarEvasion = false;
        estadoActual = EstadoAguila.Evasion;
        tiempoActualPicada = 0f;
        float lado = Mathf.Sign(rb.position.x - jugador.position.x);
        if (Mathf.Abs(rb.position.x - jugador.position.x) < 0.1f) lado = rb.velocity.x < 0f ? 1f : -1f;
        float variacion = Random.Range(0.92f, 1.08f);
        objetivoEvasion = rb.position + new Vector2(lado * distanciaEvasionHorizontal * variacion, distanciaEvasionVertical);
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
        if (!EstaViva) return;
        duoIgnorarEvasion = false;
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

    private bool MoverHacia(Vector2 destino, float velocidadMaxima)
    {
        Vector2 diferencia = destino - rb.position;
        float aceleracion = estadoActual == EstadoAguila.Evasion ? aceleracionAtaque * 1.2f : aceleracionVuelo * 1.65f;
        float velocidad = Mathf.Min(velocidadMaxima, Mathf.Sqrt(2f * aceleracion * Mathf.Max(0f, diferencia.magnitude - distanciaLlegada * 0.5f)));
        AplicarVelocidadSuave(diferencia.normalized * velocidad, aceleracion);
        return diferencia.sqrMagnitude <= distanciaLlegada * distanciaLlegada;
    }


    // =========================================================
    // SPRITE
    // =========================================================

    private void ActualizarDireccionVisual(float movimientoX)
    {
        if (Mathf.Abs(movimientoX) > 0.2f) spriteRenderer.flipX = movimientoX > 0f;
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
        float angularDeseada = duoVelocidadAngular * (1f + 0.05f * Mathf.Sin(duoAnguloActual * Mathf.Deg2Rad * 2f));
        duoAngularActual = Mathf.MoveTowards(duoAngularActual, angularDeseada, 260f * Time.fixedDeltaTime);
        float delta = duoAngularActual * Time.fixedDeltaTime;
        duoAnguloActual += delta;
        duoVueltasActuales += Mathf.Abs(delta) / 360f;
        duoRadioActual = Mathf.MoveTowards(duoRadioActual, duoRadioOrbita, 1.8f * Time.fixedDeltaTime);
        float r = duoAnguloActual * Mathf.Deg2Rad;
        Vector2 radial = new Vector2(Mathf.Cos(r), Mathf.Sin(r) * proporcionVerticalOrbita);
        Vector2 tangente = new Vector2(-Mathf.Sin(r), Mathf.Cos(r) * proporcionVerticalOrbita);
        Vector2 destino = duoCentroOrbita + radial * duoRadioActual;
        Vector2 deseada = tangente * (duoAngularActual * Mathf.Deg2Rad * duoRadioActual) + (destino - rb.position) * 4f;
        float limite = Mathf.Abs(duoVelocidadAngular) * Mathf.Deg2Rad * duoRadioOrbita + 3f;
        AplicarVelocidadSuave(Vector2.ClampMagnitude(deseada, limite), aceleracionOrbita);
    }


    // =========================================================
    // LANZAMIENTO DÚO
    // =========================================================

    private void ActualizarDuoLanzamiento()
    {
        AplicarVelocidadSuave(direccionAtaque * duoVelocidadMovimiento, aceleracionAtaque);
        if (Vector2.Dot(duoDestino - rb.position, direccionAtaque) <= 0f || SueloDemasiadoCerca()) IniciarRetirada();
    }


    // =========================================================
    // HOSTIGAMIENTO DÚO
    // =========================================================

    private void ActualizarDuoHostigamiento()
    {
        Vector2 destino = duoDestino + Vector2.up * (Mathf.Sin(fasePlaneo) * amplitudPlaneo * 0.3f);
        MoverHacia(destino, duoVelocidadMovimiento);
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
        if (!EstaViva)
            return;

        jefeSolitarioPersistente =
            true;
        if (jugador != null) centroPatrullaX = jugador.position.x;

        modoJefeDuoActivo =
            false;

        combateActivo =
            true;

        duoIgnorarEvasion =
            false;

        tiempoActualPreparacionPicada =
            0f;
        tiempoAnguloValido = 0f;

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
        if (!PuedeSerDirigidaPorPareja)
            return;

        combateActivo =
            true;

        estadoActual =
            EstadoAguila.DuoOrbita;
        tiempoEnEstado = 0f;
        duoIgnorarEvasion = false;

        duoCentroOrbita =
            centro;
        duoRadioActual = Mathf.Max(0.25f, Vector2.Distance(rb.position, centro));
        duoAngularActual = 0f;

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
        if (!PuedeSerDirigidaPorPareja)
            return;

        combateActivo =
            true;

        estadoActual =
            EstadoAguila.DuoLanzamiento;
        tiempoEnEstado = 0f;

        duoDestino =
            destino;

        direccionAtaque = (destino - rb.position).normalized;
        if (direccionAtaque.sqrMagnitude < 0.01f) direccionAtaque = Vector2.right;

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
        if (!PuedeSerDirigidaPorPareja)
            return;

        combateActivo =
            true;

        estadoActual =
            EstadoAguila.DuoHostigamiento;
        tiempoEnEstado = 0f;
        duoIgnorarEvasion = false;

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
        if (!EstaViva)
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

    private void ActualizarPlaneo(float centro, float ancho)
    {
        float mitad = Mathf.Max(0.7f, ancho * 0.5f);
        if (rb.position.x >= centro + mitad - 0.45f) direccionPatrulla = -1f;
        else if (rb.position.x <= centro - mitad + 0.45f) direccionPatrulla = 1f;
        var destino = new Vector2(centro + direccionPatrulla * mitad,
            AlturaDePlaneo() + Mathf.Sin(fasePlaneo) * amplitudPlaneo);
        var deseada = new Vector2(Mathf.Clamp((destino.x-rb.position.x)*2f, -velocidadPatrulla, velocidadPatrulla) * variacionVuelo,
            Mathf.Clamp((destino.y-rb.position.y)*1.8f, -velocidadAjusteAltura, velocidadAjusteAltura));
        AplicarVelocidadSuave(deseada, aceleracionVuelo);
    }

    private float AlturaDePlaneo()
    {
        return combateActivo && jugador != null ? Mathf.Max(alturaBaseY, jugador.position.y + 2.8f) : alturaBaseY;
    }

    private void AplicarVelocidadSuave(Vector2 deseada, float aceleracion)
    {
        rb.velocity = Vector2.MoveTowards(rb.velocity, deseada, Mathf.Max(0.1f, aceleracion) * Time.fixedDeltaTime);
        ActualizarDireccionVisual(rb.velocity.x);
    }

    private void AlSerDerrotada(DamageInfo dano)
    {
        duoIgnorarEvasion = false;
        rb.velocity = Vector2.zero;
    }

    public void ActualizarDestinoHostigamientoDuo(Vector2 destino)
    {
        if (estadoActual == EstadoAguila.DuoHostigamiento) duoDestino = destino;
    }

}
