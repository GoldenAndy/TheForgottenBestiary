using UnityEngine;

[RequireComponent(typeof(AguilaEnemy))]
public class AguilaDuoCoordinator : MonoBehaviour
{
    // =========================================================
    // ESTADOS
    // =========================================================

    private enum EstadoPatron
    {
        Inactivo,
        Cooldown,
        Pinza,
        SuperGiro
    }

    private enum FaseSalidaSuperGiro
    {
        Orbitando,
        Colocandose,
        Atacando,
        Terminada
    }


    // =========================================================
    // CONFIGURACIÓN GENERAL
    // =========================================================

    [Header("Pareja")]

    [Tooltip(
        "Distancia máxima a la que esta águila puede encontrar " +
        "otra águila para formar una pareja."
    )]
    [SerializeField]
    private float radioBuscarPareja = 10f;

    [Tooltip(
        "Rango general desde el centro de ambas águilas " +
        "para activar el comportamiento de dúo."
    )]
    [SerializeField]
    private float radioActivacionJefe = 12f;


    // =========================================================
    // TIEMPO ENTRE PATRONES
    // =========================================================

    [Header("Tiempo entre patrones")]

    [SerializeField]
    private float cooldownMin = 0.8f;

    [SerializeField]
    private float cooldownMax = 1.5f;


    // =========================================================
    // PATRÓN PINZA
    // =========================================================

    [Header("Patrón Pinza")]

    [SerializeField]
    private float distanciaHostigamiento = 1.8f;

    [SerializeField]
    private float velocidadHostigamiento = 6f;

    [SerializeField]
    private float velocidadAtaquePinza = 10.5f;

    [SerializeField]
    private float duracionHostigamiento = 1.1f;


    // =========================================================
    // SUPERGIRO
    // =========================================================

    [Header("Patrón Supergiro")]

    [Tooltip(
        "Radio de la circunferencia del Supergiro. " +
        "Un valor menor hace el giro más compacto."
    )]
    [SerializeField]
    private float radioOrbita = 1.6f;

    [SerializeField]
    private float velocidadAngular = 360f;

    [SerializeField]
    private int vueltasMinimas = 1;

    [SerializeField]
    private int vueltasMaximas = 5;

    [SerializeField]
    private float velocidadSalidaSupergiro = 12f;


    // =========================================================
    // SUPERGIRO NATURAL
    // =========================================================

    [Header("Activación natural del Supergiro")]

    [Tooltip(
        "Las águilas SOLO podrán comenzar el Supergiro " +
        "si están a esta distancia o menos entre ellas."
    )]
    [SerializeField]
    private float distanciaMaximaInicioSuperGiro = 3.5f;

    [Tooltip(
        "Probabilidad de que hagan el Supergiro cuando ambas " +
        "están patrullando, están suficientemente cerca " +
        "y terminó el cooldown."
    )]
    [Range(0f, 1f)]
    [SerializeField]
    private float probabilidadSuperGiro = 0.40f;


    // =========================================================
    // SALIDA LATERAL DEL SUPERGIRO
    // =========================================================

    [Header("Salida lateral del Supergiro")]

    [Tooltip(
        "Qué tan lejos del jugador se colocará el águila " +
        "antes de atacar desde izquierda o derecha."
    )]
    [SerializeField]
    private float distanciaPreparacionLateral = 2.8f;

    [Tooltip(
        "Velocidad con la que se coloca en el costado " +
        "antes del ataque."
    )]
    [SerializeField]
    private float velocidadPreparacionLateral = 7f;

    [Tooltip(
        "Cuánto atravesará hacia el lado contrario " +
        "durante el ataque horizontal."
    )]
    [SerializeField]
    private float distanciaCruceAtaque = 3f;

    [Tooltip(
        "Qué tan cerca debe llegar a su posición lateral " +
        "antes de lanzar el ataque."
    )]
    [SerializeField]
    private float toleranciaPreparacionLateral = 0.35f;

    [Tooltip(
        "Permite subir o bajar ligeramente la altura " +
        "desde la que atacará al jugador."
    )]
    [SerializeField]
    private float offsetAlturaAtaqueLateral = 0f;


    // =========================================================
    // REFERENCIAS
    // =========================================================

    private AguilaEnemy aguila;

    private AguilaDuoCoordinator parejaCoord;
    private AguilaEnemy pareja;

    private Transform jugador;


    // =========================================================
    // ESTADO GENERAL
    // =========================================================

    private EstadoPatron estadoPatron =
        EstadoPatron.Inactivo;

    private bool modoJefeActivo;

