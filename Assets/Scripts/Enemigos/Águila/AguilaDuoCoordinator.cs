using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AguilaEnemy))]
public class AguilaDuoCoordinator : MonoBehaviour
{
    private enum EstadoPatron { Inactivo, Cooldown, Pinza, SuperGiro }
    private enum FaseSalidaSuperGiro { Orbitando, EsperandoTurno, Colocandose, Atacando, Terminada }
    private sealed class Salida
    {
        public FaseSalidaSuperGiro fase = FaseSalidaSuperGiro.Terminada;
        public Vector2 destino;
        public float lado, tiempo;
    }

    [Header("Pareja")]
    [SerializeField] private float radioBuscarPareja = 10f;
    [SerializeField] private float radioActivacionJefe = 12f;
    [Header("Tiempo entre patrones")]
    [SerializeField] private float cooldownMin = 1.2f;
    [SerializeField] private float cooldownMax = 2.2f;
    [Header("Patron Pinza")]
    [SerializeField] private float distanciaHostigamiento = 1.8f;
    [SerializeField] private float velocidadHostigamiento = 6f;
    [SerializeField] private float velocidadAtaquePinza = 10.5f;
    [SerializeField] private float duracionHostigamiento = 1.1f;
    [Header("Patron Supergiro")]
    [SerializeField] private float radioOrbita = 2.2f;
    [SerializeField] private float velocidadAngular = 250f;
    [SerializeField] private int vueltasMinimas = 1;
    [SerializeField] private int vueltasMaximas = 3;
    [SerializeField] private float velocidadSalidaSupergiro = 12f;
    [Header("Activacion natural del Supergiro")]
    [SerializeField] private float distanciaMaximaInicioSuperGiro = 3.5f;
    [SerializeField, Range(0f, 1f)] private float probabilidadSuperGiro = 0.4f;
    [Header("Salida lateral del Supergiro")]
    [SerializeField] private float distanciaPreparacionLateral = 2.8f;
    [SerializeField] private float velocidadPreparacionLateral = 7f;
    [SerializeField] private float distanciaCruceAtaque = 3f;
    [SerializeField] private float toleranciaPreparacionLateral = 0.35f;
    [SerializeField] private float offsetAlturaAtaqueLateral;
    [Header("Ritmo y recuperacion")]
    [SerializeField, Min(0.1f)] private float retrasoEntreSalidas = 0.3f;
    [SerializeField, Min(0.5f)] private float tiempoMaximoColocacion = 2.5f;
    [SerializeField, Min(5f)] private float tiempoMaximoPatron = 18f;
    [SerializeField, Min(0f)] private float velocidadDerivaOrbita = 0.85f;

    private static readonly HashSet<AguilaDuoCoordinator> activas = new HashSet<AguilaDuoCoordinator>();
    private AguilaEnemy aguila, pareja;
    private EnemigoVida vida;
    private AguilaDuoCoordinator parejaCoord;
    private Transform jugador;
    private Rigidbody2D jugadorRb;
    private EstadoPatron estadoPatron;
    private bool modoJefeActivo, combateDuoIniciadoAlgunaVez, parejaPerdidaDefinitivamente;
    private bool colisionesIgnoradas, pinzaLanzada, primeraSalidaA = true;
    private float timerCooldown, tiempoPatron, proximaBusqueda, timerHostigamiento, tiempoPinza;
    private AguilaEnemy atacantePinza, hostigadorPinza;
    private float ladoPinza;
    private Vector2 centroSuperGiro;
    private readonly Salida salidaA = new Salida();
    private readonly Salida salidaB = new Salida();

    public bool TieneParejaValida => ParejaSigueActiva();
    public AguilaEnemy ParejaActual => ParejaSigueActiva() ? pareja : null;
    public string PatronActual => estadoPatron.ToString();
    public string FaseSalidaA => salidaA.fase.ToString();
    public string FaseSalidaB => salidaB.fase.ToString();
    private bool SoyLider => parejaCoord != null && GetInstanceID() < parejaCoord.GetInstanceID();

