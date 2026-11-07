using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent, DefaultExecutionOrder(-60)]
[RequireComponent(typeof(SapoEnemy))]
public class SapoCoordinacionGrupo : MonoBehaviour
{
    // Preserve existing enum values and serialized tuning when loading scenes/prefabs.
    public enum PatronGrupo { Duelista, Pinza, Triangulo, Oleadas, Emboscada }
    public enum RolTactico { Duelista, Presion, Interceptor, FlancoIzquierdo, FlancoDerecho, Senuelo }
    public enum SaltoTactico { Libre, Pequeno, Medio, Grande }
    public enum FaseEquipo { Lectura, Presion, Respiro }

    [Header("Jugador")]
    [SerializeField] private Transform jugador;
    [SerializeField] private string playerTag = "Player";
    [Header("Equipo local")]
    [SerializeField, Min(1f)] private float radioCoordinacion = 7f;
    [SerializeField, Range(1, 5)] private int maximoMiembros = 5;
    [SerializeField, Min(0.1f)] private float intervaloRecalculoGrupo = 0.25f;
    [Header("Ritmo por equipo")]
    [SerializeField] private float duracionTurnoPinza = 1.35f;
    [SerializeField] private float duracionTurnoTriangulo = 1.20f;
    [SerializeField] private float duracionOleada = 1.15f;
    [SerializeField, Min(0.4f)] private float respiroEntreRondas = 0.85f;
    [SerializeField, Min(0.15f)] private float retrasoCastigoEspada = 0.22f;
    [SerializeField, Range(0.4f, 1.5f)] private float esperaMaximaAnteEspada = 1.1f;
    [Header("Posicionamiento")]
    [SerializeField] private float distanciaFlanco = 2.4f;
    [SerializeField] private float adelantamientoInterceptor = 1.1f;
    [SerializeField] private float distanciaSeparacion = 0.9f;
    [SerializeField] private float fuerzaSeparacion = 0.65f;
    [Header("Lectura observable del jugador")]
    [SerializeField] private float tiempoPrediccionInterceptor = 0.55f;
    [SerializeField] private float prediccionMaxima = 3f;
    [Header("Dificultad por cantidad")]
    [SerializeField] private float agresividadDos = 1.08f;
    [SerializeField] private float agresividadTres = 1.18f;
    [SerializeField] private float agresividadCuatro = 1.30f;
    [SerializeField] private float agresividadCinco = 1.40f;
    [SerializeField] private float pausaDos = 0.95f;
    [SerializeField] private float pausaTres = 0.90f;
    [SerializeField] private float pausaCuatro = 0.86f;
    [SerializeField] private float pausaCinco = 0.82f;
    [Header("Debug")]
    [SerializeField] private bool dibujarGizmos = true;
    [Header("Estado en juego")]
    [SerializeField] private PatronGrupo patronActual;
    [SerializeField] private RolTactico rolActual;
    [SerializeField] private int cantidadGrupo = 1, indiceEnGrupo, faseActual;
    [SerializeField] private bool puedeAtacarAhora;
    [SerializeField] private float multiplicadorAgresividad = 1f, multiplicadorPausa = 1f;
    [SerializeField] private FaseEquipo faseEquipo;

    private static readonly List<SapoCoordinacionGrupo> activos = new List<SapoCoordinacionGrupo>();
    private static readonly List<Equipo> equipos = new List<Equipo>();
    private static float ultimoTick = -1f, siguienteRecalculo;
    private static int siguienteId;
    private SapoEnemy sapo;
    private EnemigoVida vida;
    private Rigidbody2D rbJugador;
    private SpriteRenderer spriteJugador;
    private EspadaHitbox espada;
    private SapoSensorSuelo sensor;
    private Equipo equipo;
    private float bloqueoAtaqueHasta, siguienteReposicion;