    /*
     * Se vuelve true en LAS DOS águilas en cuanto
     * el combate de dúo se activa por primera vez.
     *
     * Esto permite que, aunque muera el líder o el
     * seguidor en un momento en que no esté ejecutando
     * un patrón compartido, la superviviente recuerde
     * que ya formaba parte del jefe.
     */
    private bool combateDuoIniciadoAlgunaVez;

    /*
     * Cuando la pareja muere después de haber comenzado
     * el combate de jefe, la pérdida es definitiva.
     *
     * Desde ese momento este Coordinator deja de buscar
     * nuevas parejas y no vuelve a intervenir en la IA.
     */
    private bool parejaPerdidaDefinitivamente;

    private bool colisionesIgnoradas;

    private float timerCooldown;


    // =========================================================
    // PINZA
    // =========================================================

    private AguilaEnemy atacantePinza;
    private AguilaEnemy hostigadorPinza;

    private float timerHostigamiento;


    // =========================================================
    // SUPERGIRO
    // =========================================================

    private Vector2 centroSuperGiro;

    private FaseSalidaSuperGiro faseAguilaA;
    private FaseSalidaSuperGiro faseAguilaB;

    private Vector2 destinoPreparacionA;
    private Vector2 destinoPreparacionB;

    /*
     * -1 = izquierda
     *  1 = derecha
     */
    private float ladoAguilaA;
    private float ladoAguilaB;


    // =========================================================
    // LIDERAZGO
    // =========================================================