    private void Awake()
    {
        aguila = GetComponent<AguilaEnemy>();
        vida = GetComponent<EnemigoVida>();
    }
    private void OnEnable()
    {
        activas.Add(this);
        if (vida != null) vida.OnDerrotado += AlMorir;
    }
    private void Start()
    {
        BuscarJugador();
        proximaBusqueda = Time.time + Random.Range(0f, 0.25f);
    }
    private void OnDisable()
    {
        activas.Remove(this);
        if (vida != null) vida.OnDerrotado -= AlMorir;
        RomperPareja();
    }
    private void AlMorir(DamageInfo dano) { RomperPareja(); }

    private void FixedUpdate()
    {
        if (aguila == null || !aguila.EstaViva || parejaPerdidaDefinitivamente) return;
        // Unity treats destroyed components as null, even if their managed reference remains.
        if (aguila.EsJefeSolitario || (parejaCoord == null && combateDuoIniciadoAlgunaVez))
        { RomperPareja(); parejaPerdidaDefinitivamente = true; return; }
        if (parejaCoord != null && !ParejaSigueActiva()) { RomperPareja(); return; }
        if (parejaCoord == null)
        {
            if (Time.time >= proximaBusqueda)
            {
                proximaBusqueda = Time.time + 0.65f;
                BuscarPareja();
            }
            return;
        }
        if (!SoyLider) return;
        if (jugador == null) BuscarJugador();
        if (jugador == null) { CancelarPatron(); return; }
        if (!modoJefeActivo)
        {
            Vector2 centro = ((Vector2)aguila.transform.position + (Vector2)pareja.transform.position) * 0.5f;
            if (Vector2.Distance(jugador.position, centro) > radioActivacionJefe && !aguila.CombateActivo && !pareja.CombateActivo) return;
            modoJefeActivo = true;
            combateDuoIniciadoAlgunaVez = parejaCoord.combateDuoIniciadoAlgunaVez = true;
            aguila.ForzarModoJefeDuo(true);
            pareja.ForzarModoJefeDuo(true);
            EntrarCooldown();
        }
        if (estadoPatron == EstadoPatron.Cooldown)
        {
            timerCooldown -= Time.fixedDeltaTime;
            if (timerCooldown > 0f || !AmbasDisponibles()) return;
            if (Vector2.Distance(aguila.transform.position, pareja.transform.position) <= distanciaMaximaInicioSuperGiro && Random.value < probabilidadSuperGiro) IniciarSuperGiro();
            else IniciarPinza();
            return;
        }
        tiempoPatron += Time.fixedDeltaTime;
        if (tiempoPatron > tiempoMaximoPatron) { CancelarPatron(); return; }
        if (estadoPatron == EstadoPatron.Pinza) ActualizarPinza();
        else if (estadoPatron == EstadoPatron.SuperGiro) ActualizarSuperGiro();
    }