    public PatronGrupo PatronActual => patronActual;
    public RolTactico RolActual => rolActual;
    public int CantidadGrupo => equipo != null ? equipo.miembros.Count : 1;
    public int IndiceEnGrupo => indiceEnGrupo;
    public int FaseActual => faseActual;
    public float MultiplicadorAgresividad => multiplicadorAgresividad;
    public float MultiplicadorPausa => multiplicadorPausa;
    public bool PuedeAtacarAhora => Valido && equipo != null && rolActual != RolTactico.Senuelo &&
        equipo.PuedeParticipar(this) && Time.time >= bloqueoAtaqueHasta && sapo.PuedePrepararAtaque;
    public int EquipoId => equipo != null ? equipo.id : 0;
    public int LimiteAtaquesSimultaneos => CantidadGrupo < 4 ? 1 : 2;
    public int AtaquesSimultaneos => equipo != null ? equipo.ContarAtaques() : 0;
    public FaseEquipo FaseActualEquipo => equipo != null ? equipo.fase : FaseEquipo.Lectura;
    public float PresionEspadaObservada => equipo != null ? equipo.presionEspada : 0f;
    public Vector2 VelocidadJugadorObservada => equipo != null ? equipo.velocidadLeida : Vector2.zero;
    public bool ComparteEquipoCon(SapoCoordinacionGrupo otro) => otro != null && equipo != null && equipo == otro.equipo;
    private bool Valido => isActiveAndEnabled && sapo != null && sapo.EstaVivo;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarRegistro()
    { activos.Clear(); equipos.Clear(); ultimoTick = -1f; siguienteRecalculo = 0f; siguienteId = 0; }
    private void Awake()
    {
        sapo = GetComponent<SapoEnemy>(); vida = GetComponent<EnemigoVida>(); sensor = GetComponent<SapoSensorSuelo>();
    }
    private void OnEnable()
    {
        if (!activos.Contains(this)) activos.Add(this);
        vida.OnDerrotado += AlMorir;
        siguienteRecalculo = 0f;
    }
    private void Start() { BuscarJugador(); }
    private void OnDisable()
    {
        vida.OnDerrotado -= AlMorir;
        activos.Remove(this);
        RetirarDelEquipo(); siguienteRecalculo = 0f;
    }
    private void AlMorir(DamageInfo dano) { RetirarDelEquipo(); siguienteRecalculo = 0f; }
    private void RetirarDelEquipo()
    {
        if (equipo == null) return;
        var anterior = equipo;
        anterior.Colisiones(false);
        anterior.miembros.Remove(this); equipo = null;
        anterior.Reconfigurar(); anterior.Colisiones(true);
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
        spriteJugador = jugador.GetComponent<SpriteRenderer>();
        espada = jugador.GetComponentInChildren<EspadaHitbox>(true);
    }
    private void FixedUpdate()
    {
        if (ultimoTick == Time.fixedTime) return;
        ultimoTick = Time.fixedTime;
        for (int i = equipos.Count - 1; i >= 0; i--)
        {
            var e = equipos[i];
            for (int m = e.miembros.Count - 1; m >= 0; m--)
            {
                var miembro = e.miembros[m];
                if (miembro != null && miembro.Valido) continue;
                e.Colisiones(false);
                if (miembro != null) miembro.equipo = null;
                e.miembros.RemoveAt(m); e.Reconfigurar(); e.Colisiones(true);
            }
            if (e.miembros.Count == 0) equipos.RemoveAt(i);
        }
        if (Time.time >= siguienteRecalculo)
        {
            siguienteRecalculo = Time.time + Mathf.Max(0.1f, intervaloRecalculoGrupo);
            RecalcularEquipos();
        }
        foreach (var e in equipos) e.Tick();
    }
    private static bool Compatibles(SapoCoordinacionGrupo a, SapoCoordinacionGrupo b, float margen)
    {
        if (a == null || b == null || !a.Valido || !b.Valido || a.jugador != b.jugador) return false;
        if (Vector2.Distance(a.transform.position, b.transform.position) > Mathf.Min(a.radioCoordinacion, b.radioCoordinacion) + margen) return false;
        int mask = a.sensor != null ? a.sensor.CapasSuelo.value : 0;
        Vector2 desde = (Vector2)a.transform.position + Vector2.up * 0.25f;
        Vector2 hasta = (Vector2)b.transform.position + Vector2.up * 0.25f;
        var hit = Physics2D.Linecast(desde, hasta, mask);
        return hit.collider == null || hit.collider.isTrigger;
    }
    private static bool EquiposCompatibles(Equipo a, Equipo b, float margen = 0f)
    {
        int cantidad = a.miembros.Count + b.miembros.Count;
        if (cantidad > 5) return false;
        foreach (var m in a.miembros) if (cantidad > Mathf.Clamp(m.maximoMiembros, 1, 5)) return false;
        foreach (var m in b.miembros) if (cantidad > Mathf.Clamp(m.maximoMiembros, 1, 5)) return false;
        foreach (var x in a.miembros) foreach (var y in b.miembros) if (!Compatibles(x, y, margen)) return false;
        return true;
    }
    private static void RecalcularEquipos()
    {
        // Keep membership stable through jumps, then split only members that actually leave the area.
        foreach (var e in equipos.ToArray())
        {
            for (int i = e.miembros.Count - 1; i > 0; i--)
            {
                bool cerca = true;
                for (int j = 0; j < i; j++) if (!Compatibles(e.miembros[i], e.miembros[j], 2.2f)) { cerca = false; break; }
                if (!cerca) e.miembros[i].RetirarDelEquipo();
            }
        }
        foreach (var miembro in activos)
        {
            if (!miembro.Valido) continue;
            if (miembro.jugador == null) miembro.BuscarJugador();
            if (miembro.equipo != null) continue;
            var nuevo = new Equipo(++siguienteId);
            nuevo.miembros.Add(miembro); miembro.equipo = nuevo;
            nuevo.Reconfigurar(); equipos.Add(nuevo);
        }
        // Merge whole teams atomically; every member sees the same roster and shared attack budget.
        for (int i = 0; i < equipos.Count; i++)
        {
            var a = equipos[i];
            if (a.ContarAtaques() > 0) continue;
            for (int j = equipos.Count - 1; j > i; j--)
            {
                var b = equipos[j];
                if (b.ContarAtaques() > 0 || !EquiposCompatibles(a, b)) continue;
                a.Colisiones(false); b.Colisiones(false);
                foreach (var m in b.miembros) { a.miembros.Add(m); m.equipo = a; }
                b.miembros.Clear(); equipos.RemoveAt(j);
                a.Reconfigurar(); a.Colisiones(true);
            }
        }
    }
    private void PublicarEstado(FaseEquipo fase)
    {
        cantidadGrupo = CantidadGrupo; indiceEnGrupo = equipo.miembros.IndexOf(this);
        patronActual = cantidadGrupo == 1 ? PatronGrupo.Duelista : cantidadGrupo == 2 ? PatronGrupo.Pinza :
            cantidadGrupo == 3 ? PatronGrupo.Triangulo : cantidadGrupo == 4 ? PatronGrupo.Oleadas : PatronGrupo.Emboscada;
        multiplicadorAgresividad = cantidadGrupo == 1 ? 1f : cantidadGrupo == 2 ? agresividadDos : cantidadGrupo == 3 ? agresividadTres : cantidadGrupo == 4 ? agresividadCuatro : agresividadCinco;
        multiplicadorPausa = cantidadGrupo == 1 ? 1f : cantidadGrupo == 2 ? pausaDos : cantidadGrupo == 3 ? pausaTres : cantidadGrupo == 4 ? pausaCuatro : pausaCinco;
        faseEquipo = fase; faseActual = equipo.ronda;
        puedeAtacarAhora = PuedeAtacarAhora;
    }
    private sealed class Equipo
    {
        public readonly int id;
        public readonly List<SapoCoordinacionGrupo> miembros = new List<SapoCoordinacionGrupo>();
        private readonly HashSet<SapoCoordinacionGrupo> usados = new HashSet<SapoCoordinacionGrupo>();
        public FaseEquipo fase;
        public Vector2 velocidadLeida;
        public float presionEspada;
        public int ronda;
        private float proximoAtaque, hastaFase, ultimaEvasion = -100f, ultimaReposicion = -100f;
        private float finEspada = -100f;
        private float ultimoAvance;
        private float esperaEspadaDesde = -1f;
        private int secuencia = -1, ataquesRonda;
        private bool espadaActivaAntes;
        public Equipo(int id) { this.id = id; }
        public int ContarAtaques()
        { int n = 0; foreach (var m in miembros) if (m.Valido && m.sapo.AtaqueEnCurso) n++; return n; }
        private int ContarEnAire()
        { int n = 0; foreach (var m in miembros) if (m.Valido && !m.sapo.EnSuelo) n++; return n; }
        public bool PuedeOfensiva => fase == FaseEquipo.Presion && Time.time >= proximoAtaque && ContarAtaques() < (miembros.Count < 4 ? 1 : 2);
        public bool PuedeParticipar(SapoCoordinacionGrupo miembro) => PuedeOfensiva && !usados.Contains(miembro);
        public void Colisiones(bool ignorar)
        {
            for (int i = 0; i < miembros.Count; i++) for (int j = i + 1; j < miembros.Count; j++)
            {
                if (miembros[i] == null || miembros[j] == null) continue;
                var a = miembros[i].GetComponent<Collider2D>(); var b = miembros[j].GetComponent<Collider2D>();
                if (a != null && b != null) Physics2D.IgnoreCollision(a, b, ignorar);
            }
        }
        public void Reconfigurar()
        {
            usados.Clear(); ataquesRonda = 0;
            foreach (var m in miembros) m.sapo.CancelarPreparacion();
            AsignarRoles(); fase = FaseEquipo.Lectura;
            hastaFase = proximoAtaque = Time.time + Random.Range(0.45f, 0.7f);
            ultimoAvance = Time.time;
            esperaEspadaDesde = -1f;
            foreach (var m in miembros) m.PublicarEstado(fase);
        }
        private void AsignarRoles()
        {
            var orden = new List<SapoCoordinacionGrupo>(miembros);
            for (int i = orden.Count - 1; i > 0; i--)
            { int j = Random.Range(0, i + 1); var t = orden[i]; orden[i] = orden[j]; orden[j] = t; }
            var roles = new List<RolTactico>();
            if (orden.Count == 1) roles.Add(RolTactico.Duelista);
            else
            {
                roles.Add(RolTactico.Presion); roles.Add(RolTactico.Interceptor);
                if (orden.Count >= 3) roles.Add(Random.value < 0.5f ? RolTactico.FlancoIzquierdo : RolTactico.FlancoDerecho);
                if (orden.Count >= 4) roles.Add(roles[2] == RolTactico.FlancoIzquierdo ? RolTactico.FlancoDerecho : RolTactico.FlancoIzquierdo);
                if (orden.Count == 5) roles.Add(RolTactico.Senuelo);
            }
            for (int i = 0; i < orden.Count; i++) orden[i].rolActual = roles[i];
        }
        public void Tick()
        {
            if (miembros.Count == 0) return;
            var lector = miembros[0];
            if (lector.jugador == null) lector.BuscarJugador();
            if (lector.jugador == null) return;
            Vector2 v = lector.rbJugador != null ? lector.rbJugador.velocity : Vector2.zero;
            velocidadLeida = Vector2.Lerp(velocidadLeida, v, 1f - Mathf.Exp(-Time.fixedDeltaTime / 0.3f));
            bool espadaActiva = lector.espada != null && lector.espada.EstaActiva;
            int nueva = lector.espada != null ? lector.espada.SecuenciaAtaque : -1;
            if (nueva != secuencia)
            { if (secuencia >= 0) presionEspada = Mathf.Min(1f, presionEspada + 0.28f); secuencia = nueva; }
            presionEspada = Mathf.Max(0f, presionEspada - Time.fixedDeltaTime * 0.12f);
            if (!espadaActiva && espadaActivaAntes) finEspada = Time.time;
            espadaActivaAntes = espadaActiva;
            if (fase == FaseEquipo.Respiro && Time.time >= hastaFase)
            {
                ronda++; usados.Clear(); ataquesRonda = 0; AsignarRoles();
                fase = FaseEquipo.Lectura; hastaFase = Time.time + Random.Range(0.22f, 0.38f); ultimoAvance = Time.time;
            }
            if (fase == FaseEquipo.Lectura && Time.time >= hastaFase) fase = FaseEquipo.Presion;
            int cupo = miembros.Count == 1 ? 1 : miembros.Count == 2 ? 2 : miembros.Count == 5 ? 4 : 3;
            if (fase == FaseEquipo.Presion && Time.time - ultimoAvance > 4f && ContarAtaques() == 0) ataquesRonda = cupo;
            if (fase == FaseEquipo.Presion && ataquesRonda >= cupo && ContarAtaques() == 0)
            {
                fase = FaseEquipo.Respiro;
                hastaFase = Time.time + Mathf.Max(0.65f, lector.respiroEntreRondas + (miembros.Count == 1 ? 0.6f : 0f));
            }
            if (PuedeOfensiva && ataquesRonda < cupo)
            {
                // Read a swing and its recovery instead of reacting to input or granting invulnerability.
                bool esperaEspada = miembros.Count >= 3 && presionEspada > 0.25f &&
                    (espadaActiva || Time.time - finEspada < lector.retrasoCastigoEspada);
                if (esperaEspada)
                {
                    if (esperaEspadaDesde < 0f) esperaEspadaDesde = Time.time;
                    // Endless swings must not freeze the whole team. Commit a telegraphed flank
                    // after a bounded observation window, favoring the side behind the player.
                    if (Time.time - esperaEspadaDesde >= lector.esperaMaximaAnteEspada)
                    { ElegirAtacante(); esperaEspadaDesde = Time.time; }
                }
                else { esperaEspadaDesde = -1f; ElegirAtacante(); }
            }
            foreach (var m in miembros) m.PublicarEstado(fase);
        }
        private void ElegirAtacante()
        {
            SapoCoordinacionGrupo elegido = null;
            float mejor = float.NegativeInfinity;
            foreach (var m in miembros)
            {
                if (!m.Valido || usados.Contains(m) || Time.time < m.bloqueoAtaqueHasta || !m.sapo.PuedePrepararAtaque || m.rolActual == RolTactico.Senuelo) continue;
                float puntuacion = Mathf.Min(4f, Time.time - m.sapo.UltimoAtaque) * 0.4f + Random.Range(0f, 0.5f);
                if (m.rolActual == RolTactico.Presion) puntuacion += 0.45f;
                if (m.rolActual == RolTactico.Interceptor && Mathf.Abs(velocidadLeida.x) > 1f) puntuacion += 0.65f;
                if (presionEspada > 0.3f && (m.transform.position.x - m.jugador.position.x) * m.MiradaJugador() < 0f) puntuacion += 0.75f;
                if (puntuacion > mejor) { mejor = puntuacion; elegido = m; }
            }
            if (elegido == null)
            {
                // Do not stall a round when its remaining member cannot reach safe ground.
                bool todosUsados = true;
                foreach (var m in miembros) if (m.rolActual != RolTactico.Senuelo && !usados.Contains(m)) todosUsados = false;
                if (todosUsados) ataquesRonda = miembros.Count == 2 ? 2 : miembros.Count == 5 ? 4 : miembros.Count == 1 ? 1 : 3;
                return;
            }
            if (!elegido.sapo.PrepararAtaque()) { elegido.bloqueoAtaqueHasta = Time.time + 0.35f; return; }
            usados.Add(elegido); ataquesRonda++;
            ultimoAvance = Time.time;
            int n = miembros.Count;
            float intervalo = n == 1 ? 1.5f : n == 2 ? elegido.duracionTurnoPinza * 0.85f :
                n == 3 ? elegido.duracionTurnoTriangulo * 0.72f : n == 4 ? elegido.duracionOleada * 0.60f : elegido.duracionOleada * 0.46f;
            proximoAtaque = Time.time + Mathf.Max(0.5f, intervalo) + Random.Range(0.03f, 0.12f);
        }
        public bool PuedeEvadir() => Time.time - ultimaEvasion >= 0.85f && ContarEnAire() < (miembros.Count >= 4 ? 3 : 2);
        public bool PuedeReposicionar() => Time.time - ultimaReposicion >= 0.55f && ContarEnAire() < (miembros.Count >= 4 ? 3 : 2);
        public void Evasion() { ultimaEvasion = Time.time; }
        public void Reposicion() { ultimaReposicion = Time.time; }
    }
    private float MiradaJugador() => spriteJugador != null && spriteJugador.flipX ? -1f : 1f;
    public float ObtenerObjetivoAtaqueX()
    {
        if (jugador == null) return transform.position.x;
        float vx = equipo != null ? equipo.velocidadLeida.x : 0f;
        float prediccion = Mathf.Clamp(vx * tiempoPrediccionInterceptor, -Mathf.Min(1.8f, prediccionMaxima), Mathf.Min(1.8f, prediccionMaxima));
        switch (rolActual)
        {
            case RolTactico.Interceptor: return jugador.position.x + prediccion + Mathf.Sign(vx) * adelantamientoInterceptor * 0.35f;
            case RolTactico.FlancoIzquierdo: return jugador.position.x - 0.45f + prediccion * 0.25f;
            case RolTactico.FlancoDerecho: return jugador.position.x + 0.45f + prediccion * 0.25f;
            default: return jugador.position.x + prediccion * (CantidadGrupo == 1 ? 0.1f : 0.2f);
        }
    }
    public float ObtenerObjetivoX()
    {
        if (jugador == null) return transform.position.x;
        float lado = Mathf.Sign(transform.position.x - jugador.position.x);
        if (lado == 0f) lado = indiceEnGrupo % 2 == 0 ? -1f : 1f;
        float distancia = Mathf.Max(2.1f, distanciaFlanco);
        if (rolActual == RolTactico.FlancoIzquierdo) lado = -1f;
        if (rolActual == RolTactico.FlancoDerecho) lado = 1f;
        if (rolActual == RolTactico.Senuelo) lado = MiradaJugador();
        float espacio = (indiceEnGrupo % 2) * distanciaSeparacion * fuerzaSeparacion;
        return jugador.position.x + lado * (distancia + espacio);
    }
    public SaltoTactico ObtenerSaltoSugerido(float distanciaJugador)
    { return rolActual == RolTactico.Interceptor ? SaltoTactico.Grande : rolActual == RolTactico.Duelista ? SaltoTactico.Libre : distanciaJugador > 1.8f ? SaltoTactico.Medio : SaltoTactico.Pequeno; }
    public void NotificarAtaqueRealizado(float bloqueoPersonal = 0.2f) { bloqueoAtaqueHasta = Time.time + bloqueoPersonal; }
    public bool PuedeEvadirAhora() => equipo == null || equipo.PuedeEvadir();
    public void NotificarEvasion() { if (equipo != null) equipo.Evasion(); }
    public bool PuedeReposicionarAhora() => Time.time >= siguienteReposicion && equipo != null && equipo.PuedeReposicionar();
    public void NotificarReposicion()
    { siguienteReposicion = Time.time + Random.Range(1.2f, 1.8f); if (equipo != null) equipo.Reposicion(); }
    private void OnDrawGizmosSelected()
    {
        if (!dibujarGizmos) return;
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, radioCoordinacion);
        if (!Application.isPlaying || equipo == null) return;
        foreach (var m in equipo.miembros) if (m != null) Gizmos.DrawLine(transform.position, m.transform.position);
        if (jugador != null) { Gizmos.color = Color.yellow; Gizmos.DrawLine(transform.position, new Vector3(ObtenerObjetivoX(), jugador.position.y)); }
    }
}