    /*
     * Las dos águilas tienen Coordinator,
     * pero solamente una controla la máquina
     * de estados compartida.
     */
    private bool SoyLider =>
        parejaCoord != null &&
        GetInstanceID() <
        parejaCoord.GetInstanceID();


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        aguila =
            GetComponent<AguilaEnemy>();
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        BuscarPareja();
        BuscarJugador();
    }


    // =========================================================
    // ON DISABLE
    // =========================================================

    private void OnDisable()
    {
        /*
         * Esto es especialmente importante cuando
         * una de las dos águilas MUERE.
         *
         * Antes de desaparecer, avisa inmediatamente
         * a su pareja para que cancele cualquier
         * Supergiro o patrón compartido.
         */
        if (parejaCoord != null)
        {
            parejaCoord.NotificarParejaPerdida(
                this
            );
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (jugador == null)
        {
            BuscarJugador();
        }

        /*
         * Una vez la compañera murió durante el combate
         * de jefe, este Coordinator ya terminó su trabajo.
         *
         * AguilaEnemy queda funcionando como jefe solitario.
         */
        if (parejaPerdidaDefinitivamente)
            return;


        // =====================================================
        // COMPROBAR SI LA PAREJA SIGUE VIVA
        // =====================================================

        if (!ParejaSigueActiva())
        {
            /*
             * IMPORTANTE:
             *
             * No basta con comprobar modoJefeActivo, porque
             * solamente el Coordinator líder mantiene la
             * máquina de estados compartida.
             *
             * combateDuoIniciadoAlgunaVez se sincroniza en
             * AMBOS coordinadores cuando empieza el jefe.
             *
             * Así, si muere el líder y sobrevive el seguidor,
             * este también sabe que debe convertirse en
             * jefe solitario.
             */
            if (combateDuoIniciadoAlgunaVez ||
                modoJefeActivo ||
                (aguila != null &&
                 aguila.EstaBajoControlDuo))
            {
                CancelarPorPerdidaPareja();

                return;
            }

            /*
             * Si el combate de jefe NUNCA llegó a empezar,
             * todavía es válido buscar otra pareja.
             */
            if (!parejaPerdidaDefinitivamente &&
                (parejaCoord == null ||
                 pareja == null))
            {
                BuscarPareja();
            }
        }


        // =====================================================
        // IGNORAR COLISIONES ENTRE LAS DOS
        // =====================================================

        if (parejaCoord != null &&
            pareja != null &&
            !colisionesIgnoradas)
        {
            IgnorarColisionesConPareja();
        }
    }

    // =========================================================
    // FIXED UPDATE
    // =========================================================

    private void FixedUpdate()
    {
        if (jugador == null)
            return;

        /*
         * Después de perder definitivamente a la pareja,
         * AguilaEnemy controla por completo el combate
         * individual. Este Coordinator queda inactivo.
         */
        if (parejaPerdidaDefinitivamente)
            return;

        /*
         * Segunda protección por si la pareja desaparece
         * entre Update y FixedUpdate.
         */
        if (!ParejaSigueActiva())
        {
            if (combateDuoIniciadoAlgunaVez ||
                modoJefeActivo ||
                (aguila != null &&
                 aguila.EstaBajoControlDuo))
            {
                CancelarPorPerdidaPareja();
            }

            return;
        }

        /*
         * Solamente una águila toma las decisiones
         * de los patrones compartidos.
         */
        if (!SoyLider)
            return;


        // =====================================================
        // ACTIVAR MODO DÚO
        // =====================================================

        if (!modoJefeActivo &&
            DebeActivarModoJefe())
        {
            ActivarModoJefe();
        }

        if (!modoJefeActivo)
            return;


        // =====================================================
        // PATRÓN ACTUAL
        // =====================================================

        switch (estadoPatron)
        {
            case EstadoPatron.Cooldown:

                ActualizarCooldown();

                break;


            case EstadoPatron.Pinza:

                ActualizarPinza();

                break;


            case EstadoPatron.SuperGiro:

                ActualizarSuperGiro();

                break;
        }
    }

    // =========================================================
    // BUSCAR JUGADOR
    // =========================================================

    private void BuscarJugador()
    {
        GameObject obj =
            GameObject.FindGameObjectWithTag(
                "Player"
            );

        if (obj != null)
        {
            jugador =
                obj.transform;
        }
    }


    // =========================================================
    // BUSCAR PAREJA
    // =========================================================

    private void BuscarPareja()
    {
        /*
         * Si esta águila ya perdió a su compañera durante
         * el combate de jefe, NO debe formar otro dúo.
         */
        if (parejaPerdidaDefinitivamente)
            return;

        /*
         * Si ya tenemos una pareja válida,
         * no necesitamos buscar otra.
         */
        if (ParejaSigueActiva())
            return;

        parejaCoord = null;
        pareja = null;
        colisionesIgnoradas = false;

        AguilaDuoCoordinator[] todas =
            FindObjectsOfType<AguilaDuoCoordinator>();

        float mejorDistancia =
            float.MaxValue;

        AguilaDuoCoordinator mejor =
            null;

        foreach (
            AguilaDuoCoordinator otra
            in todas)
        {
            if (otra == null ||
                otra == this)
            {
                continue;
            }

            if (!otra.gameObject.activeInHierarchy)
                continue;

            AguilaEnemy otraAguila =
                otra.GetComponent<AguilaEnemy>();

            if (otraAguila == null ||
                !otraAguila.gameObject.activeInHierarchy)
            {
                continue;
            }

            float distancia =
                Vector2.Distance(
                    transform.position,
                    otra.transform.position
                );

            if (distancia <=
                    radioBuscarPareja &&
                distancia <
                    mejorDistancia)
            {
                mejorDistancia =
                    distancia;

                mejor =
                    otra;
            }
        }

        if (mejor != null)
        {
            parejaCoord =
                mejor;

            pareja =
                mejor.GetComponent<AguilaEnemy>();
        }
    }


    // =========================================================
    // PAREJA ACTIVA
    // =========================================================

    private bool ParejaSigueActiva()
    {
        if (parejaCoord == null ||
            pareja == null)
        {
            return false;
        }

        if (!parejaCoord.gameObject.activeInHierarchy ||
            !pareja.gameObject.activeInHierarchy)
        {
            return false;
        }

        return true;
    }


    // =========================================================
    // NOTIFICAR MUERTE / DESAPARICIÓN
    // =========================================================

    private void NotificarParejaPerdida(
        AguilaDuoCoordinator perdida)
    {
        /*
         * Solo reaccionamos si quien desapareció
         * era realmente nuestra pareja.
         */
        if (parejaCoord != perdida)
            return;

        CancelarPorPerdidaPareja();
    }


    // =========================================================
    // CANCELACIÓN POR MUERTE DE PAREJA
    // =========================================================

    private void CancelarPorPerdidaPareja()
    {
        /*
         * Determinamos si esta pérdida ocurrió DESPUÉS de
         * que realmente hubiera comenzado el combate de jefe.
         *
         * combateDuoIniciadoAlgunaVez es la comprobación
         * principal y está sincronizada en ambos coordinadores.
         *
         * Las otras dos condiciones sirven como protección
         * adicional ante una desaparición en medio de un patrón.
         */
        bool eraParteDelJefe =
            combateDuoIniciadoAlgunaVez ||
            modoJefeActivo ||
            (
                aguila != null &&
                aguila.EstaBajoControlDuo
            );


        // =====================================================
        // LIMPIAR MÁQUINA DE ESTADOS DEL DÚO
        // =====================================================

        modoJefeActivo =
            false;

        estadoPatron =
            EstadoPatron.Inactivo;

        LimpiarEstadoPatron();


        // =====================================================
        // ROMPER LA PAREJA
        // =====================================================

        parejaCoord =
            null;

        pareja =
            null;

        colisionesIgnoradas =
            false;


        // =====================================================
        // SI YA ERA JEFE -> JEFE SOLITARIO
        // =====================================================

        if (eraParteDelJefe)
        {
            /*
             * La pérdida es definitiva.
             *
             * A partir de aquí este Coordinator:
             *
             * - NO busca otra pareja.
             * - NO vuelve a activar patrones compartidos.
             * - NO limita al águila por rango.
             *
             * AguilaEnemy toma el control con su comportamiento
             * individual normal, pero en modo jefe persistente.
             */
            parejaPerdidaDefinitivamente =
                true;

            if (aguila != null &&
                aguila.gameObject.activeInHierarchy)
            {
                aguila.ConvertirseEnJefeSolitario();
            }

            Debug.Log(
                $"{name}: la pareja fue derrotada. " +
                "La superviviente continúa como jefe solitario."
            );

            return;
        }


        // =====================================================
        // SI EL JEFE NUNCA EMPEZÓ
        // =====================================================

        /*
         * Si por alguna razón una posible pareja desapareció
         * ANTES de que empezara el combate de jefe, simplemente
         * liberamos cualquier estado extraño y permitimos que
         * este Coordinator pueda buscar otra pareja después.
         */
        if (aguila != null &&
            aguila.gameObject.activeInHierarchy)
        {
            if (aguila.EstaBajoControlDuo)
            {
                aguila.SalirDeControlDuo();
            }

            aguila.ForzarModoJefeDuo(
                false
            );
        }

        Debug.Log(
            $"{name}: pareja perdida antes de iniciar el jefe. " +
            "Se canceló el comportamiento de dúo."
        );
    }

    // =========================================================
    // ACTIVACIÓN DEL MODO DÚO
    // =========================================================

    private bool DebeActivarModoJefe()
    {
        if (pareja == null ||
            jugador == null)
        {
            return false;
        }

        Vector2 centroPareja =
            (
                (Vector2)aguila.transform.position +
                (Vector2)pareja.transform.position
            ) *
            0.5f;

        float distanciaJugador =
            Vector2.Distance(
                jugador.position,
                centroPareja
            );

        return
            distanciaJugador <=
                radioActivacionJefe ||

            aguila.CombateActivo ||

            pareja.CombateActivo;
    }


    // =========================================================
    // ACTIVAR MODO DÚO
    // =========================================================

    private void ActivarModoJefe()
    {
        if (!ParejaSigueActiva())
            return;

        modoJefeActivo =
            true;

        /*
         * MUY IMPORTANTE:
         *
         * Marcamos en LOS DOS coordinadores que el combate
         * de jefe ya comenzó.
         *
         * Solo uno de ellos es líder, pero cualquiera de los
         * dos puede ser quien sobreviva si el otro muere.
         */
        combateDuoIniciadoAlgunaVez =
            true;

        if (parejaCoord != null)
        {
            parejaCoord.combateDuoIniciadoAlgunaVez =
                true;
        }

        aguila.ForzarModoJefeDuo(
            true
        );

        pareja.ForzarModoJefeDuo(
            true
        );

        EntrarCooldown();

        Debug.Log(
            "¡Las dos águilas entraron en modo dúo!"
        );
    }


    // =========================================================
    // DESACTIVAR MODO DÚO
    // =========================================================

    private void DesactivarModoJefe()
    {
        modoJefeActivo =
            false;

        LiberarAguilaDelDuo(
            aguila
        );

        LiberarAguilaDelDuo(
            pareja
        );

        estadoPatron =
            EstadoPatron.Inactivo;

        LimpiarEstadoPatron();
    }


    // =========================================================
    // LIBERAR ÁGUILA
    // =========================================================

    private void LiberarAguilaDelDuo(
        AguilaEnemy objetivo)
    {
        if (objetivo == null)
            return;

        if (!objetivo.gameObject.activeInHierarchy)
            return;

        if (objetivo.EstaBajoControlDuo)
        {
            objetivo.SalirDeControlDuo();
        }

        objetivo.ForzarModoJefeDuo(
            false
        );
    }


    // =========================================================
    // LIMPIAR ESTADO DEL PATRÓN
    // =========================================================

    private void LimpiarEstadoPatron()
    {
        atacantePinza = null;
        hostigadorPinza = null;

        timerHostigamiento = 0f;

        faseAguilaA =
            FaseSalidaSuperGiro.Terminada;

        faseAguilaB =
            FaseSalidaSuperGiro.Terminada;

        destinoPreparacionA =
            Vector2.zero;

        destinoPreparacionB =
            Vector2.zero;
    }


    // =========================================================
    // COOLDOWN
    // =========================================================

    private void EntrarCooldown()
    {
        estadoPatron =
            EstadoPatron.Cooldown;

        timerCooldown =
            Random.Range(
                cooldownMin,
                cooldownMax
            );
    }


    // =========================================================
    // ACTUALIZAR COOLDOWN
    // =========================================================

    private void ActualizarCooldown()
    {
        /*
         * MUY IMPORTANTE:
         *
         * Los patrones compartidos ahora SOLO comienzan
         * cuando las DOS águilas han vuelto a patrullar.
         *
         * No interrumpimos:
         *
         * - picadas
         * - retiradas
         * - evasiones
         * - golpes
         *
         * Esto hace que todo se sienta mucho menos robótico.
         */
        if (!AmbasDisponibles())
            return;

        timerCooldown -=
            Time.fixedDeltaTime;

        if (timerCooldown > 0f)
            return;


        // =====================================================
        // ¿PUEDE OCURRIR EL SUPERGIRO NATURALMENTE?
        // =====================================================

        if (PuedeIniciarSuperGiroNatural() &&
            Random.value <=
                probabilidadSuperGiro)
        {
            IniciarSuperGiro();

            return;
        }


        // =====================================================
        // SI NO, REALIZAN PINZA
        // =====================================================

        IniciarPinza();
    }


    // =========================================================
    // DISPONIBILIDAD
    // =========================================================

    private bool AmbasDisponibles()
    {
        if (aguila == null ||
            pareja == null)
        {
            return false;
        }

        /*
         * Antes bastaba con que pudieran ser dirigidas.
         *
         * Ahora exigimos además que AMBAS estén
         * realmente patrullando.
         */
        return
            aguila.PuedeSerDirigidaPorPareja &&
            pareja.PuedeSerDirigidaPorPareja &&

            aguila.EstaPatrullando &&
            pareja.EstaPatrullando &&

            !aguila.EstaBajoControlDuo &&
            !pareja.EstaBajoControlDuo;
    }


    // =========================================================
    // SUPERGIRO NATURAL
    // =========================================================

    private bool PuedeIniciarSuperGiroNatural()
    {
        if (!AmbasDisponibles())
            return false;

        float distanciaEntreAguilas =
            Vector2.Distance(
                aguila.transform.position,
                pareja.transform.position
            );

        /*
         * Si están demasiado lejos:
         *
         * NO intentan juntarse artificialmente.
         *
         * Simplemente siguen con otro comportamiento
         * y esperarán otra oportunidad.
         */
        return
            distanciaEntreAguilas <=
            distanciaMaximaInicioSuperGiro;
    }


    // =========================================================
    // =========================================================
    // PATRÓN PINZA
    // =========================================================
    // =========================================================

    private void IniciarPinza()
    {
        if (!AmbasDisponibles())
        {
            EntrarCooldown();
            return;
        }

        estadoPatron =
            EstadoPatron.Pinza;


        // =====================================================
        // ELEGIR ROLES
        // =====================================================

        if (Random.value < 0.5f)
        {
            atacantePinza =
                aguila;

            hostigadorPinza =
                pareja;
        }
        else
        {
            atacantePinza =
                pareja;

            hostigadorPinza =
                aguila;
        }


        // =====================================================
        // ATACANTE
        // =====================================================

        Vector2 destinoAtaque =
            PredecirJugador(
                0.18f
            );

        atacantePinza.IniciarLanzamientoDuo(
            destinoAtaque,
            velocidadAtaquePinza,
            false
        );


        // =====================================================
        // HOSTIGADOR
        // =====================================================

        float ladoAtaque =
            Mathf.Sign(
                atacantePinza.transform.position.x -
                jugador.position.x
            );

        if (ladoAtaque == 0f)
        {
            ladoAtaque =
                Random.value < 0.5f
                    ? -1f
                    : 1f;
        }

        float ladoOpuesto =
            -ladoAtaque;

        Vector2 destinoHostigamiento =
            new Vector2(
                jugador.position.x +
                ladoOpuesto *
                distanciaHostigamiento,

                hostigadorPinza.AlturaBaseY
            );

        hostigadorPinza.IniciarHostigamientoDuo(
            destinoHostigamiento,
            velocidadHostigamiento
        );

        timerHostigamiento =
            duracionHostigamiento;
    }


    // =========================================================
    // ACTUALIZAR PINZA
    // =========================================================

    private void ActualizarPinza()
    {
        /*
         * Si la pareja murió durante la pinza,
         * también cancelamos todo.
         */
        if (!ParejaSigueActiva())
        {
            CancelarPorPerdidaPareja();
            return;
        }

        if (atacantePinza == null ||
            hostigadorPinza == null)
        {
            EntrarCooldown();

            return;
        }

        timerHostigamiento -=
            Time.fixedDeltaTime;

        if (timerHostigamiento <= 0f &&
            hostigadorPinza.EstaBajoControlDuo)
        {
            hostigadorPinza
                .SalirDeControlDuo();
        }

        if (!atacantePinza.EstaBajoControlDuo &&
            !hostigadorPinza.EstaBajoControlDuo)
        {
            EntrarCooldown();
        }
    }


    // =========================================================
    // =========================================================
    // SUPERGIRO
    // =========================================================
    // =========================================================

    private void IniciarSuperGiro()
    {
        if (!PuedeIniciarSuperGiroNatural())
        {
            EntrarCooldown();

            return;
        }

        estadoPatron =
            EstadoPatron.SuperGiro;

        faseAguilaA =
            FaseSalidaSuperGiro.Orbitando;

        faseAguilaB =
            FaseSalidaSuperGiro.Orbitando;


        // =====================================================
        // VUELTAS ALEATORIAS
        // =====================================================

        int minimo =
            Mathf.Max(
                1,
                vueltasMinimas
            );

        int maximo =
            Mathf.Max(
                minimo,
                vueltasMaximas
            );

        int vueltasA =
            Random.Range(
                minimo,
                maximo + 1
            );

        int vueltasB =
            Random.Range(
                minimo,
                maximo + 1
            );


        // =====================================================
        // CENTRO NATURAL DEL SUPERGIRO
        // =====================================================

        /*
         * CAMBIO IMPORTANTE:
         *
         * Ya NO obligamos a las águilas a correr
         * hasta la X del jugador.
         *
         * El Supergiro se forma EXACTAMENTE alrededor
         * del punto donde ambas se encontraron.
         *
         * Como ambas estaban patrullando en el cielo,
         * el círculo aparece naturalmente en el cielo.
         */
        Vector2 posicionA =
            aguila.transform.position;

        Vector2 posicionB =
            pareja.transform.position;

        centroSuperGiro =
            (
                posicionA +
                posicionB
            ) *
            0.5f;


        // =====================================================
        // ELEGIR IZQUIERDA Y DERECHA
        // =====================================================

        /*
         * La que estaba más a la izquierda intentará
         * atacar después desde la izquierda.
         *
         * La otra desde la derecha.
         *
         * Así reducimos movimientos artificiales.
         */
        if (posicionA.x <
            posicionB.x)
        {
            ladoAguilaA = -1f;
            ladoAguilaB = 1f;
        }
        else if (posicionA.x >
                 posicionB.x)
        {
            ladoAguilaA = 1f;
            ladoAguilaB = -1f;
        }
        else
        {
            if (Random.value < 0.5f)
            {
                ladoAguilaA = -1f;
                ladoAguilaB = 1f;
            }
            else
            {
                ladoAguilaA = 1f;
                ladoAguilaB = -1f;
            }
        }


        // =====================================================
        // ÁNGULOS INICIALES NATURALES
        // =====================================================

        float anguloA =
            CalcularAnguloInicial(
                posicionA,
                centroSuperGiro,
                0f
            );

        float anguloB =
            CalcularAnguloInicial(
                posicionB,
                centroSuperGiro,
                180f
            );


        // =====================================================
        // INICIAR ÓRBITAS
        // =====================================================

        aguila.IniciarOrbitaDuo(
            centroSuperGiro,
            radioOrbita,
            velocidadAngular,
            anguloA,
            vueltasA
        );

        pareja.IniciarOrbitaDuo(
            centroSuperGiro,
            radioOrbita,
            velocidadAngular,
            anguloB,
            vueltasB
        );

        Debug.Log(
            $"SUPERGIRO natural iniciado. " +
            $"Distancia entre águilas: " +
            $"{Vector2.Distance(posicionA, posicionB):0.00}. " +
            $"A: {vueltasA} vuelta(s), " +
            $"B: {vueltasB} vuelta(s)."
        );
    }


    // =========================================================
    // CALCULAR ÁNGULO INICIAL
    // =========================================================

    private float CalcularAnguloInicial(
        Vector2 posicion,
        Vector2 centro,
        float respaldo)
    {
        Vector2 diferencia =
            posicion -
            centro;

        /*
         * Si por casualidad ambas están prácticamente
         * en el mismo punto, usamos el ángulo de respaldo.
         */
        if (diferencia.sqrMagnitude <
            0.001f)
        {
            return respaldo;
        }

        return
            Mathf.Atan2(
                diferencia.y,
                diferencia.x
            ) *
            Mathf.Rad2Deg;
    }


    // =========================================================
    // ACTUALIZAR SUPERGIRO
    // =========================================================

    private void ActualizarSuperGiro()
    {
        /*
         * PROTECCIÓN PRINCIPAL:
         *
         * Si cualquiera de las dos desapareció,
         * la superviviente deja INMEDIATAMENTE
         * el Supergiro.
         */
        if (!ParejaSigueActiva())
        {
            CancelarPorPerdidaPareja();

            return;
        }


        // =====================================================
        // ÁGUILA A
        // =====================================================

        ActualizarSalidaSuperGiro(
            aguila,
            ladoAguilaA,
            ref faseAguilaA,
            ref destinoPreparacionA
        );


        // =====================================================
        // ÁGUILA B
        // =====================================================

        ActualizarSalidaSuperGiro(
            pareja,
            ladoAguilaB,
            ref faseAguilaB,
            ref destinoPreparacionB
        );


        // =====================================================
        // TERMINAR PATRÓN
        // =====================================================

        if (faseAguilaA ==
                FaseSalidaSuperGiro.Terminada &&
            faseAguilaB ==
                FaseSalidaSuperGiro.Terminada)
        {
            EntrarCooldown();
        }
    }


    // =========================================================
    // SALIDA INDIVIDUAL DEL SUPERGIRO
    // =========================================================

    private void ActualizarSalidaSuperGiro(
        AguilaEnemy objetivo,
        float lado,
        ref FaseSalidaSuperGiro fase,
        ref Vector2 destinoPreparacion)
    {
        if (objetivo == null ||
            !objetivo.gameObject.activeInHierarchy)
        {
            fase =
                FaseSalidaSuperGiro.Terminada;

            return;
        }


        switch (fase)
        {
            // =================================================
            // 1. ORBITANDO
            // =================================================

            case FaseSalidaSuperGiro.Orbitando:

                if (objetivo.EstaEnOrbitaDuo)
                {
                    objetivo.ActualizarCentroOrbitaDuo(
                        centroSuperGiro
                    );
                }

                /*
                 * Si terminó sus vueltas,
                 * NO ataca directamente al jugador.
                 *
                 * Primero va a un COSTADO.
                 */
                if (objetivo.TerminoVueltasDuo)
                {
                    destinoPreparacion =
                        ObtenerDestinoPreparacionLateral(
                            lado
                        );

                    objetivo.IniciarHostigamientoDuo(
                        destinoPreparacion,
                        velocidadPreparacionLateral
                    );

                    fase =
                        FaseSalidaSuperGiro.Colocandose;
                }
                else if (!objetivo.EstaEnOrbitaDuo)
                {
                    /*
                     * Si salió inesperadamente de la
                     * órbita por algún otro motivo,
                     * damos por terminada su parte.
                     */
                    fase =
                        FaseSalidaSuperGiro.Terminada;
                }

                break;


            // =================================================
            // 2. COLOCÁNDOSE A IZQUIERDA / DERECHA
            // =================================================

            case FaseSalidaSuperGiro.Colocandose:

                /*
                 * Si por evasión, golpe u otra razón
                 * abandonó el control del dúo,
                 * no lo obligamos a volver.
                 */
                if (!objetivo.EstaBajoControlDuo)
                {
                    fase =
                        FaseSalidaSuperGiro.Terminada;

                    break;
                }

                float distanciaAlPunto =
                    Vector2.Distance(
                        objetivo.transform.position,
                        destinoPreparacion
                    );

                if (distanciaAlPunto <=
                    toleranciaPreparacionLateral)
                {
                    /*
                     * Antes de dispararlo comprobamos
                     * que realmente siga estando en
                     * el lado correcto del jugador.
                     *
                     * Si el jugador corrió y lo pasó,
                     * recalculamos la posición lateral.
                     */
                    if (!EstaEnLadoCorrecto(
                        objetivo,
                        lado))
                    {
                        destinoPreparacion =
                            ObtenerDestinoPreparacionLateral(
                                lado
                            );

                        objetivo.IniciarHostigamientoDuo(
                            destinoPreparacion,
                            velocidadPreparacionLateral
                        );

                        break;
                    }


                    // =========================================
                    // ATAQUE HORIZONTAL
                    // =========================================

                    Vector2 destinoAtaque =
                        ObtenerDestinoAtaqueHorizontal(
                            objetivo,
                            lado
                        );

                    objetivo.IniciarLanzamientoDuo(
                        destinoAtaque,
                        velocidadSalidaSupergiro,
                        true
                    );

                    fase =
                        FaseSalidaSuperGiro.Atacando;
                }

                break;


            // =================================================
            // 3. ATAQUE HORIZONTAL
            // =================================================

            case FaseSalidaSuperGiro.Atacando:

                /*
                 * IniciarLanzamientoDuo termina
                 * automáticamente entrando en retirada
                 * cuando alcanza el destino o golpea
                 * al jugador.
                 *
                 * Cuando deja los estados de dúo,
                 * damos por terminada su parte.
                 */
                if (!objetivo.EstaBajoControlDuo)
                {
                    fase =
                        FaseSalidaSuperGiro.Terminada;
                }

                break;


            // =================================================
            // 4. TERMINADA
            // =================================================

            case FaseSalidaSuperGiro.Terminada:

                break;
        }
    }


    // =========================================================
    // PUNTO DE PREPARACIÓN LATERAL
    // =========================================================

    private Vector2 ObtenerDestinoPreparacionLateral(
        float lado)
    {
        return
            new Vector2(
                jugador.position.x +
                lado *
                distanciaPreparacionLateral,

                jugador.position.y +
                offsetAlturaAtaqueLateral
            );
    }


    // =========================================================
    // COMPROBAR LADO
    // =========================================================

    private bool EstaEnLadoCorrecto(
        AguilaEnemy objetivo,
        float lado)
    {
        float diferenciaX =
            objetivo.transform.position.x -
            jugador.position.x;

        /*
         * lado = -1
         * Queremos diferencia negativa.
         *
         * lado = 1
         * Queremos diferencia positiva.
         */
        return
            diferenciaX *
            lado >
            0.25f;
    }


    // =========================================================
    // DESTINO DEL ATAQUE HORIZONTAL
    // =========================================================

    private Vector2 ObtenerDestinoAtaqueHorizontal(
        AguilaEnemy objetivo,
        float lado)
    {
        /*
         * Si viene desde la IZQUIERDA:
         *
         *      🦅 ------> Player ------> destino
         *
         * Si viene desde la DERECHA:
         *
         * destino <------ Player <------ 🦅
         *
         * La Y se mantiene EXACTAMENTE igual a
         * la del águila al empezar el ataque.
         *
         * Por eso NO puede lanzarse diagonalmente
         * desde encima del jugador.
         */
        float ladoContrario =
            -lado;

        return
            new Vector2(
                jugador.position.x +
                ladoContrario *
                distanciaCruceAtaque,

                objetivo.transform.position.y
            );
    }


    // =========================================================
    // PREDECIR JUGADOR
    // =========================================================

    private Vector2 PredecirJugador(
        float tiempo)
    {
        Rigidbody2D rbJugador =
            jugador.GetComponent<Rigidbody2D>();

        Vector2 basePos =
            jugador.position;

        if (rbJugador == null)
        {
            return basePos;
        }

        return
            basePos +
            rbJugador.velocity *
            tiempo;
    }


    // =========================================================
    // IGNORAR COLISIONES ENTRE ÁGUILAS
    // =========================================================

    private void IgnorarColisionesConPareja()
    {
        if (parejaCoord == null)
            return;

        Collider2D[] misColliders =
            GetComponentsInChildren<Collider2D>();

        Collider2D[] susColliders =
            parejaCoord
                .GetComponentsInChildren<Collider2D>();

        foreach (
            Collider2D mio
            in misColliders)
        {
            foreach (
                Collider2D suyo
                in susColliders)
            {
                if (mio != null &&
                    suyo != null)
                {
                    Physics2D.IgnoreCollision(
                        mio,
                        suyo,
                        true
                    );
                }
            }
        }

        colisionesIgnoradas =
            true;
    }


    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        // =====================================================
        // BÚSQUEDA DE PAREJA
        // =====================================================

        Gizmos.color =
            Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            radioBuscarPareja
        );


        // =====================================================
        // DISTANCIA NATURAL DEL SUPERGIRO
        // =====================================================

        Gizmos.color =
            Color.cyan;

        Gizmos.DrawWireSphere(
            transform.position,
            distanciaMaximaInicioSuperGiro
        );
    }
}