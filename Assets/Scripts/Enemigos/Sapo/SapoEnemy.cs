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
    [Header("Jugador")]
    [SerializeField] private Transform jugador;
    [SerializeField] private string playerTag = "Player";
    [Header("Atención")]
    [SerializeField] private float radioAtencion = 8f;
    [SerializeField] private float intervaloDecision = 0.12f;
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
    [Header("Distancias de decisión")]
    [SerializeField] private float distanciaMuyCerca = 1.4f;
    [SerializeField] private float distanciaMedia = 3.5f;
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
    [Header("Feedback de daño")]
    [SerializeField] private int cantidadParpadeos = 3;
    [SerializeField] private float tiempoParpadeo = 0.06f;
    [Header("Retroceso al recibir espada")]
    [SerializeField] private float velocidadRetroceso = 4f;
    [SerializeField] private float duracionRetroceso = 0.18f;
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
    [Header("Sprite")]
    [Tooltip(
        "Actívalo únicamente si los sprites originales " +
        "miran hacia la derecha."
    )]
    [SerializeField]
    private bool spriteMiraDerecha = false;
    public enum EstadoSapo { Observando, Preparando, EnAire, Recuperando, Herido, Derrotado }
    public enum AccionSapo { Esperar, Presionar, Interceptar, Flanquear, Amagar, Reposicionar, Esquivar }

    [Header("Lectura y compromiso")]
    [SerializeField, Min(0.15f)] private float anticipacionAtaque = 0.34f;
    [SerializeField, Min(0.1f)] private float reaccionEvasion = 0.18f;
    [SerializeField, Min(1f)] private float descansoEvasion = 2.3f;
    [SerializeField, Min(0.15f)] private float recuperacionVulnerable = 0.48f;
    [SerializeField, Min(0.1f)] private float frenadoSuelo = 22f;
    [Header("Estado en juego")]
    [SerializeField] private EstadoSapo estadoActual;
    [SerializeField] private AccionSapo accionActual;

    private Rigidbody2D rb, rbJugador;
    private Collider2D cuerpoCollider, cuerpoJugador;
    private SpriteRenderer spriteRenderer;
    private EnemigoVida enemigoVida;
    private SapoSensorSuelo sensorSuelo;
    private SapoCoordinacionGrupo coordinacion;
    private SapoContactoJugador contactoJugador;
    private EspadaHitbox espadaJugador;
    private Collider2D espadaColliderJugador;
    private float siguienteDecision, siguienteSaltoPermitido, siguientePisotonPermitido, ignorarContactoHasta;
    private float hastaEstado, ignorarSueloHasta, siguienteEvasion, amenazaDesde = -1f;
    private float velocidadJugadorAntes, piesJugadorAntes, techoAntes, reaccionPreparada;
    private float amenazaPisotonDesde = -1f, parpadeoHasta, inicioParpadeo;
    private int ultimoAtaqueObservado = -1;
    private bool evadiraEspada, evadiraPisoton, procesandoPisoton, saltoOfensivo, estuvoEnAire;
    private bool inicio, muestraJugadorValida;
    private Vector2 impulsoPreparado;
    private float destinoPreparado;
    private TipoSalto tipoPreparado;
    private float direccionRetroceso;
    private Color colorOriginal;
    private Vector3 escalaVisualOriginal;
    private Coroutine coroutineSquash;

    public EstadoSapo EstadoActual => estadoActual;
    public AccionSapo AccionActual => accionActual;
    public bool EstaVivo => isActiveAndEnabled && gameObject.activeInHierarchy && enemigoVida != null && !enemigoVida.EstaMuerto;
    public bool EnSuelo => sensorSuelo != null && sensorSuelo.EnSuelo && Time.time >= ignorarSueloHasta && rb.velocity.y <= 0.15f;
    public bool AtaqueEnCurso => EstaVivo && (estadoActual == EstadoSapo.Preparando || (estadoActual == EstadoSapo.EnAire && saltoOfensivo));
    public bool PuedePrepararAtaque => inicio && EstaVivo && EnSuelo && jugador != null &&
        estadoActual != EstadoSapo.Preparando && estadoActual != EstadoSapo.Herido && Time.time >= siguienteSaltoPermitido &&
        Vector2.Distance(transform.position, jugador.position) <= radioAtencion;
    public Transform Jugador => jugador;
    public float UltimoAtaque { get; private set; } = -100f;
    public int AtaquesRealizados { get; private set; }
    public int EvasionesRealizadas { get; private set; }
    public Vector2 ImpulsoPreparado => impulsoPreparado;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>(); cuerpoCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>(); enemigoVida = GetComponent<EnemigoVida>();
        sensorSuelo = GetComponent<SapoSensorSuelo>(); coordinacion = GetComponent<SapoCoordinacionGrupo>();
        contactoJugador = GetComponent<SapoContactoJugador>(); colorOriginal = spriteRenderer.color;
        rb.freezeRotation = true; rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        if (visualParaSquash != null) escalaVisualOriginal = visualParaSquash.localScale;
    }
    private void OnEnable()
    {
        enemigoVida.OnDanoRecibido += AlRecibirDano;
        enemigoVida.OnDerrotado += AlMorir;
        spriteRenderer.enabled = true;
    }
    private void OnDisable()
    {
        enemigoVida.OnDanoRecibido -= AlRecibirDano;
        enemigoVida.OnDerrotado -= AlMorir;
        spriteRenderer.enabled = true; spriteRenderer.color = colorOriginal;
        if (coroutineSquash != null) StopCoroutine(coroutineSquash);
        if (PuedeAplastarVisual()) visualParaSquash.localScale = escalaVisualOriginal;
        coroutineSquash = null;
    }
    private void Start()
    {
        BuscarJugador(); inicio = true;
        estadoActual = EstadoSapo.Observando;
        siguienteSaltoPermitido = Time.time + Random.Range(0.55f, 0.95f);
        siguienteDecision = Time.time + Random.Range(0f, intervaloDecision);
    }
    private void BuscarJugador()
    {
        if (jugador == null)
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go == null) return;
            jugador = go.transform;
        }
        rbJugador = jugador.GetComponent<Rigidbody2D>();
        if (rbJugador == null) rbJugador = jugador.GetComponentInChildren<Rigidbody2D>();
        cuerpoJugador = null;
        foreach (var col in jugador.GetComponentsInChildren<Collider2D>())
            if (!col.isTrigger && col.attachedRigidbody == rbJugador) { cuerpoJugador = col; break; }
        espadaJugador = jugador.GetComponentInChildren<EspadaHitbox>(true);
        espadaColliderJugador = espadaJugador != null ? espadaJugador.GetComponent<Collider2D>() : null;
    }
    private void FixedUpdate()
    {
        if (!EstaVivo) return;
        if (jugador == null || rbJugador == null) BuscarJugador();
        GuardarMuestraJugador();
        ActualizarDireccionVisual();
        if (estadoActual == EstadoSapo.Herido)
        {
            if (Time.time < hastaEstado)
            {
                rb.velocity = new Vector2(Mathf.MoveTowards(rb.velocity.x, direccionRetroceso * velocidadRetroceso, 30f * Time.fixedDeltaTime), rb.velocity.y);
                return;
            }
            if (EnSuelo) EntrarRecuperacion(recuperacionVulnerable);
            else { estadoActual = EstadoSapo.EnAire; saltoOfensivo = false; estuvoEnAire = true; }
        }
        if (estadoActual == EstadoSapo.EnAire)
        {
            if (!EnSuelo) estuvoEnAire = true;
            if (EnSuelo && estuvoEnAire) EntrarRecuperacion(PausaAterrizaje());
            else return; // Ballistic flight: no homing, midair dodge or role changes.
        }
        if (estadoActual == EstadoSapo.Preparando)
        {
            Frenar();
            if (!EnSuelo) { CancelarPreparacion(); return; }
            if (Time.time >= hastaEstado)
            {
                Vector2 impulso;
                if (PlanificarSalto(destinoPreparado, tipoPreparado, out impulso)) Lanzar(impulso, true);
                else CancelarPreparacion();
            }
            return;
        }
        if (!EnSuelo) return;
        Frenar();
        if (estadoActual == EstadoSapo.Recuperando && Time.time < siguienteSaltoPermitido) return;
        estadoActual = EstadoSapo.Observando;
        if (jugador == null || Vector2.Distance(transform.position, jugador.position) > radioAtencion) return;
        if (Time.time < siguienteDecision || Time.time < siguienteSaltoPermitido) return;
        float atencion = coordinacion != null ? coordinacion.MultiplicadorAgresividad : 1f;
        siguienteDecision = Time.time + Mathf.Max(0.02f, intervaloDecision / Mathf.Max(0.1f, atencion));
        if (IntentarEvasion()) return;
        if (coordinacion == null || !coordinacion.isActiveAndEnabled) PrepararAtaque();
        else if (coordinacion.CantidadGrupo > 1) IntentarReposicion();
    }
    private void Update()
    {
        if (spriteRenderer == null) return;
        bool parpadeando = Time.time < parpadeoHasta;
        spriteRenderer.enabled = !parpadeando || Mathf.FloorToInt((Time.time - inicioParpadeo) / Mathf.Max(0.02f, tiempoParpadeo)) % 2 != 0;
        spriteRenderer.color = estadoActual == EstadoSapo.Preparando && !parpadeando
            ? Color.Lerp(colorOriginal, new Color(1f, 0.78f, 0.46f, colorOriginal.a), 0.45f) : colorOriginal;
    }
    private void GuardarMuestraJugador()
    {
        muestraJugadorValida = rbJugador != null && cuerpoJugador != null;
        if (!muestraJugadorValida) return;
        velocidadJugadorAntes = rbJugador.velocity.y;
        piesJugadorAntes = cuerpoJugador.bounds.min.y;
        techoAntes = cuerpoCollider.bounds.max.y;
    }
    private void Frenar()
    {
        rb.velocity = new Vector2(Mathf.MoveTowards(rb.velocity.x, 0f, frenadoSuelo * Time.fixedDeltaTime), rb.velocity.y);
    }
    private float PausaAterrizaje()
    {
        float pausa = Random.Range(pausaMinimaAterrizaje, pausaMaximaAterrizaje);
        float factor = coordinacion != null ? coordinacion.MultiplicadorPausa : 1f;
        return Mathf.Max(recuperacionVulnerable, pausa * factor + (Cantidad() == 1 ? 0.38f : 0f));
    }
    private int Cantidad() => coordinacion != null ? coordinacion.CantidadGrupo : 1;
    private void EntrarRecuperacion(float pausa)
    {
        estadoActual = EstadoSapo.Recuperando; accionActual = AccionSapo.Esperar;
        saltoOfensivo = false; estuvoEnAire = false;
        siguienteSaltoPermitido = Time.time + pausa;
    }

    public bool PrepararAtaque()
    {
        if (!PuedePrepararAtaque) return false;
        var rol = coordinacion != null ? coordinacion.RolActual : SapoCoordinacionGrupo.RolTactico.Duelista;
        float objetivo = coordinacion != null ? coordinacion.ObtenerObjetivoAtaqueX() : jugador.position.x;
        float distancia = Mathf.Abs(objetivo - rb.position.x);
        TipoSalto tipo = distancia < 1.9f ? TipoSalto.Pequeno : TipoSalto.Medio;
        accionActual = AccionSapo.Presionar;
        if (rol == SapoCoordinacionGrupo.RolTactico.Interceptor)
        { tipo = TipoSalto.Grande; accionActual = AccionSapo.Interceptar; }
        else if (rol == SapoCoordinacionGrupo.RolTactico.FlancoIzquierdo || rol == SapoCoordinacionGrupo.RolTactico.FlancoDerecho)
        { tipo = distancia > 3f ? TipoSalto.Grande : TipoSalto.Medio; accionActual = AccionSapo.Flanquear; }
        if (distancia > distanciaMedia + 0.7f) tipo = TipoSalto.Grande;
        Vector2 impulso;
        if (!PlanificarSalto(objetivo, tipo, out impulso)) return false;
        impulsoPreparado = impulso; destinoPreparado = objetivo; tipoPreparado = tipo;
        estadoActual = EstadoSapo.Preparando;
        reaccionPreparada = Mathf.Max(0.21f, anticipacionAtaque - (Cantidad() - 1) * 0.028f) + Random.Range(0f, 0.06f);
        hastaEstado = Time.time + reaccionPreparada;
        return true;
    }
    public void CancelarPreparacion()
    {
        if (estadoActual != EstadoSapo.Preparando) return;
        EntrarRecuperacion(recuperacionVulnerable);
    }
    private void Lanzar(Vector2 impulso, bool ofensivo)
    {
        rb.velocity = impulso;
        saltoOfensivo = ofensivo; estuvoEnAire = false;
        estadoActual = EstadoSapo.EnAire; ignorarSueloHasta = Time.time + 0.12f;
        float variacion = Random.Range(-variacionTiempoSalto, variacionTiempoSalto);
        siguienteSaltoPermitido = Time.time + Mathf.Max(0.1f, tiempoMinimoEntreSaltos + variacion);
        if (ofensivo)
        {
            UltimoAtaque = Time.time; AtaquesRealizados++;
            if (coordinacion != null) coordinacion.NotificarAtaqueRealizado();
        }
    }
    private bool IntentarEvasion()
    {
        if (Time.time < siguienteEvasion || jugador == null) return false;
        bool espadaCerca = espadaJugador != null && espadaJugador.EstaActiva && espadaColliderJugador != null &&
            Vector2.Distance(cuerpoCollider.bounds.center, espadaColliderJugador.bounds.center) <= radioEvasionEspada;
        int secuencia = espadaJugador != null ? espadaJugador.SecuenciaAtaque : -1;
        if (secuencia != ultimoAtaqueObservado)
        {
            ultimoAtaqueObservado = secuencia; amenazaDesde = -1f;
            // One roll per visible swing; repeated physics ticks never reroll an escape.
            evadiraEspada = Random.value < Mathf.Min(probabilidadEvasionEspada, 0.12f + (Cantidad() - 1) * 0.12f);
        }
        if (espadaCerca && amenazaDesde < 0f) amenazaDesde = Time.time;
        if (!espadaCerca) amenazaDesde = -1f;
        bool pisoton = rbJugador != null && cuerpoJugador != null && rbJugador.velocity.y < velocidadCaidaAmenazante &&
            cuerpoJugador.bounds.min.y > cuerpoCollider.bounds.max.y + 0.8f &&
            Mathf.Abs(cuerpoJugador.bounds.center.x - cuerpoCollider.bounds.center.x) < radioEvasionPisoton;
        if (pisoton && amenazaPisotonDesde < 0f)
        { amenazaPisotonDesde = Time.time; evadiraPisoton = Random.value < Mathf.Min(probabilidadEvasionPisoton, 0.06f + (Cantidad() - 1) * 0.03f); }
        if (!pisoton) amenazaPisotonDesde = -1f;
        bool reaccionEspada = espadaCerca && evadiraEspada && Time.time - amenazaDesde >= reaccionEvasion;
        bool reaccionPisoton = pisoton && evadiraPisoton && Time.time - amenazaPisotonDesde >= reaccionEvasion;
        if (!reaccionEspada && !reaccionPisoton) return false;
        if (coordinacion != null && !coordinacion.PuedeEvadirAhora()) return false;
        float lejos = Mathf.Sign(rb.position.x - jugador.position.x);
        if (lejos == 0f) lejos = 1f;
        // A bait can hop over the player, but only if there is room to clear the body safely.
        bool cruzar = Cantidad() >= 3 && Mathf.Abs(rb.position.x - jugador.position.x) < distanciaMuyCerca &&
            Random.value < probabilidadSaltarSobreJugador * 0.45f;
        float destino = cruzar ? jugador.position.x - lejos * 2.5f : rb.position.x + lejos * (Cantidad() == 1 ? 1.2f : 2.1f);
        Vector2 impulso;
        if (!PlanificarSalto(destino, cruzar ? TipoSalto.Grande : TipoSalto.Medio, out impulso)) return false;
        accionActual = AccionSapo.Esquivar;
        Lanzar(impulso, false); EvasionesRealizadas++;
        siguienteEvasion = Time.time + descansoEvasion;
        amenazaDesde = amenazaPisotonDesde = -1f;
        if (coordinacion != null) coordinacion.NotificarEvasion();
        return true;
    }
    private void IntentarReposicion()
    {
        if (coordinacion == null || !coordinacion.PuedeReposicionarAhora()) return;
        float objetivo = coordinacion.ObtenerObjetivoX();
        float diferencia = objetivo - rb.position.x;
        if (Mathf.Abs(diferencia) < 0.8f) return;
        // Supporting frogs hold a flank; they do not cross through the player as a hidden extra attack.
        if ((rb.position.x - jugador.position.x) * (objetivo - jugador.position.x) < 0f) return;
        float paso = Mathf.Min(1.25f, distanciaChequeoAterrizaje * 0.6f);
        float destino = rb.position.x + Mathf.Clamp(diferencia, -paso, paso);
        if (Mathf.Abs(destino - jugador.position.x) < 1.9f) return;
        Vector2 impulso;
        if (!PlanificarSalto(destino, TipoSalto.Pequeno, out impulso)) return;
        accionActual = coordinacion.RolActual == SapoCoordinacionGrupo.RolTactico.Senuelo ? AccionSapo.Amagar : AccionSapo.Reposicionar;
        Lanzar(impulso, false);
        coordinacion.NotificarReposicion();
    }

    // Compute a descending landing from gravity, jump impulse and the actual terrain below it.
    private bool PlanificarSalto(float objetivoX, TipoSalto tipo, out Vector2 impulso)
    {
        impulso = Vector2.zero;
        float vy = tipo == TipoSalto.Pequeno ? pequenoVertical : tipo == TipoSalto.Medio ? medioVertical : grandeVertical;
        float vxMax = tipo == TipoSalto.Pequeno ? pequenoHorizontal : tipo == TipoSalto.Medio ? medioHorizontal : grandeHorizontal;
        float gravedad = -Physics2D.gravity.y * rb.gravityScale;
        if (gravedad <= 0.1f || groundLayer.value == 0) return false;
        float vuelo = 2f * vy / gravedad;
        float destinoX = rb.position.x + Mathf.Clamp(objetivoX - rb.position.x, -vxMax * vuelo, vxMax * vuelo);
        Bounds b = cuerpoCollider.bounds;
        float pies = b.min.y;
        float maxAltura = vy * vy / (2f * gravedad);
        RaycastHit2D suelo;
        if (!SueloEn(destinoX, pies, maxAltura, out suelo)) return false;
        float deltaY = suelo.point.y - pies;
        float discriminante = vy * vy - 2f * gravedad * deltaY;
        if (discriminante < 0.2f) return false;
        vuelo = (vy + Mathf.Sqrt(discriminante)) / gravedad;
        float vx = Mathf.Clamp((destinoX - rb.position.x) / vuelo, -vxMax, vxMax);
        destinoX = rb.position.x + vx * vuelo;
        RaycastHit2D bordeA, bordeB;
        float margen = b.extents.x * 0.9f;
        if (!SueloEn(destinoX - margen, pies, maxAltura, out bordeA) || !SueloEn(destinoX + margen, pies, maxAltura, out bordeB)) return false;
        if (Mathf.Abs(bordeA.point.y - suelo.point.y) > 0.22f || Mathf.Abs(bordeB.point.y - suelo.point.y) > 0.22f) return false;
        Vector2 anterior = b.center;
        for (int i = 1; i <= 7; i++)
        {
            float t = vuelo * i / 8f;
            Vector2 siguiente = (Vector2)b.center + new Vector2(vx * t, vy * t - 0.5f * gravedad * t * t);
            Vector2 tramo = siguiente - anterior;
            var hit = Physics2D.BoxCast(anterior, (Vector2)b.size * 0.82f, 0f, tramo.normalized, tramo.magnitude, groundLayer);
            if (hit.collider != null && !hit.collider.isTrigger) return false;
            anterior = siguiente;
        }
        impulso = new Vector2(vx, vy);
        return true;
    }
    private bool SueloEn(float x, float pies, float altura, out RaycastHit2D suelo)
    {
        suelo = Physics2D.Raycast(new Vector2(x + cuerpoCollider.bounds.center.x - rb.position.x, pies + altura + 0.3f),
            Vector2.down, altura + alturaChequeoSuelo, groundLayer);
        return suelo.collider != null && !suelo.collider.isTrigger && suelo.normal.y > 0.6f;
    }

    private bool ObtenerCuerpoJugador(Collider2D col, out Rigidbody2D jugadorRb, out Transform raiz)
    {
        jugadorRb = null; raiz = null;
        if (col == null || col.isTrigger || col.GetComponentInParent<EspadaHitbox>() != null) return false;
        jugadorRb = col.attachedRigidbody;
        if (jugadorRb == null) return false;
        // Player can be nested under an organizational GameObject.
        raiz = jugadorRb.transform;
        while (raiz != null && !raiz.CompareTag(playerTag)) raiz = raiz.parent;
        return raiz != null;
    }
    private bool EsPisoton(Collider2D col, Rigidbody2D jugadorRb)
    {
        Bounds p = col.bounds, s = cuerpoCollider.bounds;
        bool muestra = muestraJugadorValida && jugadorRb == rbJugador;
        bool descendiendo = jugadorRb.velocity.y < -0.2f || (muestra && velocidadJugadorAntes < -0.2f);
        if (!descendiendo || jugadorRb.velocity.y > 0.25f) return false;
        float solapamiento = Mathf.Min(p.max.x, s.max.x) - Mathf.Max(p.min.x, s.min.x);
        if (solapamiento < Mathf.Min(p.size.x, s.size.x) * 0.18f || p.center.y <= s.max.y) return false;
        float margen = Mathf.Min(margenCabezaPisoton, s.size.y * 0.4f);
        float toleranciaCaida = Mathf.Max(0.12f, Mathf.Abs(jugadorRb.velocity.y) * Time.fixedDeltaTime + 0.04f);
        if (p.min.y < s.max.y - margen || p.min.y > s.max.y + toleranciaCaida) return false;
        return !muestra || piesJugadorAntes >= techoAntes - margen;
    }
    public bool IntentarProcesarPisoton(Collider2D jugadorCollider)
    {
        if (!EstaVivo || Time.time < siguientePisotonPermitido) return false;
        Rigidbody2D jugadorRb; Transform raiz;
        if (!ObtenerCuerpoJugador(jugadorCollider, out jugadorRb, out raiz) || !EsPisoton(jugadorCollider, jugadorRb)) return false;
        siguientePisotonPermitido = Time.time + cooldownPisoton;
        ignorarContactoHasta = Time.time + tiempoIgnorarContactoTrasPisoton;
        procesandoPisoton = true;
        enemigoVida.RecibirDano(new DamageInfo(danoPisoton, raiz.gameObject,
            new Vector2(jugadorCollider.bounds.center.x, cuerpoCollider.bounds.max.y), Vector2.down, 0f));
        procesandoPisoton = false;
        jugadorRb.velocity = new Vector2(jugadorRb.velocity.x, fuerzaReboteJugador);
        if (contactoJugador != null) contactoJugador.PermitirPaso(jugadorCollider, tiempoIgnorarContactoTrasPisoton);
        if (!enemigoVida.EstaMuerto) { EntrarRecuperacion(Mathf.Max(0.6f, recuperacionVulnerable)); IniciarSquash(); }
        return true;
    }
    private void OnCollisionEnter2D(Collision2D collision) { IntentarProcesarPisoton(collision.collider); }
    private void OnCollisionStay2D(Collision2D collision) { IntentarProcesarPisoton(collision.collider); }
    public bool PuedeDañarPorContacto(Collider2D objetivo)
    {
        if (!EstaVivo || Time.time < ignorarContactoHasta) return false;
        Rigidbody2D jugadorRb; Transform raiz;
        if (!ObtenerCuerpoJugador(objetivo, out jugadorRb, out raiz)) return false;
        if (IntentarProcesarPisoton(objetivo)) return false;
        if (estadoActual == EstadoSapo.EnAire && !saltoOfensivo)
        {
            // A harmless support/evasion hop must not get stranded on the player's head.
            if (contactoJugador != null) contactoJugador.PermitirPaso(objetivo, 0.2f);
            return false;
        }
        return estadoActual != EstadoSapo.Herido && estadoActual != EstadoSapo.Preparando &&
            (EnSuelo || (estadoActual == EstadoSapo.EnAire && saltoOfensivo));
    }
    private void AlRecibirDano(DamageInfo dano)
    {
        inicioParpadeo = Time.time;
        parpadeoHasta = Time.time + cantidadParpadeos * tiempoParpadeo * 2f;
        if (procesandoPisoton) return;
        estadoActual = EstadoSapo.Herido; saltoOfensivo = false;
        hastaEstado = Time.time + duracionRetroceso;
        siguienteSaltoPermitido = hastaEstado + recuperacionVulnerable;
        direccionRetroceso = Mathf.Sign(dano.direccion.x);
        if (direccionRetroceso == 0f) direccionRetroceso = jugador != null ? Mathf.Sign(rb.position.x - jugador.position.x) : 1f;
        if (direccionRetroceso == 0f) direccionRetroceso = 1f;
    }
    private void AlMorir(DamageInfo dano)
    {
        estadoActual = EstadoSapo.Derrotado; saltoOfensivo = false; rb.velocity = Vector2.zero;
    }
    private bool PuedeAplastarVisual()
    {
        // Never shrink the root, its rigidbody, its head trigger or any physical collider.
        return visualParaSquash != null && visualParaSquash != transform &&
            visualParaSquash.GetComponentsInChildren<Collider2D>(true).Length == 0;
    }
    private void IniciarSquash()
    {
        if (!PuedeAplastarVisual()) return;
        if (coroutineSquash != null) StopCoroutine(coroutineSquash);
        coroutineSquash = StartCoroutine(HacerSquash());
    }
    private IEnumerator HacerSquash()
    {
        Vector3 aplastada = Vector3.Scale(escalaVisualOriginal, new Vector3(squashX, squashY, 1f));
        float t = 0f;
        while (t < duracionSquash)
        {
            t += Time.deltaTime;
            visualParaSquash.localScale = Vector3.Lerp(aplastada, escalaVisualOriginal, Mathf.Clamp01(t / duracionSquash));
            yield return null;
        }
        visualParaSquash.localScale = escalaVisualOriginal; coroutineSquash = null;
    }
    private void ActualizarDireccionVisual()
    {
        float dx = Mathf.Abs(rb.velocity.x) > 0.2f ? rb.velocity.x : jugador != null ? jugador.position.x - rb.position.x : 0f;
        if (Mathf.Abs(dx) > 0.1f) spriteRenderer.flipX = spriteMiraDerecha ? dx < 0f : dx > 0f;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, radioAtencion);
        if (Application.isPlaying && estadoActual == EstadoSapo.Preparando)
        { Gizmos.color = Color.red; Gizmos.DrawLine(transform.position, new Vector3(destinoPreparado, transform.position.y)); }
    }
}