    private void BuscarJugador()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go == null) return;
        jugador = go.transform;
        jugadorRb = go.GetComponent<Rigidbody2D>();
    }
    private void BuscarPareja()
    {
        if (parejaPerdidaDefinitivamente || parejaCoord != null || !aguila.PuedeSerDirigidaPorPareja) return;
        AguilaDuoCoordinator mejor = null;
        float distancia = radioBuscarPareja;
        foreach (var otra in activas)
        {
            if (otra == null || otra == this || !otra.isActiveAndEnabled || otra.parejaPerdidaDefinitivamente || otra.parejaCoord != null) continue;
            if (otra.aguila == null || !otra.aguila.PuedeSerDirigidaPorPareja) continue;
            float d = Vector2.Distance(transform.position, otra.transform.position);
            if (d <= distancia && d <= otra.radioBuscarPareja) { mejor = otra; distancia = d; }
        }
        if (mejor == null) return;
        // Claim both ends atomically: three eagles cannot form a chain of incompatible partners.
        parejaCoord = mejor;
        pareja = mejor.aguila;
        mejor.parejaCoord = this;
        mejor.pareja = aguila;
        CambiarColisiones(mejor, true);
    }
    private bool ParejaSigueActiva()
    {
        return isActiveAndEnabled && aguila != null && aguila.EstaViva && parejaCoord != null &&
            parejaCoord.isActiveAndEnabled && parejaCoord.parejaCoord == this && pareja != null && pareja.EstaViva;
    }

    private void RomperPareja()
    {
        var otra = parejaCoord;
        bool eraCombate = combateDuoIniciadoAlgunaVez || modoJefeActivo || (aguila != null && aguila.EstaBajoControlDuo) ||
            (otra != null && (otra.combateDuoIniciadoAlgunaVez || otra.modoJefeActivo));
        if (otra != null) CambiarColisiones(otra, false);
        LimpiarVinculo(eraCombate);
        if (otra != null && otra.parejaCoord == this) otra.LimpiarVinculo(eraCombate);
        RecuperarIndividual(aguila, eraCombate);
        if (otra != null) RecuperarIndividual(otra.aguila, eraCombate);
    }
    private void LimpiarVinculo(bool definitivo)
    {
        parejaCoord = null; pareja = null;
        modoJefeActivo = false;
        estadoPatron = EstadoPatron.Inactivo;
        parejaPerdidaDefinitivamente |= definitivo;
        atacantePinza = hostigadorPinza = null;
        salidaA.fase = salidaB.fase = FaseSalidaSuperGiro.Terminada;
        tiempoPatron = tiempoPinza = timerHostigamiento = 0f;
        colisionesIgnoradas = false;
    }
    private static void RecuperarIndividual(AguilaEnemy objetivo, bool persistente)
    {
        if (objetivo == null || !objetivo.EstaViva) return;
        if (persistente) objetivo.ConvertirseEnJefeSolitario();
        else
        {
            if (objetivo.EstaBajoControlDuo) objetivo.SalirDeControlDuo();
            objetivo.ForzarModoJefeDuo(false);
        }
    }
    private void CambiarColisiones(AguilaDuoCoordinator otra, bool ignorar)
    {
        if (otra == null || (ignorar && colisionesIgnoradas)) return;
        foreach (var a in GetComponentsInChildren<Collider2D>(true))
            foreach (var b in otra.GetComponentsInChildren<Collider2D>(true))
                if (a != null && b != null) Physics2D.IgnoreCollision(a, b, ignorar);
        colisionesIgnoradas = otra.colisionesIgnoradas = ignorar;
    }

    private bool AmbasDisponibles()
    {
        return ParejaSigueActiva() && aguila.PuedeSerDirigidaPorPareja && pareja.PuedeSerDirigidaPorPareja && aguila.EstaPatrullando && pareja.EstaPatrullando;
    }
    private void EntrarCooldown()
    {
        estadoPatron = EstadoPatron.Cooldown;
        tiempoPatron = 0f;
        timerCooldown = Random.Range(Mathf.Max(0.1f, cooldownMin), Mathf.Max(cooldownMin, cooldownMax));
    }
    private void CancelarPatron()
    {
        if (aguila != null && aguila.EstaViva && aguila.EstaBajoControlDuo) aguila.SalirDeControlDuo();
        if (pareja != null && pareja.EstaViva && pareja.EstaBajoControlDuo) pareja.SalirDeControlDuo();
        salidaA.fase = salidaB.fase = FaseSalidaSuperGiro.Terminada;
        EntrarCooldown();
    }

    private void IniciarPinza()
    {
        if (!AmbasDisponibles()) { EntrarCooldown(); return; }
        estadoPatron = EstadoPatron.Pinza;
        tiempoPatron = tiempoPinza = 0f;
        pinzaLanzada = false;
        primeraSalidaA = !primeraSalidaA;
        atacantePinza = primeraSalidaA ? aguila : pareja;
        hostigadorPinza = primeraSalidaA ? pareja : aguila;
        ladoPinza = atacantePinza.transform.position.x < jugador.position.x ? -1f : 1f;
        atacantePinza.IniciarHostigamientoDuo(PuntoLateral(ladoPinza) + Vector2.up * 1.1f, velocidadPreparacionLateral * 0.8f);
        hostigadorPinza.IniciarHostigamientoDuo(PuntoCobertura(), velocidadHostigamiento);
    }
    private Vector2 PuntoCobertura()
    {
        return new Vector2(jugador.position.x - ladoPinza * distanciaHostigamiento,
            Mathf.Max(jugador.position.y + 2.6f, hostigadorPinza.AlturaBaseY) + Mathf.Sin(Time.time * 1.2f) * 0.15f);
    }
    private void ActualizarPinza()
    {
        tiempoPinza += Time.fixedDeltaTime;
        if (!pinzaLanzada)
        {
            if (!atacantePinza.EstaBajoControlDuo || !atacantePinza.PuedeSerDirigidaPorPareja) { CancelarPatron(); return; }
            Vector2 punto = PuntoLateral(ladoPinza) + Vector2.up * 1.1f;
            atacantePinza.ActualizarDestinoHostigamientoDuo(punto);
            if (tiempoPinza > 0.25f && (Vector2.Distance(atacantePinza.transform.position, punto) < 0.6f || tiempoPinza > 1.25f))
            {
                Vector2 objetivo = PredecirJugador(0.18f);
                Vector2 direccion = (objetivo - (Vector2)atacantePinza.transform.position).normalized;
                atacantePinza.IniciarLanzamientoDuo(objetivo + direccion * 1.2f, velocidadAtaquePinza, false);
                pinzaLanzada = true;
                timerHostigamiento = duracionHostigamiento;
            }
        }
        else
        {
            timerHostigamiento -= Time.fixedDeltaTime;
            if (timerHostigamiento <= 0f && hostigadorPinza.EstaBajoControlDuo) hostigadorPinza.SalirDeControlDuo();
        }
        hostigadorPinza.ActualizarDestinoHostigamientoDuo(PuntoCobertura());
        if (pinzaLanzada && !atacantePinza.EstaBajoControlDuo && !hostigadorPinza.EstaBajoControlDuo) EntrarCooldown();
    }

    private void IniciarSuperGiro()
    {
        if (!AmbasDisponibles()) { EntrarCooldown(); return; }
        estadoPatron = EstadoPatron.SuperGiro;
        tiempoPatron = 0f;
        primeraSalidaA = !primeraSalidaA;
        Vector2 a = aguila.transform.position, b = pareja.transform.position;
        centroSuperGiro = (a + b) * 0.5f;
        salidaA.lado = a.x <= b.x ? -1f : 1f;
        salidaB.lado = -salidaA.lado;
        salidaA.fase = salidaB.fase = FaseSalidaSuperGiro.Orbitando;
        salidaA.tiempo = salidaB.tiempo = 0f;
        int vueltas = Random.Range(Mathf.Max(1, vueltasMinimas), Mathf.Max(vueltasMinimas, vueltasMaximas) + 1);
        float angulo = Mathf.Atan2(a.y - centroSuperGiro.y, a.x - centroSuperGiro.x) * Mathf.Rad2Deg;
        aguila.IniciarOrbitaDuo(centroSuperGiro, radioOrbita, velocidadAngular, angulo, vueltas);
        pareja.IniciarOrbitaDuo(centroSuperGiro, radioOrbita, velocidadAngular, angulo + 180f, vueltas);
    }
    private void ActualizarSuperGiro()
    {
        Vector2 centroDeseado = new Vector2(jugador.position.x, Mathf.Max(jugador.position.y + 3.2f, (aguila.AlturaBaseY + pareja.AlturaBaseY) * 0.5f));
        centroSuperGiro = Vector2.MoveTowards(centroSuperGiro, centroDeseado, velocidadDerivaOrbita * Time.fixedDeltaTime);
        ActualizarSalida(aguila, salidaA, salidaB, primeraSalidaA);
        ActualizarSalida(pareja, salidaB, salidaA, !primeraSalidaA);
        if (salidaA.fase == FaseSalidaSuperGiro.Terminada && salidaB.fase == FaseSalidaSuperGiro.Terminada) EntrarCooldown();
    }
    private void ActualizarSalida(AguilaEnemy objetivo, Salida salida, Salida otra, bool primera)
    {
        salida.tiempo += Time.fixedDeltaTime;
        switch (salida.fase)
        {
            case FaseSalidaSuperGiro.Orbitando:
            case FaseSalidaSuperGiro.EsperandoTurno:
                if (!objetivo.EstaEnOrbitaDuo) { TerminarSalida(objetivo, salida); break; }
                objetivo.ActualizarCentroOrbitaDuo(centroSuperGiro);
                if (!objetivo.TerminoVueltasDuo) break;
                if (primera || (otra.fase == FaseSalidaSuperGiro.Terminada && otra.tiempo >= retrasoEntreSalidas))
                {
                    salida.fase = FaseSalidaSuperGiro.Colocandose;
                    salida.tiempo = 0f;
                    salida.destino = PuntoLateral(salida.lado);
                    objetivo.IniciarHostigamientoDuo(salida.destino, velocidadPreparacionLateral);
                }
                else
                {
                    if (salida.fase != FaseSalidaSuperGiro.EsperandoTurno) { salida.fase = FaseSalidaSuperGiro.EsperandoTurno; salida.tiempo = 0f; }
                    if (salida.tiempo > 8f) TerminarSalida(objetivo, salida);
                }
                break;
            case FaseSalidaSuperGiro.Colocandose:
                if (!objetivo.EstaBajoControlDuo || !objetivo.PuedeSerDirigidaPorPareja) { TerminarSalida(objetivo, salida); break; }
                salida.destino = Vector2.MoveTowards(salida.destino, PuntoLateral(salida.lado), velocidadPreparacionLateral * 0.65f * Time.fixedDeltaTime);
                objetivo.ActualizarDestinoHostigamientoDuo(salida.destino);
                if (salida.tiempo > tiempoMaximoColocacion) { TerminarSalida(objetivo, salida); break; }
                if (salida.tiempo >= 0.18f && Vector2.Distance(objetivo.transform.position, salida.destino) <= toleranciaPreparacionLateral &&
                    (objetivo.transform.position.x - jugador.position.x) * salida.lado > 0.5f)
                {
                    Vector2 destino = new Vector2(jugador.position.x - salida.lado * distanciaCruceAtaque, objetivo.transform.position.y);
                    objetivo.IniciarLanzamientoDuo(destino, velocidadSalidaSupergiro, true);
                    salida.fase = FaseSalidaSuperGiro.Atacando;
                    salida.tiempo = 0f;
                }
                break;
            case FaseSalidaSuperGiro.Atacando:
                if (!objetivo.EstaEnLanzamientoDuo || salida.tiempo > 2f) TerminarSalida(objetivo, salida);
                break;
        }
    }
    private static void TerminarSalida(AguilaEnemy objetivo, Salida salida)
    {
        if (objetivo != null && objetivo.EstaViva && objetivo.EstaBajoControlDuo) objetivo.SalirDeControlDuo();
        salida.fase = FaseSalidaSuperGiro.Terminada;
        salida.tiempo = 0f;
    }
    private Vector2 PuntoLateral(float lado)
    {
        return new Vector2(jugador.position.x + lado * distanciaPreparacionLateral, jugador.position.y + offsetAlturaAtaqueLateral);
    }
    private Vector2 PredecirJugador(float tiempo)
    {
        return (Vector2)jugador.position + (jugadorRb != null ? Vector2.ClampMagnitude(jugadorRb.velocity * tiempo, 1.2f) : Vector2.zero);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioBuscarPareja);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, distanciaMaximaInicioSuperGiro);
    }
}
